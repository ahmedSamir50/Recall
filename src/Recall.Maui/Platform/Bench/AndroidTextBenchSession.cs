#if ANDROID
using System.Diagnostics;
using System.Globalization;
using Recall.Adapters.Embedding;
using Recall.Adapters.Indexing;
using Recall.Benchmarks;
using Recall.Core.Download;
using Recall.Core.Indexing;
using Recall.Core.Search;
using Recall.Core.Text;
using Recall.Core.Vocabulary;

namespace Recall.Maui.Platform.Bench;

public static class AndroidTextBenchSession
{
    public static int LastHitCount { get; private set; }

    public static void RunIfReady(string appFilesDirectory)
    {
        var options = new TextModelDownloadOptions();
        string? root = null;
        var external = global::Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
        if (IsBenchRoot(external, options.ModelFileName))
        {
            root = external;
        }
        else if (IsBenchRoot(appFilesDirectory, options.ModelFileName))
        {
            root = appFilesDirectory;
        }

        if (root is null)
        {
            return;
        }

        var hostLine = File.ReadAllLines(Path.Combine(root, "bench-host.txt"))[0].Trim();
        if (!string.Equals(hostLine, "phone", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(hostLine, "emulator", StringComparison.OrdinalIgnoreCase))
        {
            WriteReport(root, "bad-host");
            TryDelete(Path.Combine(root, "bench-host.txt"));
            return;
        }

        var hostLabel = string.Equals(hostLine, "phone", StringComparison.OrdinalIgnoreCase) ? "phone" : "emulator";
        var modelDirectory = Path.Combine(root, "bench-model");
        if (!File.Exists(Path.Combine(modelDirectory, options.ModelFileName)))
        {
            WriteReport(root, "missing-model");
            TryDelete(Path.Combine(root, "bench-host.txt"));
            return;
        }

        string? tempRoot = null;
        try
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "recall_bench_" + Guid.NewGuid().ToString("N"));
            var notesDirectory = Path.Combine(tempRoot, "notes");
            Directory.CreateDirectory(notesDirectory);
            var index = new ZVecTextVectorIndex(tempRoot);
            List<double> hybridSamples;
            try
            {
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

                hybridSamples = new List<double>(TextBenchmarkHost.AndroidMeasuredIterations);
                for (var w = 0; w < TextBenchmarkHost.AndroidWarmupIterations; w++)
                {
                    LastHitCount = HybridTop20(index, notesDirectory);
                }

                for (var m = 0; m < TextBenchmarkHost.AndroidMeasuredIterations; m++)
                {
                    var sw = Stopwatch.StartNew();
                    LastHitCount = HybridTop20(index, notesDirectory);
                    sw.Stop();
                    hybridSamples.Add(sw.Elapsed.TotalMilliseconds);
                }
            }
            finally
            {
                index.Dispose();
            }

            var loadSamples = new List<double>(TextBenchmarkHost.AndroidMeasuredIterations);
            for (var w = 0; w < TextBenchmarkHost.AndroidWarmupIterations; w++)
            {
                using var warmup = new TextModelRuntime(modelDirectory);
                warmup.GetEncoder();
            }

            for (var m = 0; m < TextBenchmarkHost.AndroidMeasuredIterations; m++)
            {
                var sw = Stopwatch.StartNew();
                using var runtime = new TextModelRuntime(modelDirectory);
                runtime.GetEncoder();
                sw.Stop();
                loadSamples.Add(sw.Elapsed.TotalMilliseconds);
            }

            var hybridMean = TextBenchmarkHost.Mean(hybridSamples);
            var loadMean = TextBenchmarkHost.Mean(loadSamples);
            var lines = new[]
            {
                "Host: " + hostLabel,
                "HybridTop20 mean " + hybridMean.ToString("F2", CultureInfo.InvariantCulture) + " ms",
                "LoadTextEncoder mean " + loadMean.ToString("F2", CultureInfo.InvariantCulture) + " ms",
            };
            WriteReport(root, lines);
            foreach (var line in lines)
            {
                global::Android.Util.Log.Info("RecallBench", line);
            }

            TryDelete(Path.Combine(root, "bench-host.txt"));
        }
        catch (Exception ex)
        {
            WriteReport(root, "fault " + ex.GetType().Name);
            TryDelete(Path.Combine(root, "bench-host.txt"));
        }
        finally
        {
            if (tempRoot is not null && Directory.Exists(tempRoot))
            {
                try
                {
                    Directory.Delete(tempRoot, true);
                }
                catch
                {
                    // Best effort cleanup of the temp benchmark root.
                }
            }
        }
    }

    private static int HybridTop20(ZVecTextVectorIndex index, string notesDirectory)
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

    private static bool IsBenchRoot(string? directory, string modelFileName) =>
        !string.IsNullOrEmpty(directory)
        && File.Exists(Path.Combine(directory, "bench-host.txt"))
        && File.Exists(Path.Combine(directory, "bench-model", modelFileName));

    private static void WriteReport(string root, string line) => File.WriteAllText(Path.Combine(root, "android-text-bench.txt"), line);

    private static void WriteReport(string root, IReadOnlyList<string> lines) =>
        File.WriteAllLines(Path.Combine(root, "android-text-bench.txt"), lines);

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort removal of the bench trigger file.
        }
    }
}
#endif
