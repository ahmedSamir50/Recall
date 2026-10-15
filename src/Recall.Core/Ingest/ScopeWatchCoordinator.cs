using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public sealed class ScopeWatchCoordinator : IScopeChangeSink, IDisposable
{
    private readonly IFileScopeWatcherService watcher;
    private readonly IScopeManagerService scope;
    private readonly ForegroundScanService scan;
    private readonly ScopeChangeApplier applier;
    private readonly IUserSkipStore skips;
    private readonly ScopeChangeBuffer buffer = new();
    private readonly SemaphoreSlim wake = new(0, 1);
    private CancellationTokenSource? loopCts;
    private Task? loopTask;
    private bool disposed;

    public ScopeWatchCoordinator(
        IFileScopeWatcherService watcher,
        IScopeManagerService scope,
        ForegroundScanService scan,
        ScopeChangeApplier applier,
        IUserSkipStore skips)
    {
        this.watcher = watcher;
        this.scope = scope;
        this.scan = scan;
        this.applier = applier;
        this.skips = skips;
    }

    public ScopeChangeBuffer Buffer => buffer;

    public bool IsRunning => loopTask is not null && !loopTask.IsCompleted;

    public void Report(ScopeChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        buffer.TryAdd(change);
        Signal();
    }

    public void RequestRescan()
    {
        buffer.RequestRescan();
        Signal();
    }

    public void OnCreated(RecallShellKind kind)
    {
        if (kind != RecallShellKind.Windows && kind != RecallShellKind.Android && kind != RecallShellKind.MacCatalyst && kind != RecallShellKind.Ios)
        {
            return;
        }

        RequestRescan();
        if (ScopeWatcherKindPolicy.Choose(kind) == ScopeWatcherKind.WindowsFileSystem)
        {
            watcher.Start(scope.ListGrants(), this);
        }

        StartLoop();
    }

    public void OnResumed(RecallShellKind kind)
    {
        if (kind != RecallShellKind.Windows && kind != RecallShellKind.Android && kind != RecallShellKind.MacCatalyst && kind != RecallShellKind.Ios)
        {
            return;
        }

        RequestRescan();
        if (!IsRunning)
        {
            StartLoop();
        }
    }

    public void Stop()
    {
        try
        {
            loopCts?.Cancel();
        }
        catch (Exception ex)
        {
            // Cancel can throw after dispose; stopping the watcher is still required.
            GC.KeepAlive(ex);
        }

        Signal();
        watcher.Stop();
    }

    public async Task PumpOnceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (buffer.NeedsRescan)
        {
            await scan.ScanAsync(cancellationToken).ConfigureAwait(false);
            buffer.AcknowledgeRescan();
        }

        var changes = buffer.Drain();
        foreach (var change in changes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await ApplySingleAsync(change, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One bad change must not stop the next change.
                GC.KeepAlive(ex);
                GC.KeepAlive(change);
            }
        }
    }

    private void Signal()
    {
        if (wake.CurrentCount == 0)
        {
            wake.Release();
        }
    }

    private async Task ApplySingleAsync(ScopeChange change, CancellationToken cancellationToken)
    {
        var grant = FindGrant(change.Path);
        if (grant is null && change.Kind == ScopeChangeKind.Renamed && !string.IsNullOrWhiteSpace(change.PreviousPath))
        {
            grant = FindGrant(change.PreviousPath);
        }

        GrantedTextDocument? document = null;
        if (grant is not null && !string.IsNullOrWhiteSpace(change.Path))
        {
            document = await FilesystemScopeFileReader.TryReadAsync(grant, change.Path, skips, cancellationToken).ConfigureAwait(false);
        }

        await applier.ApplyAsync(change, grant, document, cancellationToken).ConfigureAwait(false);
    }

    private ScopeFolderGrant? FindGrant(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var grant in scope.ListGrants())
        {
            if (grant is null || string.IsNullOrWhiteSpace(grant.Path))
            {
                continue;
            }

            if (ContentUri.IsContent(grant.Path))
            {
                if (string.Equals(grant.Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    return grant;
                }

                continue;
            }

            if (ScopeGrantPolicy.IsInsideGrant(grant.Path, path))
            {
                return grant;
            }
        }

        return null;
    }

    private void StartLoop()
    {
        if (IsRunning)
        {
            return;
        }

        loopCts?.Dispose();
        loopCts = new CancellationTokenSource();
        var token = loopCts.Token;
        loopTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await wake.WaitAsync(token).ConfigureAwait(false);
                    await PumpOnceAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Keep the watch loop alive; PumpOnce already isolates per-change faults.
                    GC.KeepAlive(ex);
                }
            }
        }, CancellationToken.None);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Stop();
        wake.Dispose();
        loopCts?.Dispose();
    }
}
