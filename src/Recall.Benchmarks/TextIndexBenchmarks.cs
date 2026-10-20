using BenchmarkDotNet.Attributes;
using Recall.Adapters.Indexing;
using Recall.Core.Indexing;
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
            index.TryInsert(BenchInsert(
                ContentIdBuilder.Build(Modality.Text, ContentIdBuilder.HashSourceContent("bench-" + i), StoredWidth.W512, 0),
                Path.Combine(notesDirectory, "note-" + i + ".txt"),
                "benchmark note " + i,
                vector,
                notesDirectory));
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
        return index.TryInsert(BenchInsert(
            ContentIdBuilder.Build(Modality.Text, ContentIdBuilder.HashSourceContent("bench-" + id), StoredWidth.W512, 0),
            Path.Combine(notesDirectory, "note-" + id + ".txt"),
            "benchmark note " + id,
            vector,
            notesDirectory));
    }

    [Benchmark]
    public int HybridTop20()
    {
        var query = new float[512];
        query[0] = 1f;
        var grants = new HashSet<string>([notesDirectory], StringComparer.OrdinalIgnoreCase);
        const string hash = "bench-hash";
        var hits = index.Search(new TextSearchRequest(
            "benchmark note",
            query,
            grants,
            hash,
            TextSearchFilter.Build(grants, hash),
            true,
            RecallIndexCaps.FileCap));
        return hits.Count;
    }

    private static TextIndexInsert BenchInsert(string contentId, string path, string raw, float[] vector, string grantPath) =>
        new(
            contentId,
            path,
            Path.GetFileName(path),
            raw,
            vector,
            DateTimeOffset.UtcNow,
            "bench-hash",
            true,
            grantPath,
            raw,
            0);
}
