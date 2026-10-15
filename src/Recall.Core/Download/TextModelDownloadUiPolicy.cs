namespace Recall.Core.Download;

public static class TextModelDownloadUiPolicy
{
    public static bool ShouldAutoStartDownload(TextModelDownloadPhase phase) =>
        phase == TextModelDownloadPhase.NotStarted;

    public static bool ShouldOfferRetry(TextModelDownloadPhase phase) =>
        phase is TextModelDownloadPhase.HashFailed
            or TextModelDownloadPhase.Interrupted
            or TextModelDownloadPhase.Cancelled;

    public static bool ShouldPublishProgress(int lastPublishedPercent, int nextPercent) =>
        nextPercent != lastPublishedPercent;

    public static TextModelDownloadSnapshot MarkVerifying(TextModelDownloadSnapshot snapshot) =>
        snapshot with { Phase = TextModelDownloadPhase.Verifying, Fault = null };

    public static int ComputeOverallPercent(int completedFiles, int fileCount, long bytesReceived, long? totalBytes)
    {
        if (fileCount <= 0)
        {
            return 0;
        }

        if (completedFiles < 0)
        {
            completedFiles = 0;
        }

        if (completedFiles > fileCount)
        {
            completedFiles = fileCount;
        }

        var fileFraction = ComputePercent(bytesReceived, totalBytes) / 100d;
        var overall = (completedFiles + fileFraction) / fileCount;
        if (overall <= 0)
        {
            return 0;
        }

        if (overall >= 1)
        {
            return 100;
        }

        return (int)Math.Floor(overall * 100);
    }

    public static int ComputePercent(long bytesReceived, long? totalBytes)
    {
        if (totalBytes is null or <= 0)
        {
            return 0;
        }

        var ratio = (double)bytesReceived / totalBytes.Value;
        if (ratio <= 0)
        {
            return 0;
        }

        if (ratio >= 1)
        {
            return 100;
        }

        return (int)Math.Floor(ratio * 100);
    }
}
