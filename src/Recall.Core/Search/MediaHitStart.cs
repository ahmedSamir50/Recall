namespace Recall.Core.Search;

public static class MediaHitStart
{
    public static double Seconds(TextSearchHit hit)
    {
        ArgumentNullException.ThrowIfNull(hit);
        return hit.StartSeconds;
    }

    public static double Seconds(MediaSearchHit hit)
    {
        ArgumentNullException.ThrowIfNull(hit);
        return hit.StartSeconds;
    }

    public static bool Seeks(double startSeconds) => startSeconds > 0;
}
