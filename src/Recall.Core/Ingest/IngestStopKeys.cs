namespace Recall.Core.Ingest;

public static class IngestStopKeys
{
    public const string MemoryBudget = "Ingest_StoppedMemoryBudget";

    public const string FileTooLarge = "Ingest_SkippedTooLarge";

    public const string DeviceTooSmall = "Ingest_DeviceTooSmall";

    public const string GpuTemperature = "Ingest_StoppedGpuTemperature";

    public const string SearchCancel = "Ingest_StoppedSearch";

    public const string Interrupted = "Ingest_StoppedInterrupted";

    public const string RetryDeferred = "Ingest_RetryDeferred";
}
