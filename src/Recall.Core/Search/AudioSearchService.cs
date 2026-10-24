using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Vectors;

namespace Recall.Core.Search;

public sealed class AudioSearchService : IAudioSearchService
{
    private readonly IAudioEmbeddingEncoder encoder;
    private readonly IMediaVectorIndex index;
    private readonly IEncoderFileHash encoderHash;

    public AudioSearchService(
        IAudioEmbeddingEncoder encoder,
        IMediaVectorIndex index,
        IEncoderFileHash encoderHash)
    {
        this.encoder = encoder;
        this.index = index;
        this.encoderHash = encoderHash;
    }

    public async Task<IReadOnlyList<MediaSearchHit>> FindAsync(
        ReadOnlyMemory<float> mono16k,
        IReadOnlyList<ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default)
    {
        if (mono16k.IsEmpty || grants is null)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();
        var grantedPaths = grants
            .Where(grant => grant.EnabledKinds.HasFlag(GrantFileKinds.Audio))
            .Select(grant => grant.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (grantedPaths.Count == 0)
        {
            return [];
        }

        var filter = TextSearchFilter.Build(grantedPaths, encoderHash.AudioHash ?? string.Empty);
        if (filter.Length == 0)
        {
            return [];
        }

        var vector = new float[MediaCollections.Width];
        await Task.Run(() => encoder.EmbedPcm(mono16k, vector), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var vector512 = MatryoshkaVectorMath.CutAndNormalize(vector, MediaCollections.OldWidth);

        var lists = new List<IReadOnlyList<MediaSearchHit>>(MediaCollections.AudioCollections.Length);
        foreach (var collection in MediaCollections.AudioCollections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = MediaCollections.WidthFor(collection) == MediaCollections.OldWidth ? vector512 : vector;
            var request = new MediaSearchRequest(collection, query, filter, RecallIndexCaps.RawListCap);
            var hits = index.Search(request, cancellationToken)
                .Where(hit => IsInsideGrant(hit.SourcePath, grantedPaths))
                .ToList();
            lists.Add(CollapseCollection(hits));
        }

        return AgeRrf.Fuse(lists).Take(RecallIndexCaps.FileCap).ToList();
    }

    private static bool IsInsideGrant(string path, IReadOnlySet<string> grantedPaths) =>
        grantedPaths.Any(grantPath => ScopeGrantPolicy.IsInsideGrant(grantPath, path));

    private static IReadOnlyList<MediaSearchHit> CollapseCollection(IReadOnlyList<MediaSearchHit> hits) =>
        hits
            .GroupBy(hit => hit.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => Collapse.EarliestStart(group.ToList()))
            .ToList();
}
