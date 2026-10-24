using Recall.Core.Ocr;
using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public sealed class IngestPassHooks
{
    public Action? OpenText { get; init; }

    public Action? CloseText { get; init; }

    public Func<string, CancellationToken, Task>? ProcessTextJobAsync { get; init; }

    public Action? OpenVision { get; init; }

    public Action? CloseVision { get; init; }

    public Action? OpenAudio { get; init; }

    public Action? CloseAudio { get; init; }

    public Func<string, CancellationToken, Task>? ProcessMediaJobAsync { get; init; }

    public Action<string>? ProcessOcrJob { get; init; }

    public Action? EnsureMediaContextProbe { get; init; }

    public Action<IngestMediaLeg>? BeginMediaLeg { get; init; }

    public Action<DateTimeOffset>? SweepAgedMedia { get; init; }
}

public sealed class IngestSchedulerRunner
{
    private readonly IngestScheduler scheduler;

    public IngestSchedulerRunner(IngestScheduler scheduler)
    {
        this.scheduler = scheduler;
    }

    public async Task RunReadyPassesAsync(DateTimeOffset now, IngestPassHooks hooks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hooks);
        cancellationToken.ThrowIfCancellationRequested();
        await RunTextPassAsync(now, hooks, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await RunMediaPassAsync(now, hooks, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        RunOcrPass(now, hooks, cancellationToken);
    }

    private async Task RunTextPassAsync(DateTimeOffset now, IngestPassHooks hooks, CancellationToken cancellationToken)
    {
        if (!scheduler.ShouldOpen(IngestModel.Text, now))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var opened = false;
        try
        {
            if (hooks.OpenText is not null)
            {
                hooks.OpenText();
                opened = true;
            }

            while (scheduler.QueueCount(IngestModel.Text) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!scheduler.TryDequeue(IngestModel.Text, out var job))
                {
                    break;
                }

                if (hooks.ProcessTextJobAsync is not null)
                {
                    await hooks.ProcessTextJobAsync(job.Path, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            if (opened)
            {
                hooks.CloseText?.Invoke();
            }
        }
    }

#pragma warning disable S3267 // foreach is clearer than LINQ for ordered media passes
    private async Task RunMediaPassAsync(DateTimeOffset now, IngestPassHooks hooks, CancellationToken cancellationToken)
    {
        if (!scheduler.ShouldOpen(IngestModel.Media, now))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        hooks.SweepAgedMedia?.Invoke(now);
        hooks.EnsureMediaContextProbe?.Invoke();

        var jobs = new List<string>();
        while (scheduler.QueueCount(IngestModel.Media) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!scheduler.TryDequeue(IngestModel.Media, out var job))
            {
                break;
            }

            jobs.Add(job.Path);
        }

        if (jobs.Count == 0)
        {
            return;
        }

        var visionJobs = new List<string>();
        var audioJobs = new List<string>();
        foreach (var path in jobs)
        {
            if (MediaFileKinds.IsImage(path))
            {
                visionJobs.Add(path);
            }
            else if (MediaFileKinds.IsVideo(path))
            {
                visionJobs.Add(path);
                audioJobs.Add(path);
            }
            else if (MediaFileKinds.IsAudio(path))
            {
                audioJobs.Add(path);
            }
            else
            {
                visionJobs.Add(path);
            }
        }

        var oneContext = MediaContextPolicy.OneContext;
        var visionOpen = false;
        var audioOpen = false;
        try
        {
            if (oneContext)
            {
                hooks.BeginMediaLeg?.Invoke(IngestMediaLeg.Shared);
                if (hooks.OpenVision is not null)
                {
                    hooks.OpenVision();
                    visionOpen = true;
                }

                await ProcessMediaAsync(hooks, jobs, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (visionJobs.Count > 0)
                {
                    hooks.BeginMediaLeg?.Invoke(IngestMediaLeg.Vision);
                    if (hooks.OpenVision is not null)
                    {
                        hooks.OpenVision();
                        visionOpen = true;
                    }

                    await ProcessMediaAsync(hooks, visionJobs, cancellationToken).ConfigureAwait(false);
                    if (visionOpen)
                    {
                        hooks.CloseVision?.Invoke();
                        visionOpen = false;
                    }
                }

                if (audioJobs.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    hooks.BeginMediaLeg?.Invoke(IngestMediaLeg.Audio);
                    if (hooks.OpenAudio is not null)
                    {
                        hooks.OpenAudio();
                        audioOpen = true;
                    }

                    await ProcessMediaAsync(hooks, audioJobs, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            foreach (var path in jobs)
            {
                scheduler.Enqueue(new IngestJob(IngestModel.Media, path));
            }

            throw;
        }
        finally
        {
            if (audioOpen)
            {
                hooks.CloseAudio?.Invoke();
            }

            if (visionOpen)
            {
                hooks.CloseVision?.Invoke();
            }
        }
    }

    private static async Task ProcessMediaAsync(IngestPassHooks hooks, List<string> paths, CancellationToken cancellationToken)
    {
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (hooks.ProcessMediaJobAsync is not null)
            {
                await hooks.ProcessMediaJobAsync(path, cancellationToken).ConfigureAwait(false);
            }
        }
    }
#pragma warning restore S3267

    private void RunOcrPass(DateTimeOffset now, IngestPassHooks hooks, CancellationToken cancellationToken)
    {
        if (!scheduler.ShouldOpen(IngestModel.Ocr, now))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var jobs = new List<string>();
        while (scheduler.QueueCount(IngestModel.Ocr) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!scheduler.TryDequeue(IngestModel.Ocr, out var job))
            {
                break;
            }

            jobs.Add(job.Path);
        }

        if (jobs.Count == 0)
        {
            return;
        }

        var index = 0;
        try
        {
            OcrPassStub.Run(() =>
            {
                for (; index < jobs.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    hooks.ProcessOcrJob?.Invoke(jobs[index]);
                }
            });
        }
        catch (OperationCanceledException)
        {
            for (var pending = index; pending < jobs.Count; pending++)
            {
                scheduler.Enqueue(new IngestJob(IngestModel.Ocr, jobs[pending]));
            }

            throw;
        }
    }

}
