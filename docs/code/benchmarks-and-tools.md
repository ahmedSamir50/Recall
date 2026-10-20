A benchmark that did not run has no measured number on this page.

## src/Recall.Benchmarks/Program.cs

```csharp
if (directory is null)
if (string.Equals(label, "emulator", StringComparison.Ordinal))
```

No public type.


## src/Recall.Benchmarks/TextBenchmarkHost.cs

```csharp
namespace Recall.Benchmarks;
public static class TextBenchmarkHost
    public static string? FindModelDirectory()
    public static string LabelThisProcess()
    public static bool PhoneHybridAndLoadMeetTheExit(string hostLabel, double hybridTop20Milliseconds, double loadMilliseconds)
        if (string.IsNullOrWhiteSpace(directory))
        if (!File.Exists(Path.Combine(directory, options.OnnxFileName))
        if (!androidOrIos)
        if (string.Equals(benchHost, "phone", StringComparison.OrdinalIgnoreCase))
        && loadMilliseconds <= 600d
```


## src/Recall.Benchmarks/TextIndexBenchmarks.cs

```csharp
namespace Recall.Benchmarks;
public class TextIndexBenchmarks
    public void Setup()
    public void Cleanup()
    public bool InsertOne()
    public int HybridTop20()
        if (Directory.Exists(root))
```


## src/Recall.Benchmarks/TextSearchBenchmarks.cs

```csharp
namespace Recall.Benchmarks;
public class TextSearchBenchmarks
    public void Setup()
    public void Cleanup() => runtime.Dispose();
    public async Task<int> EmbedOneSearchQueryAsync()
    public int LoadTextEncoder()
```

No branch.


## tools/corpus-builder/Program.cs

No public type.

No branch.


## tools/mrl-eval/Program.cs

No public type.

No branch.
