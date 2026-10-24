using Recall.Core.Checkpoints;
using Recall.Core.Ocr;
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
    private readonly IAudioFileIngest? audio;
    private readonly IngestScheduler? scheduler;

    public GrantedTextIngestService(
        IScopeManagerService scope,
        IGrantedTextReader reader,
        TextIngestEngine engine,
        IIngestCursorStore cursors,
        ITextVectorIndex index,
        LocalOpLogWriter opLog,
        IEncoderFileHash encoderFileHash,
        IAudioFileIngest? audio = null,
        IngestScheduler? scheduler = null)
    {
        this.scope = scope;
        this.reader = reader;
        this.engine = engine;
        this.cursors = cursors;
        this.index = index;
        this.opLog = opLog;
        this.encoderFileHash = encoderFileHash;
        this.audio = audio;
        this.scheduler = scheduler;
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

        var textOrCode = grant.EnabledKinds.HasFlag(GrantFileKinds.Text) || grant.EnabledKinds.HasFlag(GrantFileKinds.Code);
        var media = grant.EnabledKinds.HasFlag(GrantFileKinds.Audio) || grant.EnabledKinds.HasFlag(GrantFileKinds.Video);
        if (!textOrCode && !media)
        {
            return;
        }

        if (textOrCode)
        {
            cursors.MarkGrantReading(grant.Path);
            await foreach (var document in reader.ReadAsync(grant, cancellationToken).ConfigureAwait(false))
            {
                await IngestDocumentAsync(grant, document, cancellationToken).ConfigureAwait(false);
            }

            cursors.MarkGrantFinished(grant.Path);
        }

        if (media && audio is not null)
        {
            await audio.IndexGrantAsync(grant, cancellationToken).ConfigureAwait(false);
        }
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
            var repairChunks = cursors.ListChunks(document.Path).Where(static c => !c.Retired).ToList();
#pragma warning restore S3267
            cursors.RetirePath(document.Path);
#pragma warning disable S3267
            foreach (var chunk in repairChunks)
#pragma warning restore S3267
            {
                if (cursors.AnyOtherLivePath(chunk.ContentId, document.Path))
                {
                    cursors.EnqueuePending(chunk.ContentId);
                    continue;
                }

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
            .Select(c => new ChunkWrite(c.ContentId, c.Ordinal, c.PageStart, c.PageEnd, c.CharStart, c.CharEnd))
            .ToList();
        foreach (var chunk in result.Chunks.Where(c => !c.Dense))
        {
            cursors.PutBm25Only(chunk.ContentId, document.Path, document.Text);
        }

        cursors.FinishPath(document.Path, result.ContentHash, grant.Path, encoderHash, writes);
        if (result.OcrPageNumbers.Count > 0)
        {
            OcrPageWaitlist.Apply(document.Path, result.OcrPageNumbers, cursors, scheduler);
        }

        foreach (var retiredId in cursors.ListRetiredIds(document.Path))
        {
            if (cursors.AnyLivePath(retiredId))
            {
                continue;
            }

            if (index.TryDelete(retiredId))
            {
                opLog.AppendDelete(retiredId);
                cursors.EnqueuePending(retiredId);
            }
        }
    }
}
