using BenchmarkDotNet.Attributes;
using Recall.Adapters.Indexing;
using Recall.Core.Search;
using Recall.Core.Text;
using Recall.Core.Vocabulary;

namespace Recall.Benchmarks;

[MemoryDiagnoser]
public class TextIndexBenchmarks
{
    private string root = string.Empty;
    private string notesDirectory = string.Empty;
    private ZVecTextVectorIndex index = null!;
    private int nextId;

    [GlobalSetup]
    public void Setup()
    {
        root = Path.Combine(Path.GetTempPath(), "recall_bench_" + Guid.NewGuid().ToString("N"));
        notesDirectory = Path.Combine(root, "notes");
        Directory.CreateDirectory(notesDirectory);
        index = new ZVecTextVectorIndex(root);
        for (var i = 0; i < 1000; i++)
        {
            var vector = new float[512];
            vector[0] = 1f;
            index.TryInsert(new TextIndexInsert(
                ContentIdBuilder.Build(Modality.Text, ContentIdBuilder.HashSourceContent("bench-" + i), StoredWidth.W512, 0),
                Path.Combine(notesDirectory, "note-" + i + ".txt"),
                "note-" + i + ".txt",
                "benchmark note " + i,
                vector,
                DateTimeOffset.UtcNow));
        }

        nextId = 1000;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        index.Dispose();
        if (Directory.Exists(root))
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch
            {
                // Best effort cleanup of the temp benchmark root.
            }
        }
    }

    [Benchmark]
    public bool InsertOne()
    {
        var id = nextId++;
        var vector = new float[512];
        vector[0] = 1f;
        return index.TryInsert(new TextIndexInsert(
            ContentIdBuilder.Build(Modality.Text, ContentIdBuilder.HashSourceContent("bench-" + id), StoredWidth.W512, 0),
            Path.Combine(notesDirectory, "note-" + id + ".txt"),
            "note-" + id + ".txt",
            "benchmark note " + id,
            vector,
            DateTimeOffset.UtcNow));
    }

    [Benchmark]
    public int HybridTop20()
    {
        var query = new float[512];
        query[0] = 1f;
        var hits = index.Search(new TextSearchRequest(
            "benchmark note",
            query,
            new HashSet<string>([notesDirectory], StringComparer.OrdinalIgnoreCase),
            TopK: 20));
        return hits.Count;
    }
}
