using System.Diagnostics;
using Recall.Core.Faults;
using Recall.Core.Ports;
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

    public Func<IReadOnlyList<string>, IOcrEngine>? RunOcr { get; init; }

    public Action? EnsureMediaContextProbe { get; init; }

    public Action<IngestMediaLeg>? BeginMediaLeg { get; init; }

    public Action<DateTimeOffset>? SweepAgedMedia { get; init; }
}

public sealed class IngestSchedulerRunner
{
    public const int TextSlice = 64;

    public const int MediaSlice = 32;

    public const int OcrSlice = 64;

    public static readonly TimeSpan PassBudget = TimeSpan.FromSeconds(120);

    private readonly IngestScheduler scheduler;
    private readonly IngestPassMetrics metrics;
    private readonly IIngestPreemption preemption;
    private readonly TimeProvider time;

    public IngestSchedulerRunner(
        IngestScheduler scheduler,
        IngestPassMetrics? metrics = null,
        IIngestPreemption? preemption = null,
        TimeProvider? time = null)
    {
        this.scheduler = scheduler;
        this.metrics = metrics ?? new IngestPassMetrics();
        this.preemption = preemption ?? new IngestPreemption();
        this.time = time ?? TimeProvider.System;
    }

    public async Task RunReadyPassesAsync(DateTimeOffset now, IngestPassHooks hooks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hooks);
        cancellationToken.ThrowIfCancellationRequested();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, preemption.Token);
        var passToken = linked.Token;
        var pass = new PassState(this.time.GetTimestamp());

        while (!BudgetSpent(pass))
        {
            var worked = false;
            var text = await RunTextSliceAsync(now, hooks, pass, passToken, cancellationToken).ConfigureAwait(false);
            worked |= text.Worked;
            if (text.Stop)
            {
                return;
            }

            var media = await RunMediaSliceAsync(now, hooks, pass, passToken, cancellationToken).ConfigureAwait(false);
            worked |= media.Worked;
            if (media.Stop)
            {
                return;
            }

            var ocr = await RunOcrSliceAsync(now, hooks, pass, passToken, cancellationToken).ConfigureAwait(false);
            worked |= ocr.Worked;
            if (ocr.Stop)
            {
                return;
            }

            if (!worked)
            {
                return;
            }
        }
    }

    private bool BudgetSpent(PassState pass) =>
        this.time.GetElapsedTime(pass.BudgetStart) >= PassBudget;

    private async Task<SliceOutcome> RunTextSliceAsync(
        DateTimeOffset now,
        IngestPassHooks hooks,
        PassState pass,
        CancellationToken passToken,
        CancellationToken cancellationToken)
    {
        if (!scheduler.ShouldOpen(IngestModel.Text, now))
        {
            return default;
        }

        var jobs = new List<string>();
        var processed = 0;
        var openWatch = new Stopwatch();
        var totalWatch = Stopwatch.StartNew();
        var opened = false;
        try
        {
            while (jobs.Count < TextSlice && !BudgetSpent(pass) && scheduler.TryDequeue(IngestModel.Text, out var job))
            {
                jobs.Add(job.Path);
            }

            if (jobs.Count == 0)
            {
                return default;
            }

            if (hooks.OpenText is not null)
            {
                openWatch.Start();
                hooks.OpenText();
                openWatch.Stop();
                opened = true;
            }

            foreach (var path in jobs)
            {
                if (BudgetSpent(pass))
                {
                    break;
                }

                passToken.ThrowIfCancellationRequested();
                if (hooks.ProcessTextJobAsync is not null)
                {
                    await hooks.ProcessTextJobAsync(path, passToken).ConfigureAwait(false);
                }

                processed++;
            }

            Requeue(IngestModel.Text, jobs.Skip(processed));
            return new SliceOutcome(processed > 0, false);
        }
        catch (OperationCanceledException)
        {
            Requeue(IngestModel.Text, jobs.Skip(processed));
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return new SliceOutcome(processed > 0, true);
        }
        finally
        {
            if (opened)
            {
                hooks.CloseText?.Invoke();
            }

            openWatch.Stop();
            totalWatch.Stop();
            metrics.Record(new IngestPassMetric(IngestModel.Text, processed, openWatch.ElapsedMilliseconds, totalWatch.ElapsedMilliseconds));
        }
    }

