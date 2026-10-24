using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;

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

        var request = new MediaSearchRequest(MediaCollections.Audio, vector, filter, RecallIndexCaps.RawListCap);
        var hits = index.Search(request, cancellationToken);
        return hits
            .Where(hit => IsInsideGrant(hit.SourcePath, grantedPaths))
            .GroupBy(hit => hit.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => Collapse.EarliestStart(group.ToList()))
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.StartSeconds)
            .Take(RecallIndexCaps.FileCap)
            .ToList();
    }

    private static bool IsInsideGrant(string path, IReadOnlySet<string> grantedPaths) =>
        grantedPaths.Any(grantPath => ScopeGrantPolicy.IsInsideGrant(grantPath, path));
}
