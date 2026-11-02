namespace Recall.Core.Ingest;

public static class IngestStopKeys
{
    public const string MemoryBudget = "Ingest_StoppedMemoryBudget";

    public const string FileTooLarge = "Ingest_SkippedTooLarge";

    public const string DeviceTooSmall = "Ingest_DeviceTooSmall";
}
