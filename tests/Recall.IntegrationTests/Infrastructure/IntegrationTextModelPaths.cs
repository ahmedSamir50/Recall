namespace Recall.IntegrationTests.Infrastructure;

public static class IntegrationTextModelPaths
{
    public const string RelativeDir = "TestAssets/embeddinggemma-2-text";

    public static string ResolveModelDirectory()
    {
        var env = Environment.GetEnvironmentVariable("RECALL_TEXT_MODEL_DIR");
        if (!string.IsNullOrWhiteSpace(env) && IsReady(env))
        {
            return env;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "embeddinggemma-2-text"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestAssets", "embeddinggemma-2-text")),
            Path.GetFullPath(Path.Combine(FindRepoRoot(), "tests", "Recall.IntegrationTests", "TestAssets", "embeddinggemma-2-text")),
        };

        foreach (var dir in candidates)
        {
            if (IsReady(dir))
            {
                return dir;
            }
        }

        throw new InvalidOperationException(
            $"Text model assets not found. Expected model_quantized.onnx under {RelativeDir}. Run: hf download onnx-community/embeddinggemma-2-ONNX --include onnx/model_quantized.onnx onnx/model_quantized.onnx_data tokenizer.json --local-dir tests/Recall.IntegrationTests/TestAssets/embeddinggemma-2-text");
    }

    public static bool IsReady(string directory) =>
        File.Exists(Path.Combine(directory, "model_quantized.onnx"))
        && File.Exists(Path.Combine(directory, "model_quantized.onnx_data"))
        && File.Exists(Path.Combine(directory, "tokenizer.json"));

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
