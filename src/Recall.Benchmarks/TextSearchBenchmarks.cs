using BenchmarkDotNet.Attributes;

namespace Recall.Benchmarks;

[MemoryDiagnoser]
public  class TextSearchBenchmarks
{
    [Benchmark(Description = "text-only-exit-placeholder")]
    public int Placeholder() => 42;
}
