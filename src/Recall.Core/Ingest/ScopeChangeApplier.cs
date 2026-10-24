using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public sealed class ScopeChangeApplier
{
    private readonly IIngestCursorStore cursors;
    private readonly IGrantedTextIngestService ingest;
    private readonly LocalOpLogWriter opLog;
    private readonly IAudioFileIngest? audio;
    private readonly IngestScheduler? scheduler;
    private readonly IUserSkipStore? skips;
    private readonly IVisionFileIngest? vision;

    public ScopeChangeApplier(
        IIngestCursorStore cursors,
        IGrantedTextIngestService ingest,
        LocalOpLogWriter opLog,
        IAudioFileIngest? audio = null,
        IngestScheduler? scheduler = null,
        IUserSkipStore? skips = null,
        IVisionFileIngest? vision = null)
    {
        this.cursors = cursors;
        this.ingest = ingest;
        this.opLog = opLog;
        this.audio = audio;
        this.scheduler = scheduler;
        this.skips = skips;
        this.vision = vision;
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

        if (MediaFileKinds.IsAudio(change.Path) || MediaFileKinds.IsVideo(change.Path) || MediaFileKinds.IsImage(change.Path))
        {
            if (change.Kind == ScopeChangeKind.Renamed)
            {
                RetireOldMediaPath(change.PreviousPath);
            }

            if (grant is not null
                && change.Kind is ScopeChangeKind.Created or ScopeChangeKind.Modified or ScopeChangeKind.Renamed)
            {
                await IndexMediaChangeAsync(grant, change.Path, cancellationToken).ConfigureAwait(false);
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
                    await EnqueueOrIngestTextAsync(grant, document, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            var previousRow = cursors.GetFile(previous);
            if (previousRow is null)
            {
                if (grant is not null && document is not null)
                {
                    await EnqueueOrIngestTextAsync(grant, document, cancellationToken).ConfigureAwait(false);
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
                await EnqueueOrIngestTextAsync(grant, document, cancellationToken).ConfigureAwait(false);
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

            await EnqueueOrIngestTextAsync(grant, document, cancellationToken).ConfigureAwait(false);
            return;
        }
    }

    private async Task IndexMediaChangeAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(path);
        var user = skips?.Load() ?? UserSkipAdditions.Empty;
        if (MediaIngestFilter.UserSkipped(fileName, user))
        {
            return;
        }

        var allowed = MediaIngestFilter.KindAllowed(grant.EnabledKinds, fileName);
        if (!allowed)
        {
            if (audio is not null && (MediaFileKinds.IsAudio(path) || MediaFileKinds.IsVideo(path)) && !grant.EnabledKinds.HasFlag(GrantFileKinds.Audio))
            {
                await audio.IndexAsync(grant, path, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (scheduler is not null)
        {
            scheduler.Enqueue(new IngestJob(IngestModel.Media, path));
            return;
        }

        if (MediaFileKinds.IsImage(path))
        {
            if (vision is not null)
            {
                await vision.IndexImageAsync(grant, path, cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        if (audio is not null && (MediaFileKinds.IsAudio(path) || MediaFileKinds.IsVideo(path)))
        {
            await audio.IndexAsync(grant, path, cancellationToken).ConfigureAwait(false);
        }

        if (vision is not null && MediaFileKinds.IsVideo(path))
        {
            await vision.IndexVideoFramesAsync(grant, path, cancellationToken).ConfigureAwait(false);
        }
    }

    private void RetireOldMediaPath(string? previous)
    {
        if (string.IsNullOrWhiteSpace(previous))
        {
            return;
        }

        var live = cursors.ListChunks(previous).Where(chunk => !chunk.Retired).ToList();
        cursors.RetirePath(previous);
        cursors.MarkMissing(previous);
#pragma warning disable S3267
        foreach (var chunk in live.Where(chunk => !cursors.AnyLivePath(chunk.ContentId)))
#pragma warning restore S3267
        {
            opLog.AppendDelete(chunk.ContentId);
            cursors.EnqueuePending(chunk.ContentId);
        }
    }

    private async Task EnqueueOrIngestTextAsync(ScopeFolderGrant grant, GrantedTextDocument document, CancellationToken cancellationToken)
    {
        if (scheduler is not null)
        {
            scheduler.Enqueue(new IngestJob(IngestModel.Text, document.Path));
            return;
        }

        await ingest.IngestDocumentAsync(grant, document, cancellationToken).ConfigureAwait(false);
    }
}
