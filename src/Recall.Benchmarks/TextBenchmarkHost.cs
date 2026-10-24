using Recall.Core.Download;

namespace Recall.Benchmarks;

public static class TextBenchmarkHost
{
    public const string ModelDirectoryVariable = "RECALL_TEXT_MODEL_DIR";
    public const string BenchHostVariable = "RECALL_BENCH_HOST";
    public const int AndroidWarmupIterations = 1;
    public const int AndroidMeasuredIterations = 10;
    public const double AndroidHybridPassMilliseconds = 200d;
    public const double AndroidLoadPassMilliseconds = 1000d;

    public static string? FindModelDirectory()
    {
        var directory = Environment.GetEnvironmentVariable(ModelDirectoryVariable);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        var options = new TextModelDownloadOptions();
        if (!File.Exists(Path.Combine(directory, options.ModelFileName)))
        {
            return null;
        }

        return directory;
    }

    public static string Label(bool androidOrIos, string? benchHost)
    {
        if (!androidOrIos)
        {
            return "desktop";
        }

        if (string.Equals(benchHost, "phone", StringComparison.OrdinalIgnoreCase))
        {
            return "phone";
        }

        return "emulator";
    }

    public static string LabelThisProcess() =>
        Label(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS(), Environment.GetEnvironmentVariable(BenchHostVariable));

    public static int PhoneLoadPassMilliseconds(double meanMilliseconds) =>
        (int)Math.Round(meanMilliseconds * 1.1, 0, MidpointRounding.AwayFromZero);

    public static double Mean(IReadOnlyList<double> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count == 0)
        {
            throw new ArgumentException("At least one sample is required.", nameof(samples));
        }

        var sum = 0d;
        foreach (var sample in samples)
        {
            sum += sample;
        }

        return sum / samples.Count;
    }

    public static bool PhoneHybridAndLoadMeetTheExit(string hostLabel, double hybridTop20Milliseconds, double loadMilliseconds) =>
        (string.Equals(hostLabel, "phone", StringComparison.OrdinalIgnoreCase)
            || string.Equals(hostLabel, "emulator", StringComparison.OrdinalIgnoreCase))
        && hybridTop20Milliseconds <= AndroidHybridPassMilliseconds
        && loadMilliseconds <= AndroidLoadPassMilliseconds;
}
