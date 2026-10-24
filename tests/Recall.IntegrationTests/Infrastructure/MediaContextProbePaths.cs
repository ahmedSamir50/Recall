namespace Recall.IntegrationTests.Infrastructure;

public static class MediaContextProbePaths
{
    public const string MmprojFileName = "mmproj-embeddinggemma-2-Q8_0.gguf";

    public const string RelativeDir = "TestAssets/embeddinggemma-2-vision";

    public static string ResolveMmprojFile()
    {
        var env = Environment.GetEnvironmentVariable("RECALL_MMPROJ_DIR");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(Path.Combine(env, MmprojFileName)))
        {
            return Path.Combine(env, MmprojFileName);
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "embeddinggemma-2-vision"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestAssets", "embeddinggemma-2-vision")),
            Path.GetFullPath(Path.Combine(FindRepoRoot(), "tests", "Recall.IntegrationTests", "TestAssets", "embeddinggemma-2-vision")),
        };

        foreach (var dir in candidates)
        {
            var file = Path.Combine(dir, MmprojFileName);
            if (File.Exists(file))
            {
                return file;
            }
        }

        throw new InvalidOperationException($"{MmprojFileName} is required for the media context probe");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Recall.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Recall.slnx not found.");
    }
}
