namespace Recall.Core.Checkpoints;

public interface IIngestCursorStore
{
    IngestFileRow? GetFile(string path);

    IReadOnlyList<IngestFileRow> ListFiles(string grantPath);

    IReadOnlyList<IngestChunkRow> ListChunks(string path);

    IngestChunkRow? FindChunk(string contentId);

    void MarkReading(string path, string contentHash, string grantPath);

    void MarkFinished(string path, string contentHash, string grantPath, IReadOnlyList<(string ContentId, int Ordinal)> chunks);

    void RenamePath(string previousPath, string nextPath);

    void MarkMissing(string path);

    void RetirePath(string path);

    void MarkGrantReading(string grantPath);

    void MarkGrantFinished(string grantPath);

    bool IsGrantFinished(string grantPath);
}
