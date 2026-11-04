using FluentAssertions;
using Recall.Core.Ingest;
using Recall.Core.Ports;

namespace Recall.UnitTests;

public sealed class SchedulerTests
{
    [Fact]
    public async Task TextBacklog_DoesNotStarveOcr()
    {
        var scheduler = new IngestScheduler();
        var runner = new IngestSchedulerRunner(scheduler);
        for (var index = 0; index < 200; index++)
        {
            scheduler.Enqueue(new IngestJob(IngestModel.Text, $"note{index}.md"));
        }

        scheduler.Enqueue(new IngestJob(IngestModel.Ocr, "scan.pdf"));
        var textProcessed = 0;
        var textProcessedWhenOcrRan = -1;
        var ocr = new RecordingOcrEngine();
        var hooks = new IngestPassHooks
        {
            ProcessTextJobAsync = (_, _) =>
            {
                textProcessed++;
                return Task.CompletedTask;
            },
            RunOcr = _ =>
            {
                textProcessedWhenOcrRan = textProcessed;
                return ocr;
            },
        };

        await runner.RunReadyPassesAsync(DateTimeOffset.UtcNow.AddSeconds(31), hooks, TestContext.Current.CancellationToken);

        ocr.RunPassCalls.Should().Be(1, "the OCR slice runs in the first round even with 200 text jobs queued");
        textProcessedWhenOcrRan.Should().Be(
            IngestSchedulerRunner.TextSlice,
            "OCR runs after the first text slice, not after the whole text backlog");
        textProcessed.Should().Be(200, "the fair slices still drain the text backlog inside the pass budget");
        scheduler.QueueCount(IngestModel.Ocr).Should().Be(0);
    }

    [Fact]
    public async Task BudgetSpent_LeavesQueuedJobs()
    {
        var scheduler = new IngestScheduler();
        var time = new FakeTimeProvider();
        var runner = new IngestSchedulerRunner(scheduler, time: time);
        for (var index = 0; index < 200; index++)
        {
            scheduler.Enqueue(new IngestJob(IngestModel.Text, $"note{index}.md"));
        }

        var processed = 0;
        var hooks = new IngestPassHooks
        {
            ProcessTextJobAsync = (_, _) =>
            {
                processed++;
                time.Advance(IngestSchedulerRunner.PassBudget);
                return Task.CompletedTask;
            },
        };

        await runner.RunReadyPassesAsync(DateTimeOffset.UtcNow.AddSeconds(31), hooks, TestContext.Current.CancellationToken);

        processed.Should().Be(1, "the budget is checked between jobs, so the slice stops once it is spent");
        scheduler.QueueCount(IngestModel.Text).Should().Be(
            199,
            "a spent budget returns without cancelling the queued jobs; they stay for the next trigger");
    }

    [Fact]
    public async Task SearchCancelsOcr_RequeuesRemaining()
    {
        var scheduler = new IngestScheduler();
        var preemption = new IngestPreemption();
        var runner = new IngestSchedulerRunner(scheduler, preemption: preemption);
        scheduler.Enqueue(new IngestJob(IngestModel.Ocr, "a.png"));
        scheduler.Enqueue(new IngestJob(IngestModel.Ocr, "b.png"));
        scheduler.Enqueue(new IngestJob(IngestModel.Ocr, "c.png"));
        var ocr = new CancellingOcrEngine(preemption);
        var hooks = new IngestPassHooks { RunOcr = _ => ocr };
        var now = DateTimeOffset.UtcNow.AddSeconds(31);

        await runner.RunReadyPassesAsync(now, hooks, TestContext.Current.CancellationToken);

        ocr.Persisted.Should().ContainSingle("the search cancels the pass after the first document is persisted");
        preemption.IsSearching.Should().BeTrue("the search is still in flight while the runner yields");
        scheduler.QueueCount(IngestModel.Ocr).Should().Be(3, "the cancelled slice is re-enqueued so no document is lost");

        preemption.SearchFinished();
        await runner.RunReadyPassesAsync(now, hooks, TestContext.Current.CancellationToken);

        ocr.Persisted.Should().HaveCount(3, "the re-enqueued slice finishes the two remaining documents");
        ocr.Persisted.Should().OnlyHaveUniqueItems("a persisted document is not processed twice");
        scheduler.QueueCount(IngestModel.Ocr).Should().Be(0);
    }

    [Fact]
    public async Task Metrics_RecordOnePerSlice()
    {
        var scheduler = new IngestScheduler();
        var metrics = new IngestPassMetrics();
        var runner = new IngestSchedulerRunner(scheduler, metrics);
        for (var index = 0; index < 3; index++)
        {
            scheduler.Enqueue(new IngestJob(IngestModel.Text, $"note{index}.md"));
        }

        for (var index = 0; index < 2; index++)
        {
            scheduler.Enqueue(new IngestJob(IngestModel.Media, $"pic{index}.jpg"));
        }

        scheduler.Enqueue(new IngestJob(IngestModel.Ocr, "scan.pdf"));
        var hooks = new IngestPassHooks
        {
            ProcessTextJobAsync = (_, _) => Task.CompletedTask,
            ProcessMediaJobAsync = (_, _) => Task.CompletedTask,
            RunOcr = _ => new RecordingOcrEngine(),
        };

        await runner.RunReadyPassesAsync(DateTimeOffset.UtcNow.AddSeconds(31), hooks, TestContext.Current.CancellationToken);

        var snapshot = metrics.Snapshot();
        snapshot.Should().HaveCount(3, "one metric is recorded per slice");
        snapshot.Should().ContainSingle(metric => metric.Model == IngestModel.Text && metric.Jobs == 3);
        snapshot.Should().ContainSingle(metric => metric.Model == IngestModel.Media && metric.Jobs == 2);
        snapshot.Should().ContainSingle(metric => metric.Model == IngestModel.Ocr && metric.Jobs == 1);
    }

    private sealed class RecordingOcrEngine : IOcrEngine
    {
        public int RunPassCalls { get; private set; }

        public Task RunPassAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken)
        {
            RunPassCalls++;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

    private sealed class CancellingOcrEngine : IOcrEngine
    {
        private readonly IIngestPreemption preemption;

        public CancellingOcrEngine(IIngestPreemption preemption)
        {
            this.preemption = preemption;
        }

        public List<string> Persisted { get; } = [];

        public Task RunPassAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken)
        {
            foreach (var path in paths)
            {
                if (Persisted.Contains(path))
                {
                    continue;
                }

                if (Persisted.Count == 0)
                {
                    Persisted.Add(path);
                    preemption.SearchStarted();
                }

                cancellationToken.ThrowIfCancellationRequested();
                Persisted.Add(path);
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private long ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Interlocked.Read(ref ticks);

        public void Advance(TimeSpan by) => Interlocked.Add(ref ticks, by.Ticks);
    }
}
