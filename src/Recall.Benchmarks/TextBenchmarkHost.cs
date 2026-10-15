using Recall.Adapters.Embedding;
using Recall.Core.Download;
using Recall.Core.Text;

namespace Recall.Benchmarks;

public static class TextBenchmarkHost
{
    public const string ModelDirectoryVariable = "RECALL_TEXT_MODEL_DIR";

    public static string? FindModelDirectory()
    {
        var directory = Environment.GetEnvironmentVariable(ModelDirectoryVariable);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        var options = new TextModelDownloadOptions();
        if (!File.Exists(Path.Combine(directory, options.OnnxFileName))
            || !File.Exists(Path.Combine(directory, options.TokenizerFileName)))
        {
            return null;
        }

        return directory;
    }

    public static string LabelThisProcess()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            return "phone";
        }

        return "desktop";
    }
}
