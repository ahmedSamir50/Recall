using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.Core.Vectors;

namespace Recall.Core.Search;

public sealed class TextSearchService : ITextSearchService
{
    private readonly ITextEmbeddingEncoder encoder;
    private readonly ITextVectorIndex index;
    private readonly IIndexPolicy indexPolicy;
    private readonly IIngestCursorStore cursors;
    private readonly IEncoderFileHash encoderHash;
    private readonly IMediaVectorIndex media;

    private readonly IVisionEmbeddingEncoder? visionEncoder;

    public TextSearchService(
        ITextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        IIndexPolicy indexPolicy,
        IIngestCursorStore cursors,
        IEncoderFileHash encoderHash,
        IMediaVectorIndex media,
        IVisionEmbeddingEncoder? visionEncoder = null)
    {
        this.encoder = encoder;
        this.index = index;
        this.indexPolicy = indexPolicy;
        this.cursors = cursors;
        this.encoderHash = encoderHash;
        this.media = media ?? throw new ArgumentNullException(nameof(media));
        this.visionEncoder = visionEncoder;
    }

    private void ReconcilePending(IReadOnlyList<ScopeFolderGrant> grants)
    {
        if (cursors.PendingCount() > 0 && cursors.TryEnterWrite(out var lease) && lease is not null)
        {
            try
            {
                var pending = cursors.ListPending(RecallIndexCaps.RawListCap);
                if (pending.Count > 0)
                {
                    var states = pending
                        .Select(id => new VectorLiveState(
                            id,
                            cursors.AnyLivePath(id),
                            encoderHash.TextHash ?? string.Empty,
                            ResolveGrantPath(grants)))
                        .ToList();
                    index.Reconcile(states);
                    media.Reconcile(states);
                    cursors.RemovePending(pending);
                }

                lease.Commit();
            }
            catch
            {
                lease.Rollback();
                throw;
            }
        }
    }

    internal IVisionEmbeddingEncoder? VisionEncoder => visionEncoder;

    public async Task<IReadOnlyList<TextSearchHit>> FindAsync(
        string query,
        IReadOnlyList<ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || grants is null)
        {
            return [];
        }

        var grantedPaths = BuildGrantedPathSet(grants);
        var mediaGrantPaths = BuildMediaGrantSet(grants);
        if (grantedPaths.Count == 0 && mediaGrantPaths.Count == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();
        ReconcilePending(grants);

        var textHits = new List<TextSearchHit>();
        if (grantedPaths.Count > 0)
        {
            textHits.AddRange(await FindTextAsync(query, grantedPaths, cancellationToken).ConfigureAwait(false));
        }

        var tryMedia = mediaGrantPaths.Count > 0 && textHits.Count < RecallIndexCaps.FileCap;
        var ranked = new List<TextSearchHit>(textHits);
        if (tryMedia)
        {
            var present = new HashSet<string>(textHits.Select(h => h.SourcePath), StringComparer.OrdinalIgnoreCase);
            foreach (var hit in SearchMediaWords(query, mediaGrantPaths, RecallIndexCaps.FileCap - textHits.Count, cancellationToken))
            {
                if (!present.Add(hit.SourcePath))
                {
                    continue;
                }

                ranked.Add(hit);
            }
        }

        return ranked.Take(RecallIndexCaps.FileCap).ToList();
    }

