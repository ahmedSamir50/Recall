using Recall.Core.Checkpoints;
using Recall.Core.Download;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Runtime;
using Recall.Core.Scope;

namespace Recall.Maui.Ingest;

public sealed class IngestRetryHostedService : IDisposable
{
    private readonly IScopeManagerService scope;
    private readonly IIngestCursorStore cursors;
    private readonly IGrantedTextIngestService ingest;
    private readonly MemoryGate gate;
    private readonly IGpuTemperature gpu;
    private readonly RetryDelayPreferenceService retryDelay;
    private readonly IModelDownloadService? downloads;
    private CancellationTokenSource? loopCts;
    private Task? loopTask;

    public IngestRetryHostedService(
        IScopeManagerService scope,
        IIngestCursorStore cursors,
        IGrantedTextIngestService ingest,
        MemoryGate gate,
        IGpuTemperature gpu,
        RetryDelayPreferenceService retryDelay,
        IModelDownloadService? downloads = null)
    {
        this.scope = scope;
        this.cursors = cursors;
        this.ingest = ingest;
        this.gate = gate;
        this.gpu = gpu;
        this.retryDelay = retryDelay;
        this.downloads = downloads;
    }

    public void Start()
    {
        if (loopTask is not null)
        {
            return;
        }

        loopCts = new CancellationTokenSource();
        loopTask = Task.Run(() => RunAsync(loopCts.Token), loopCts.Token);
    }

    public void Stop()
    {
        loopCts?.Cancel();
        try
        {
#pragma warning disable S8949
#pragma warning disable VSTHRD002
            loopTask?.Wait(TimeSpan.FromSeconds(2));
#pragma warning restore VSTHRD002
#pragma warning restore S8949
        }
        catch
        {
            // Shutting down.
        }

        loopCts?.Dispose();
        loopCts = null;
        loopTask = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            Tick(DateTimeOffset.UtcNow);
        }
    }

    internal void Tick(DateTimeOffset now)
    {
        if (ingest.PassRunning)
        {
            return;
        }

        var minutes = retryDelay.Get();
        var lastRun = cursors.ReadLastRun();
        var next = lastRun.NextRetryUtc ?? IngestRetryClock.Next(now, minutes);
        var gpuHot = GpuCooldownPolicy.Blocks(gpu.ReadCelsius());
        var gateOpen = gate.IsOpen();
        var modelReady = downloads is null || downloads.GetTextDownloadSnapshot().Phase == TextModelDownloadPhase.Ready;

        var unfinished = ListUnfinishedPaths();
        if (unfinished.Count == 0)
        {
            return;
        }

        var eligible = unfinished.Where(path => IngestRetryPolicy.AllowAutomatic(CountFailures(path))).ToList();
        if (eligible.Count == 0)
        {
            return;
        }

        if (!IngestRetryClock.ShouldRun(now, next, gateOpen, gpuHot, modelReady))
        {
            var deferred = IngestRetryClock.Defer(now, minutes);
            cursors.AppendLog(new IngestLogEntry(now, IngestStopKeys.RetryDeferred, string.Empty));
            cursors.WriteLastRun(lastRun with { NextRetryUtc = deferred });
            return;
        }

        cursors.WriteLastRun(lastRun with { NextRetryUtc = IngestRetryClock.Next(now, minutes) });
        _ = ingest.IngestGrantedTextAsync(CancellationToken.None);
    }

    private List<string> ListUnfinishedPaths()
    {
        var paths = new List<string>();
        foreach (var grant in scope.ListGrants())
        {
            paths.AddRange(cursors.ListUnfinishedPaths(grant.Path));
        }

        return paths;
    }

    private int CountFailures(string path)
    {
        var fileName = Path.GetFileName(path);
        return cursors.ListLog().Count(entry =>
            string.Equals(entry.FileName, fileName, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(entry.ReasonKey, IngestStopKeys.RetryDeferred, StringComparison.Ordinal));
    }

    public void Dispose() => Stop();
}