using BenchmarkDotNet.Running;

var directory = Recall.Benchmarks.TextBenchmarkHost.FindModelDirectory();
if (directory is null)
{
    await Console.Error.WriteLineAsync("No measurement. Set RECALL_TEXT_MODEL_DIR to the text model folder.");
    return 2;
}

BenchmarkRunner.Run<Recall.Benchmarks.TextSearchBenchmarks>();
return 0;
