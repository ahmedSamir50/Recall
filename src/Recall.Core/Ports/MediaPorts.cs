using Recall.Core.Indexing;
using Recall.Core.Search;

namespace Recall.Core.Ports;

public interface IMediaVectorIndex
{
    void Create(string collectionName, int width);

    bool TryInsert(string collectionName, MediaIndexInsert insert);

    bool TryDelete(string contentId);

    IReadOnlyList<MediaSearchHit> Search(MediaSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IVisionEmbeddingEncoder
{
    void EmbedJpeg(ReadOnlyMemory<byte> jpeg, float[] destination768);
}

public interface IPictureSearchService
{
    Task<IReadOnlyList<MediaSearchHit>> FindAsync(
        ReadOnlyMemory<byte> jpeg,
        IReadOnlyList<Scope.ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default);
}
