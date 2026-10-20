using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public sealed class ScopeChangeApplier
{
    private readonly IIngestCursorStore cursors;
    private readonly IGrantedTextIngestService ingest;
    private readonly LocalOpLogWriter opLog;

    public ScopeChangeApplier(IIngestCursorStore cursors, IGrantedTextIngestService ingest, LocalOpLogWriter opLog)
    {
        this.cursors = cursors;
        this.ingest = ingest;
        this.opLog = opLog;
    }

    public async Task ApplyAsync(
        ScopeChange change,
        ScopeFolderGrant? grant,
        GrantedTextDocument? document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(change.Path))
        {
            return;
        }

        if (change.Kind == ScopeChangeKind.Deleted)
        {
            var row = cursors.GetFile(change.Path);
            if (row is not null && row.State == IngestCursorState.Missing)
            {
                return;
            }

            var chunks = cursors.ListChunks(change.Path);
            var live = chunks.Where(c => !c.Retired).ToList();
            cursors.MarkMissing(change.Path);
#pragma warning disable S3267
            foreach (var chunk in live.Where(chunk => !cursors.AnyLivePath(chunk.ContentId)))
#pragma warning restore S3267
            {
                opLog.AppendDelete(chunk.ContentId);
                cursors.EnqueuePending(chunk.ContentId);
            }

            return;
        }

        if (change.Kind == ScopeChangeKind.Renamed)
        {
            var previous = change.PreviousPath;
            if (string.IsNullOrWhiteSpace(previous))
            {
                if (grant is not null && document is not null)
                {
                    await ingest.IngestDocumentAsync(grant, document, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            var previousRow = cursors.GetFile(previous);
            if (previousRow is null)
            {
                if (grant is not null && document is not null)
                {
                    await ingest.IngestDocumentAsync(grant, document, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            if (document is null)
            {
                cursors.MarkMissing(previous);
                return;
            }

            var nextHash = ContentIdBuilder.HashSourceContent(document.Text);
            if (string.Equals(previousRow.ContentHash, nextHash, StringComparison.Ordinal))
            {
                cursors.RenamePath(previous, change.Path);
                return;
            }

            cursors.RetirePath(previous);

            if (grant is not null)
            {
                await ingest.IngestDocumentAsync(grant, document, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (change.Kind == ScopeChangeKind.Created || change.Kind == ScopeChangeKind.Modified)
        {
            if (document is null)
            {
                return;
            }

            if (grant is null)
            {
                return;
            }

            await ingest.IngestDocumentAsync(grant, document, cancellationToken).ConfigureAwait(false);
            return;
        }
    }
}
