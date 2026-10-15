using Recall.Core.Ingest;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Vocabulary;

namespace Recall.Core.Ports;

public interface IIndexPolicy
{
    StoredWidth TextStoredWidth { get; }

    void EnsureStorableWidth(Modality modality, int width);
}

public interface ITextEmbeddingEncoder
{
    ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default);
}

public interface ITextVectorIndex
{
    bool TryInsert(TextIndexInsert insert);

    IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IGrantedTextIngestService
{
    Task IngestGrantedTextAsync(CancellationToken cancellationToken = default);

    Task IngestGrantAsync(string path, CancellationToken cancellationToken = default);

    Task IngestDocumentAsync(ScopeFolderGrant grant, GrantedTextDocument document, CancellationToken cancellationToken = default);
}

public interface IIndexNowService
{
    IReadOnlyList<ScopeFolderGrant> ListOffers();

    Task IndexAsync(string path, CancellationToken cancellationToken = default);
}

public interface ITextSearchService
{
    Task<IReadOnlyList<TextSearchHit>> FindAsync(string query, IReadOnlyList<ScopeFolderGrant> grants, CancellationToken cancellationToken = default);
}

public interface ILocalFileOpener
{
    bool Exists(string sourcePath);

    Task<LocalFileOpenResult> OpenAsync(string sourcePath, CancellationToken cancellationToken = default);
}
