using BenchmarkDotNet.Attributes;
using Recall.Adapters.Embedding;
using Recall.Core.Text;

namespace Recall.Benchmarks;

[MemoryDiagnoser]
public class TextSearchBenchmarks
{
    private TextModelRuntime runtime = null!;

    [GlobalSetup]
    public void Setup()
    {
        var directory = TextBenchmarkHost.FindModelDirectory()
            ?? throw new InvalidOperationException(TextBenchmarkHost.ModelDirectoryVariable);
        runtime = new TextModelRuntime(directory);
    }

    [GlobalCleanup]
    public void Cleanup() => runtime.Dispose();

    [Benchmark]
    public async Task<int> EmbedOneSearchQueryAsync()
    {
        var vector = await runtime.GetEncoder().EmbedPrefixedTextAsync(EmbeddingTaskPrefixes.WrapSearchQuery("red planet"));
        return vector.Length;
    }
}
