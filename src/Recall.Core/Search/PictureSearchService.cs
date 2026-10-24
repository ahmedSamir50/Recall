using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Vectors;

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
        var vector512 = MatryoshkaVectorMath.CutAndNormalize(vector, MediaCollections.OldWidth);

        var lists = new List<IReadOnlyList<MediaSearchHit>>(MediaCollections.PictureCollections.Length);
        foreach (var collection in MediaCollections.PictureCollections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = MediaCollections.WidthFor(collection) == MediaCollections.OldWidth ? vector512 : vector;
            var request = new MediaSearchRequest(collection, query, filter, RecallIndexCaps.RawListCap);
            var hits = index.Search(request, cancellationToken);
            lists.Add(CollapseCollection(hits));
        }

        return Task.FromResult<IReadOnlyList<MediaSearchHit>>(AgeRrf.Fuse(lists).Take(RecallIndexCaps.FileCap).ToList());
    }

    private static IReadOnlyList<MediaSearchHit> CollapseCollection(IReadOnlyList<MediaSearchHit> hits) =>
        hits
            .GroupBy(hit => hit.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => Collapse.EarliestStart(group.ToList()))
            .ToList();
}
