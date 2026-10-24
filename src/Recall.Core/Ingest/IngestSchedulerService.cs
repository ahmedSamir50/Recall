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
        IIngestModelSession? session = null,
        Age.AgeMoveService? ageMove = null,
        Age.PinService? pin = null)
    {
        this.scheduler = scheduler;
        this.runner = runner;
        Action<DateTimeOffset>? sweepAgedMedia = ageMove is null && pin is null
            ? null
            : now =>
            {
                pin?.Resume();
                ageMove?.Sweep(now);
            };
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
            SweepAgedMedia = sweepAgedMedia,
        };
    }

    public IngestScheduler Scheduler => scheduler;

    public Task RunReadyPassesAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        runner.RunReadyPassesAsync(now, productionHooks, cancellationToken);
}
