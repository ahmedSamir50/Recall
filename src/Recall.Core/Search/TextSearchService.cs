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

    public TextSearchService(
        ITextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        IIndexPolicy indexPolicy,
        IIngestCursorStore cursors,
        IEncoderFileHash encoderHash)
    {
        this.encoder = encoder;
        this.index = index;
        this.indexPolicy = indexPolicy;
        this.cursors = cursors;
        this.encoderHash = encoderHash;
    }

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
        if (grantedPaths.Count == 0)
        {
            return [];
        }

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

    private static HashSet<string> BuildGrantedPathSet(IReadOnlyList<ScopeFolderGrant> grants)
    {
        return grants
            .Where(grant => grant.EnabledKinds.HasFlag(GrantFileKinds.Text) || grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
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
