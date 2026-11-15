namespace Recall.Core.Ingest;

public static class IngestRetryPolicy
{
    public const int MaxAutomaticAttempts = 3;

    public static bool AllowAutomatic(int failureCount) => failureCount < MaxAutomaticAttempts;
}