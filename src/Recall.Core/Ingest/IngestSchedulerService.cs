namespace Recall.Core.Ingest;

public sealed class IngestSchedulerService
{
    private readonly IngestScheduler scheduler;
    private readonly IngestSchedulerRunner runner;
    private readonly IngestPassHooks productionHooks;

    public IngestSchedulerService(
        IngestScheduler scheduler,
        IngestSchedulerRunner runner,
        ScheduledIngestDispatcher dispatcher,
        IIngestModelSession? session = null)
    {
        this.scheduler = scheduler;
        this.runner = runner;
        productionHooks = new IngestPassHooks
        {
            OpenText = session is null ? null : session.OpenText,
            CloseText = session is null ? null : session.CloseText,
            OpenVision = session is null ? null : session.OpenVision,
            CloseVision = session is null ? null : session.CloseVision,
            OpenAudio = session is null ? null : session.OpenAudio,
            CloseAudio = session is null ? null : session.CloseAudio,
            EnsureMediaContextProbe = session is null ? null : session.EnsureProbe,
            BeginMediaLeg = dispatcher.SetLeg,
            ProcessTextJobAsync = dispatcher.DispatchTextAsync,
            ProcessMediaJobAsync = dispatcher.DispatchMediaAsync,
        };
    }

    public IngestScheduler Scheduler => scheduler;

    public Task RunReadyPassesAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        runner.RunReadyPassesAsync(now, productionHooks, cancellationToken);
}
