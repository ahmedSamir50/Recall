namespace Recall.Core.Ingest;

public static class FrameCadence
{
    public const double PeriodSeconds = 2;

    public const int WindowsMinimumBuild = 22000;

    public const string WindowsMinimumBuildMessage = "Windows frame thumbnails need Windows 11.";

    public static IReadOnlyList<double> Starts(double durationSeconds)
    {
        if (durationSeconds <= 0)
        {
            return [];
        }

        var starts = new List<double>();
        for (var start = 0d; start < durationSeconds; start += PeriodSeconds)
        {
            starts.Add(start);
        }

        return starts;
    }

    public static double End(double startSeconds, double durationSeconds) =>
        Math.Min(startSeconds + PeriodSeconds, durationSeconds);
}
