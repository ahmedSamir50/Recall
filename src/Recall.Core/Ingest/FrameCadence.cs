namespace Recall.Core.Ingest;

public static class FrameCadence
{
    public const double PeriodSeconds = 2;

    public const string WindowsPackageMessage = "Windows video sampling has no package.";

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
