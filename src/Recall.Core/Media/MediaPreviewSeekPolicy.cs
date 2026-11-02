namespace Recall.Core.Media;

public static class MediaPreviewSeekPolicy
{
    public static bool ShouldSeek(double startSeconds, TimeSpan duration) =>
        startSeconds >= 0
        && duration > TimeSpan.Zero
        && startSeconds <= duration.TotalSeconds;
}
