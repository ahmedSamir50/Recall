namespace Recall.Core.Checkpoints;

public interface IIngestCursorStore
{
    IngestFileRow? GetFile(string path);

    IReadOnlyList<IngestFileRow> ListFiles(string grantPath);

    IReadOnlyList<IngestChunkRow> ListChunks(string path);

    IngestChunkRow? FindChunk(string contentId);

    void MarkReading(string path, string contentHash, string grantPath);

    void MarkFinished(string path, string contentHash, string grantPath, string encoderHash);

    void RenamePath(string previousPath, string nextPath);

    void MarkMissing(string path);

    void RetirePath(string path);

    void MarkGrantReading(string grantPath);

    void MarkGrantFinished(string grantPath);

    bool IsGrantFinished(string grantPath);

    bool AnyLivePath(string contentId);

    bool AnyOtherLivePath(string contentId, string path);

    IReadOnlyList<string> ListRetiredIds(string path);

    void AddAlias(string contentId, string path);

    void MoveLiveAliases(string fromContentId, string toContentId);

    void PutBm25Only(string contentId, string path, string rawText);

    IReadOnlyList<Bm25OnlyRow> ListBm25Only();

    IReadOnlyList<IngestAliasRow> ListLiveAliases(string contentId);

    IReadOnlyList<string> ListPending(int limit);

    int PendingCount();

    void RemovePending(IReadOnlyList<string> contentIds);

    void EnqueuePending(string contentId);

    void ResetTextIngest();

    bool TryEnterWrite(out CheckpointLease? lease);

    CheckpointLease EnterWrite();

    void FinishPath(string path, string contentHash, string grantPath, string encoderHash, IReadOnlyList<ChunkWrite> chunks);
}