    private async Task<IReadOnlyList<TextSearchHit>> FindTextAsync(
        string query,
        HashSet<string> grantedPaths,
        CancellationToken cancellationToken = default)
    {
        var hash = encoderHash.TextHash ?? string.Empty;
        var filter = TextSearchFilter.Build(grantedPaths, hash);
        var prefixed = EmbeddingTaskPrefixes.WrapSearchQuery(query);
        var full768 = await encoder.EmbedPrefixedTextAsync(prefixed, cancellationToken).ConfigureAwait(false);
        var storedWidth = (int)indexPolicy.TextStoredWidth;
        var query512 = MatryoshkaVectorMath.CutAndNormalize(full768.Span, storedWidth);
        var bm25Only = cursors.ListBm25Only();
        var useInDbRrf = bm25Only.Count == 0;
        var request = new TextSearchRequest(query, query512, grantedPaths, hash, filter, useInDbRrf, RecallIndexCaps.RawListCap);
        IReadOnlyList<TextSearchHit> ranked;
        if (useInDbRrf)
        {
            ranked = index.Search(request, cancellationToken);
        }
        else
        {
            var denseRequest = request with { UseInDatabaseRrf = false };
            var dense = index.Search(denseRequest, cancellationToken);
            var ftsRequest = denseRequest with { QueryVector512 = ReadOnlyMemory<float>.Empty };
            var bm25Hits = index.Search(ftsRequest, cancellationToken);
            var mergedBm25 = MergeBm25Only(bm25Only, query, bm25Hits);
            ranked = HybridFusion.Fuse(dense, mergedBm25);
        }

        var byPath = new Dictionary<string, List<TextSearchHit>>(StringComparer.OrdinalIgnoreCase);
        foreach (var hit in ranked)
        {
            var aliases = cursors.ListLiveAliases(hit.ContentId);
            var expandedHits = aliases.Count > 0
                ? SearchHitMarks.Expand(hit, aliases)
                : [hit];
            foreach (var expanded in expandedHits)
            {
                if (!grantedPaths.Contains(expanded.GrantPath))
                {
                    continue;
                }

                if (!byPath.TryGetValue(expanded.SourcePath, out var list))
                {
                    list = [];
                    byPath[expanded.SourcePath] = list;
                }

                list.Add(expanded);
            }
        }

        return byPath.Values
            .Select(Collapse.MaxRrf)
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.Ordinal)
            .Take(RecallIndexCaps.FileCap)
            .ToList();
    }

    internal IReadOnlyList<TextSearchHit> SearchMediaWords(
        string query,
        IReadOnlySet<string> mediaGrantPaths,
        CancellationToken cancellationToken) =>
        SearchMediaWords(query, mediaGrantPaths, RecallIndexCaps.FileCap, cancellationToken);

    internal IReadOnlyList<TextSearchHit> SearchMediaWords(
        string query,
        IReadOnlySet<string> mediaGrantPaths,
        int fileBudget,
        CancellationToken cancellationToken)
    {
        if (fileBudget <= 0)
        {
            return [];
        }

        var mediaFilter = TextSearchFilter.Build(mediaGrantPaths, encoderHash.AudioHash ?? string.Empty);
        if (mediaFilter.Length == 0)
        {
            return [];
        }

        var mediaQuery = string.Join(' ', KeywordFold.Tokens(KeywordFold.Fold(query)));
        var best = new Dictionary<string, MediaSearchHit>(StringComparer.OrdinalIgnoreCase);
        foreach (var collection in new[] { MediaCollections.Audio, MediaCollections.Video })
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mediaHits = media.SearchWords(collection, mediaQuery, mediaFilter, RecallIndexCaps.RawListCap, cancellationToken);
            foreach (var hit in mediaHits)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!best.TryGetValue(hit.SourcePath, out var existing) || PreferMedia(hit, existing))
                {
                    best[hit.SourcePath] = hit;
                }
            }
        }

        var winners = new List<MediaSearchHit>(best.Count);
        foreach (var hit in best.Values)
        {
            InsertByScore(winners, hit);
            if (winners.Count > fileBudget)
            {
                winners.RemoveAt(winners.Count - 1);
            }
        }

        var hits = new List<TextSearchHit>(winners.Count);
        foreach (var hit in winners)
        {
            hits.Add(new TextSearchHit(
                hit.ContentId,
                hit.SourcePath,
                Path.GetFileName(hit.SourcePath),
                LastWriteUtc(hit.SourcePath),
                TextMatchReason.FullText,
                hit.Score,
                0,
                "",
                "",
                false,
                hit.StartSeconds));
        }

        return hits;
    }

    private static bool PreferMedia(MediaSearchHit candidate, MediaSearchHit current)
    {
        if (candidate.Score > current.Score)
        {
            return true;
        }

        return Math.Abs(candidate.Score - current.Score) < 1e-12 && candidate.StartSeconds < current.StartSeconds;
    }

    private static void InsertByScore(List<MediaSearchHit> winners, MediaSearchHit hit)
    {
        var index = winners.Count;
        while (index > 0 && PreferMedia(hit, winners[index - 1]))
        {
            index--;
        }

        winners.Insert(index, hit);
    }

    private static DateTimeOffset LastWriteUtc(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (info.Exists)
            {
                return new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);
            }
        }
        catch (IOException)
        {
            return DateTimeOffset.UnixEpoch;
        }
        catch (UnauthorizedAccessException)
        {
            return DateTimeOffset.UnixEpoch;
        }

        return DateTimeOffset.UnixEpoch;
    }

    private static HashSet<string> BuildGrantedPathSet(IReadOnlyList<ScopeFolderGrant> grants)
    {
        return grants
            .Where(grant => grant.EnabledKinds.HasFlag(GrantFileKinds.Text) || grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
            .Select(grant => grant.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> BuildMediaGrantSet(IReadOnlyList<ScopeFolderGrant> grants)
    {
        return grants
            .Where(grant => grant.EnabledKinds.HasFlag(GrantFileKinds.Audio) || grant.EnabledKinds.HasFlag(GrantFileKinds.Video))
            .Select(grant => grant.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveGrantPath(IReadOnlyList<ScopeFolderGrant> grants) =>
        grants.FirstOrDefault()?.Path ?? string.Empty;

    private static IReadOnlyList<TextSearchHit> MergeBm25Only(
        IReadOnlyList<Bm25OnlyRow> rows,
        string query,
        IReadOnlyList<TextSearchHit> ftsHits)
    {
        var foldedQuery = KeywordFold.Tokens(KeywordFold.Fold(query));
        var querySet = new HashSet<string>(foldedQuery, StringComparer.Ordinal);
        var fromTable = rows
            .Select(row =>
            {
                var tokens = KeywordFold.Tokens(KeywordFold.Fold(row.RawText));
                var overlap = tokens.Count(t => querySet.Contains(t));
                return new TextSearchHit(
                    row.ContentId,
                    row.Path,
                    Path.GetFileName(row.Path),
                    DateTimeOffset.UnixEpoch,
                    TextMatchReason.DenseAndFullText,
                    overlap,
                    RawNote: row.RawText,
                    GrantPath: string.Empty);
            })
            .OrderByDescending(h => h.Score)
            .Take(RecallIndexCaps.RawListCap)
            .ToList();

        var merged = new List<TextSearchHit>(ftsHits);
        merged.AddRange(fromTable);
        return merged
            .OrderByDescending(h => h.Score)
            .Take(RecallIndexCaps.RawListCap)
            .ToList();
    }
}
