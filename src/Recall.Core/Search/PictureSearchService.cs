using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Core.Search;

public sealed class PictureSearchService : IPictureSearchService
{
    private readonly IVisionEmbeddingEncoder encoder;
    private readonly IMediaVectorIndex index;
    private readonly IEncoderFileHash encoderHash;

    public PictureSearchService(
        IVisionEmbeddingEncoder encoder,
        IMediaVectorIndex index,
        IEncoderFileHash encoderHash)
    {
        this.encoder = encoder;
        this.index = index;
        this.encoderHash = encoderHash;
    }

    public Task<IReadOnlyList<MediaSearchHit>> FindAsync(
        ReadOnlyMemory<byte> jpeg,
        IReadOnlyList<ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default)
    {
        if (jpeg.IsEmpty || grants is null)
        {
            return Task.FromResult<IReadOnlyList<MediaSearchHit>>([]);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var grantedPaths = grants
            .Where(grant => grant.EnabledKinds.HasFlag(GrantFileKinds.Image) || grant.EnabledKinds.HasFlag(GrantFileKinds.Video))
            .Select(grant => grant.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hash = encoderHash.VisionHash ?? string.Empty;
        var filter = TextSearchFilter.Build(grantedPaths, hash);
        if (filter.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<MediaSearchHit>>([]);
        }

        var vector = new float[MediaCollections.Width];
        encoder.EmbedJpeg(jpeg, vector);

        var hits = new List<MediaSearchHit>();
        foreach (var collection in new[] { MediaCollections.Image, MediaCollections.Video })
        {
            var request = new MediaSearchRequest(collection, vector, filter, RecallIndexCaps.RawListCap);
            hits.AddRange(index.Search(request, cancellationToken));
        }

        return Task.FromResult<IReadOnlyList<MediaSearchHit>>(hits
            .GroupBy(hit => hit.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => Collapse.EarliestStart(group.ToList()))
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.StartSeconds)
            .Take(RecallIndexCaps.FileCap)
            .ToList());
    }
}
