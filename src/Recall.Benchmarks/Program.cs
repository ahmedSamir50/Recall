using BenchmarkDotNet.Running;

var directory = Recall.Benchmarks.TextBenchmarkHost.FindModelDirectory();
if (directory is null)
{
    return 0;
}

var label = Recall.Benchmarks.TextBenchmarkHost.LabelThisProcess();
if (string.Equals(label, "emulator", StringComparison.Ordinal))
{
    Console.WriteLine("Host: emulator");
    return 2;
}

Console.WriteLine("Host: " + label);
BenchmarkRunner.Run<Recall.Benchmarks.TextIndexBenchmarks>();
BenchmarkRunner.Run<Recall.Benchmarks.TextSearchBenchmarks>();
return 0;
