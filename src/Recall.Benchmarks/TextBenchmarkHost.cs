using Recall.Core.Download;
using Recall.Core.Text;

namespace Recall.Benchmarks;

public static class TextBenchmarkHost
{
    public const string ModelDirectoryVariable = "RECALL_TEXT_MODEL_DIR";
    public const string BenchHostVariable = "RECALL_BENCH_HOST";

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

    public static bool PhoneHybridAndLoadMeetTheExit(string hostLabel, double hybridTop20Milliseconds, double loadMilliseconds) =>
        string.Equals(hostLabel, "phone", StringComparison.OrdinalIgnoreCase)
        && hybridTop20Milliseconds <= 60d
        && loadMilliseconds <= 600d;
}