#pragma warning disable S3267 // foreach is clearer than LINQ for ordered media passes
    private async Task<SliceOutcome> RunMediaSliceAsync(
        DateTimeOffset now,
        IngestPassHooks hooks,
        PassState pass,
        CancellationToken passToken,
        CancellationToken cancellationToken)
    {
        if (!scheduler.ShouldOpen(IngestModel.Media, now))
        {
            return default;
        }

        if (!pass.MediaSwept)
        {
            pass.MediaSwept = true;
            hooks.SweepAgedMedia?.Invoke(now);
            hooks.EnsureMediaContextProbe?.Invoke();
        }

        var jobs = new List<string>();
        while (jobs.Count < MediaSlice && !BudgetSpent(pass) && scheduler.TryDequeue(IngestModel.Media, out var job))
        {
            jobs.Add(job.Path);
        }

        if (jobs.Count == 0)
        {
            return default;
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

        var pending = new List<string>(jobs);
        var oneContext = MediaContextPolicy.OneContext;
        var visionOpen = false;
        var audioOpen = false;
        var openWatch = new Stopwatch();
        var totalWatch = Stopwatch.StartNew();
        try
        {
            if (oneContext)
            {
                hooks.BeginMediaLeg?.Invoke(IngestMediaLeg.Shared);
                if (hooks.OpenVision is not null)
                {
                    openWatch.Start();
                    hooks.OpenVision();
                    openWatch.Stop();
                    visionOpen = true;
                }

                await ProcessMediaAsync(hooks, jobs, pending, pass, passToken).ConfigureAwait(false);
            }
            else
            {
                if (visionJobs.Count > 0)
                {
                    hooks.BeginMediaLeg?.Invoke(IngestMediaLeg.Vision);
                    if (hooks.OpenVision is not null)
                    {
                        openWatch.Start();
                        hooks.OpenVision();
                        openWatch.Stop();
                        visionOpen = true;
                    }

                    await ProcessMediaAsync(hooks, visionJobs, pending, pass, passToken).ConfigureAwait(false);
                    if (visionOpen)
                    {
                        hooks.CloseVision?.Invoke();
                        visionOpen = false;
                    }
                }

                if (audioJobs.Count > 0)
                {
                    passToken.ThrowIfCancellationRequested();
                    hooks.BeginMediaLeg?.Invoke(IngestMediaLeg.Audio);
                    if (hooks.OpenAudio is not null)
                    {
                        openWatch.Start();
                        hooks.OpenAudio();
                        openWatch.Stop();
                        audioOpen = true;
                    }

                    await ProcessMediaAsync(hooks, audioJobs, pending, pass, passToken).ConfigureAwait(false);
                }
            }

            Requeue(IngestModel.Media, pending);
            return new SliceOutcome(jobs.Count - pending.Count > 0, false);
        }
        catch (OperationCanceledException)
        {
            Requeue(IngestModel.Media, pending);
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return new SliceOutcome(jobs.Count - pending.Count > 0, true);
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

            openWatch.Stop();
            totalWatch.Stop();
            metrics.Record(new IngestPassMetric(IngestModel.Media, jobs.Count - pending.Count, openWatch.ElapsedMilliseconds, totalWatch.ElapsedMilliseconds));
        }
    }

    private async Task ProcessMediaAsync(IngestPassHooks hooks, List<string> paths, List<string> pending, PassState pass, CancellationToken cancellationToken)
    {
        foreach (var path in paths)
        {
            if (BudgetSpent(pass))
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (hooks.ProcessMediaJobAsync is not null)
            {
                await hooks.ProcessMediaJobAsync(path, cancellationToken).ConfigureAwait(false);
            }

            pending.Remove(path);
        }
    }
#pragma warning restore S3267

    private async Task<SliceOutcome> RunOcrSliceAsync(
        DateTimeOffset now,
        IngestPassHooks hooks,
        PassState pass,
        CancellationToken passToken,
        CancellationToken cancellationToken)
    {
        if (!scheduler.ShouldOpen(IngestModel.Ocr, now))
        {
            return default;
        }

        var runOcr = hooks.RunOcr;
        if (runOcr is null)
        {
            return default;
        }

        var jobs = new List<string>();
        var pending = new List<string>();
        IOcrEngine? engine = null;
        var openWatch = new Stopwatch();
        var totalWatch = Stopwatch.StartNew();
        try
        {
            while (jobs.Count < OcrSlice && !BudgetSpent(pass) && scheduler.TryDequeue(IngestModel.Ocr, out var job))
            {
                jobs.Add(job.Path);
            }

            if (jobs.Count == 0)
            {
                return default;
            }

            pending.AddRange(jobs);
            openWatch.Start();
            engine = runOcr(jobs);
            openWatch.Stop();
            await engine.RunPassAsync(jobs, passToken).ConfigureAwait(false);
            pending.Clear();
            return new SliceOutcome(true, false);
        }
        catch (OperationCanceledException)
        {
            Requeue(IngestModel.Ocr, pending);
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return new SliceOutcome(jobs.Count - pending.Count > 0, true);
        }
        catch (RecallFaultException)
        {
            Requeue(IngestModel.Ocr, pending);
            return new SliceOutcome(false, false);
        }
        finally
        {
            engine?.Dispose();
            openWatch.Stop();
            totalWatch.Stop();
            metrics.Record(new IngestPassMetric(IngestModel.Ocr, jobs.Count - pending.Count, openWatch.ElapsedMilliseconds, totalWatch.ElapsedMilliseconds));
        }
    }

    private void Requeue(IngestModel model, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            scheduler.Enqueue(new IngestJob(model, path));
        }
    }

    private sealed class PassState
    {
        public PassState(long budgetStart)
        {
            BudgetStart = budgetStart;
        }

        public long BudgetStart { get; }

        public bool MediaSwept { get; set; }
    }

    private readonly record struct SliceOutcome(bool Worked, bool Stop);
}


