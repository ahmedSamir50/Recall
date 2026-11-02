namespace Recall.Core.Download;

public static class WindowIngestPolicy
{
    public static bool ShouldStart(TextModelDownloadPhase phase) =>
        phase == TextModelDownloadPhase.Ready;
}
