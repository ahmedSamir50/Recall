namespace Recall.Core.Ingest;

public static class IngestRetryClock
{
    public static DateTimeOffset Next(DateTimeOffset finishedAt, int minutes) => finishedAt + TimeSpan.FromMinutes(minutes);

    public static bool ShouldRun(DateTimeOffset now, DateTimeOffset next, bool gateOpen, bool gpuHot, bool modelReady) =>
        now >= next && gateOpen && !gpuHot && modelReady;

    public static DateTimeOffset Defer(DateTimeOffset now, int minutes) => now + TimeSpan.FromMinutes(minutes);
}