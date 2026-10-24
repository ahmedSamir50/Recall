using Recall.Core.Indexing;
using Recall.Core.Search;

namespace Recall.Core.Ports;

public interface IMediaVectorIndex
{
    void Create(string collectionName, int width);

    bool TryInsert(string collectionName, MediaIndexInsert insert);

    bool TryDelete(string contentId);

    void Reconcile(IReadOnlyList<VectorLiveState> states);

    IReadOnlyList<MediaSearchHit> Search(MediaSearchRequest request, CancellationToken cancellationToken = default);

    IReadOnlyList<MediaSearchHit> SearchWords(string collectionName, string query, string filter, int topK, CancellationToken cancellationToken = default);
}

public interface IVisionEmbeddingEncoder
{
    void EmbedJpeg(ReadOnlyMemory<byte> jpeg, float[] destination768);
}

public interface IAudioEmbeddingEncoder
{
    bool IsLoaded { get; }

    void EmbedPcm(ReadOnlyMemory<float> mono16k, float[] destination768);

    void CloseAudio();
}

public interface IPictureSearchService
{
    Task<IReadOnlyList<MediaSearchHit>> FindAsync(
        ReadOnlyMemory<byte> jpeg,
        IReadOnlyList<Scope.ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default);
}

public interface IAudioSearchService
{
    Task<IReadOnlyList<MediaSearchHit>> FindAsync(
        ReadOnlyMemory<float> mono16k,
        IReadOnlyList<Scope.ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default);
}

public interface IMediaPreview
{
    Task PlayAsync(string path, double startSeconds, CancellationToken cancellationToken = default);
}
