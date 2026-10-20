using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public sealed class GrantedTextIngestService : IGrantedTextIngestService
{
    private readonly IScopeManagerService scope;
    private readonly IGrantedTextReader reader;
    private readonly TextIngestEngine engine;
    private readonly IIngestCursorStore cursors;
    private readonly ITextVectorIndex index;
    private readonly LocalOpLogWriter opLog;
    private readonly IEncoderFileHash encoderFileHash;

    public GrantedTextIngestService(
        IScopeManagerService scope,
        IGrantedTextReader reader,
        TextIngestEngine engine,
        IIngestCursorStore cursors,
        ITextVectorIndex index,
        LocalOpLogWriter opLog,
        IEncoderFileHash encoderFileHash)
    {
        this.scope = scope;
        this.reader = reader;
        this.engine = engine;
        this.cursors = cursors;
        this.index = index;
        this.opLog = opLog;
        this.encoderFileHash = encoderFileHash;
    }

    public async Task IngestGrantedTextAsync(CancellationToken cancellationToken = default)
    {
        foreach (var grant in scope.ListGrants())
        {
            await IngestGrantAsync(grant.Path, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task IngestGrantAsync(string path, CancellationToken cancellationToken = default)
    {
        var grant = scope.ListGrants()
            .FirstOrDefault(g => ScopeGrantPolicy.Normalize(g.Path).Equals(ScopeGrantPolicy.Normalize(path), StringComparison.OrdinalIgnoreCase));
        if (grant is null)
        {
            return;
        }

        if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Text) && !grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
        {
            return;
        }

        cursors.MarkGrantReading(grant.Path);
        await foreach (var document in reader.ReadAsync(grant, cancellationToken).ConfigureAwait(false))
        {
            await IngestDocumentAsync(grant, document, cancellationToken).ConfigureAwait(false);
        }

        cursors.MarkGrantFinished(grant.Path);
    }

    public async Task IngestDocumentAsync(ScopeFolderGrant grant, GrantedTextDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        ArgumentNullException.ThrowIfNull(document);
        var contentHash = ContentIdBuilder.HashSourceContent(document.Text);
        var row = cursors.GetFile(document.Path);
        var encoderHash = encoderFileHash.TextHash ?? string.Empty;
        if (EncoderRepair.NeedsRepair(row?.EncoderHash, encoderHash))
        {
            cursors.MarkReading(document.Path, contentHash, grant.Path);
#pragma warning disable S3267
            foreach (var chunk in cursors.ListChunks(document.Path).Where(static c => !c.Retired))
#pragma warning restore S3267
            {
                if (index.TryDelete(chunk.ContentId))
                {
                    opLog.AppendDelete(chunk.ContentId);
                }
            }
        }
        else if (IngestCursorPolicy.Decide(row, contentHash) == IngestCursorDecision.Skip)
        {
            return;
        }

        cursors.MarkReading(document.Path, contentHash, grant.Path);
        var codeFile = FileAdmission.IsCodeFile(document.FileName);
        var result = await engine.IngestFileAsync(
            document.Path,
            document.Text,
            document.ModifiedAt,
            grant.Path,
            encoderHash,
            codeFile,
            cancellationToken).ConfigureAwait(false);

        var writes = result.Chunks
            .Select(c => new ChunkWrite(c.ContentId, c.Ordinal))
            .ToList();
        foreach (var chunk in result.Chunks.Where(c => !c.Dense))
        {
            cursors.PutBm25Only(chunk.ContentId, document.Path, document.Text);
        }

        cursors.FinishPath(document.Path, result.ContentHash, grant.Path, encoderHash, writes);
    }
}
