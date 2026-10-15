using BenchmarkDotNet.Running;

Console.WriteLine("Host: " + Recall.Benchmarks.TextBenchmarkHost.LabelThisProcess());
BenchmarkRunner.Run<Recall.Benchmarks.TextIndexBenchmarks>();

var directory = Recall.Benchmarks.TextBenchmarkHost.FindModelDirectory();
if (directory is null)
{
    await Console.Error.WriteLineAsync("No encoder measurement. Set RECALL_TEXT_MODEL_DIR to the text model folder.");
    return 0;
}

BenchmarkRunner.Run<Recall.Benchmarks.TextSearchBenchmarks>();
return 0;
