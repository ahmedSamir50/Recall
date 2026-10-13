# Recall — Technical Details

> **Engineering and ML deep-dive** for the Recall on-device multimodal second-brain application.
> Architecture, algorithms, math derivations, and idiomatic C# integration patterns for EmbeddingGemma 2 (ONNX) + ZVec.NET + .NET MAUI Blazor Hybrid.
> Companion to `Recall_App_Implementation_Plan.md`.

---

## Document Control

| Field | Value |
|---|---|
| Document | Recall App — Technical Details |
| Version | 1.2 (revised — tiny LLM sidecar killed; replaced with zero-LLM deterministic cluster labeler; all other v1.1 scope refinements retained) |
| Status | Living — owned by the Recall Tech Lead |
| Audience | ML, Backend, Mobile, Desktop Engineers |
| Source | Derived from `IdeaTank_Embeddinggemma_2_ZVecNET.md` §3 + verified stack capabilities §1 |
| Companion doc | `Recall_App_Implementation_Plan.md` (also v1.2) |
| Last reviewed | 2026-10-09 (v1.2) |

---

## Table of Contents

1. Architecture Deep Dive
2. EmbeddingGemma 2 Modular Encoder Loading
3. ONNX Runtime Integration in .NET
4. ZVec.NET Integration
5. Index Strategy Per Platform
6. MRL Tiering — Algorithm & Derivation
7. HNSW Complexity & Recall Tradeoff
8. RRF Reranker Derivation
9. Task Prefix Engineering
10. 8K Context Handling
11. Capture Pipeline
12. Query Pipeline
13. Sync Design — Technical
14. Memory & Storage Budget Per Platform
15. Failure Modes & Recovery
16. Appendix A — MRL Truncation Error Bound
17. Appendix B — RRF Probability Interpretation
18. Appendix C — References

---

## 1. Architecture Deep Dive

### 1.1 The single-process invariant

Recall's most important architectural invariant is the **single-process invariant**: the embedder (ONNX Runtime), the vector database (ZVec.NET), and the application logic (Recall.Core) all live inside the same .NET process. There is no IPC, no localhost HTTP, no UDS, no shared-memory shim.

This invariant is the source of every defensible property of the system:

- **Latency:** `ReadOnlyMemory<float>` flows from the ONNX inference session's output tensor to a ZVec.NET `Collection.Insert` call without crossing a process boundary. The cost is a memory copy of `768 × 4 = 3,072 bytes`. At 100k items in HNSW, this is the difference between p95 = 50 ms and p95 = 150 ms.
- **Privacy:** There is no network call to the embedder (no OpenAI / Cohere / Voyage), and no network call to the DB (no Pinecone / Weaviate / Qdrant Cloud). The `BannedApiAnalyzers` Roslyn rule makes it a compile error to introduce a network call outside the `Recall.Core.Sync` namespace, and `Recall.Core.Sync` is interface-only in v1.
- **Density:** ZVec.NET's in-process architecture means the DB lives in the application's address space. There is no separate DB server whose RSS we have to budget for. The full memory budget is the app's RSS.

Any future architecture that violates the single-process invariant breaks the thesis. The temptation will be to "just stand up a localhost Qdrant for desktop" — that is exactly the wrong move.

### 1.2 Component responsibilities

```
┌──────────────────────────────────────────────────────────────────────┐
│                        Recall Process (.NET 9)                       │
│                                                                      │
│  ┌─────────────────┐   ┌─────────────────┐   ┌──────────────────┐   │
│  │ MAUI Shell      │   │ Recall.Core     │   │ ONNX Runtime     │   │
│  │ (Blazor WebView)│ ─►│ (capture/query) │ ─►│ InferenceSession │   │
│  │ + Platform Svc  │   │ + MRL tiering   │   │ (text/vision/    │   │
│  └─────────────────┘   │ + OpLog         │   │  audio encoders) │   │
│          ▲              └────────┬────────┘   └────────┬─────────┘   │
│          │                       │                     │             │
│          │                       ▼                     │             │
│          │              ┌────────────────┐             │             │
│          │              │ ZVec.NET Coll. │ ◄───────────┘             │
│          │              │ (HNSW + FTS +  │                           │
│          │              │  RRF + scalar) │                           │
│          │              └────────────────┘                           │
│          │                       ▲                                   │
│          │                       │                                   │
│          │              ┌────────────────┐                           │
│          │              │ Local Disk     │                           │
│          │              │ - zvec.db      │                           │
│          │              │ - oplog/*.log  │                           │
│          │              │ - onnx/*.onnx  │                           │
│          │              └────────────────┘                           │
│          │                                                           │
│  ┌──────┴───────────────┐                                            │
│  │ Razor UI (Blazor)    │                                            │
│  │ Search/Capture/Pin   │                                            │
│  └──────────────────────┘                                            │
└──────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                       [NO NETWORK EGRESS]
                  (Sync transport disabled in v1;
                   design in §13 of this doc and §12 of the plan)
```

### 1.3 Threading model

- **UI thread** — Blazor WebView runs on a single UI thread; Razor components render synchronously. UI events dispatch to ViewModels, which call `Recall.Core` async APIs.
- **Background ingestion worker** — `IHostedService` running a `BackgroundService`; pulls from an in-memory channel (`System.Threading.Channels`) that capture sources push to. Runs ONNX inference and ZVec inserts on a worker thread; never blocks the UI thread.
- **Query path** — runs on the Blazor dispatch thread but awaits `ValueTask`s from `Recall.Core`. ZVec.NET's async API uses `ValueTask + CancellationToken`; the underlying native ZVec call is dispatched to a thread pool thread via `Task.Run` for HNSW search at scale.
- **ONNX inference** — `InferenceSession.Run<T>()` is thread-safe but blocks; we serialize inference per-session via a `SemaphoreSlim(1, 1)` to avoid contention on mobile (where the EP is single-threaded).

### 1.4 Lifecycle

```
Process start
    │
    ├──► MAUI native host init
    │
    ├──► MauiProgram.Build()
    │       │
    │       ├──► AddZVec(cfg => ...)             # registers IZVecClient, IZVecCollectionFactory
    │       ├──► AddOnnxRuntime(opts => ...)     # registers IOnnxEncoderSessionFactory
    │       ├──► AddRecallCore(opts => ...)      # registers CapturePipeline, QueryPipeline, MrlTieringPolicy, OpLog
    │       └──► AddHostedService<IngestionWorker>()
    │
    ├──► MAUI loads Razor components into Blazor WebView
    │
    ├──► IngestionWorker.StartAsync()
    │       │
    │       └──► Channel<CaptureItem>.Reader → process loop
    │
    ├──► UI: Search.razor renders
    │
    └──► User interacts
                │
                └──► SearchViewModel.SearchAsync()
                       │
                       ├──► TaskPrefixSelector.Select(query) → "search"
                       ├──► QueryPipeline.RunAsync(query, "search", topK=20, ct)
                       │       │
                       │       ├──► OnnxEncoderSession.RunAsync(query, prefix) → 768-d
                       │       ├──► MrlTieringPolicy.TruncateForCorpus(query) → query per tier
                       │       ├──► ZVecCollection.HybridSearchAsync(queryByTier, scalarFilter, topK=50)
                       │       │       │
                       │       │       └──► RRF reranker fuses dense + FTS ranks
                       │       │
                       │       └──► Return top-20 with provenance
                       │
                       └──► UI renders 20 ResultCards

Process shutdown
    │
    ├──► IngestionWorker.StopAsync()      # drain channel, flush op-log
    ├──► IZVecCollection.DisposeAsync()    # SafeZvecHandle.Release
    ├──► OnnxEncoderSession.Dispose()     # native ONNX RT cleanup
    └──► MAUI host teardown
```

### 1.5 Key invariants enforced in code

1. **No egress:** `BannedApiAnalyzers` rule `RE0001` makes `HttpClient`, `Socket`, `TcpListener` outside `Recall.Core.Sync` a compile error.
2. **Single process:** `OnnxEncoderSession` and `IZVecCollection` are injected as singletons via DI; both load their native dependencies into the same process address space.
3. **SafeHandle discipline:** ZVec.NET `SafeZvecHandle` and ONNX Runtime `OrtEnv` are wrapped in `using`/`Dispose` patterns; finalizers log if a handle leaks (test fixture asserts zero leaks across 1000 inserts).
4. **Tier floor:** `MrlTieringPolicy` throws `ArgumentOutOfRangeException` for any `dim < 128`.
5. **Modality isolation:** Each modality has its own ONNX encoder session; the `EncoderMode` enum (`Text`/`TextVision`/`Full`) is the only switch.

---

## 2. EmbeddingGemma 2 Modular Encoder Loading

### 2.1 Why modular loading matters for Recall

EmbeddingGemma 2 (IdeaTank §1.1) is a 740M parameter model with three modular configurations:

| Configuration | Parameters loaded | Use case in Recall |
|---|---|---|
| Text-only | 270M (130M transformer + 140M embedder) | Background text ingestion (clipboard, OCR, file names) |
| Text + Vision | 440M (text 270M + vision 170M) | Image embedding (photos, screenshots, video frames) |
| Full multimodal | 740M (text 270M + vision 170M + audio 300M) | Cross-modal queries (audio in / out, video frames) |

The reason modular loading matters on a mobile device with 4 GB of addressable RSS is that loading 740M parameters as float16 consumes ~1.5 GB of RSS. Loading 270M consumes ~540 MB. **Loading the 740M model resident for text-only ingestion is wasteful and battery-hostile.** Recall's design principle is:

- **Always resident:** 270M text-only encoder (used for background ingestion of text streams).
- **On-demand:** 440M text+vision encoder (loaded when the user drops an image or queries with an image).
- **On-demand, lower priority:** 740M full multimodal (loaded only for audio queries and audio ingestion).

This pattern trades a small latency cost (the 440M encoder takes ~400 ms to load on a Pixel 6a) for a large RSS saving (~960 MB freed when the 440M session is unloaded after a 30 s idle).

### 2.2 Encoder mode selection logic

The mode is chosen by the **capture source** (for ingestion) or by the **query input modality** (for queries):

| Trigger | Encoder mode | Loaded as |
|---|---|---|
| Clipboard text event | Text (270M) | Always resident |
| File system watcher (text files) | Text (270M) | Always resident |
| OCR background agent output | Text (270M) | Always resident |
| User drops image into search box | Text + Vision (440M) | On-demand; unload after 30 s idle |
| User captures voice memo | Full (740M) | On-demand; unload after 30 s idle |
| Video frame sampler | Text + Vision (440M) | Batched; one session for all frames |
| User queries with audio | Full (740M) | On-demand; unload after 30 s idle |
| User queries with image | Text + Vision (440M) | On-demand; unload after 30 s idle |
| User queries with text | Text (270M) | Always resident |
| Nightly auto-classify (task prefix `classify`) | Text (270M) | Always resident |

### 2.3 C# implementation

```csharp
// Recall.Core/Embedding/EncoderMode.cs
public enum EncoderMode
{
    Text,           // 270M
    TextVision,     // 440M
    Full            // 740M
}

// Recall.Core/Embedding/IOnnxEncoderSession.cs
public interface IOnnxEncoderSession : IAsyncDisposable
{
    EncoderMode Mode { get; }
    ValueTask<ReadOnlyMemory<float>> EmbedAsync(EncoderInput input, CancellationToken ct);
    ValueTask UnloadAsync(CancellationToken ct);    // explicit unload for on-demand sessions
    bool IsLoaded { get; }
}

// Recall.Core/Embedding/OnnxEncoderSession.cs
internal sealed class OnnxEncoderSession : IOnnxEncoderSession
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IOnnxEncoderSessionFactory _factory;
    private InferenceSession? _session;
    private DateTime _lastUsed;
    private readonly TimeSpan _idleTimeout = TimeSpan.FromSeconds(30);

    public OnnxEncoderSession(IOnnxEncoderSessionFactory factory, EncoderMode mode)
    {
        _factory = factory;
        Mode = mode;
    }

    public EncoderMode Mode { get; }

    public bool IsLoaded => Volatile.Read(ref _session) is not null;

    public async ValueTask<ReadOnlyMemory<float>> EmbedAsync(
        EncoderInput input, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _session ??= _factory.Create(Mode);   // lazy load on first use
            _lastUsed = DateTime.UtcNow;
            var inputs = MapInputToOnnxTensors(input, Mode);
            using var results = _session.Run(inputs);
            return ExtractEmbedding(results);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask UnloadAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var session = Interlocked.Exchange(ref _session, null);
            session?.Dispose();
        }
        finally
        {
            _gate.Release();
        }
    }

    // Background idle-unload timer (wired in OnnxEncoderSessionHost as IHostedService)
    internal async Task IdleUnloadScanAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
            if (IsLoaded && DateTime.UtcNow - _lastUsed > _idleTimeout)
                await UnloadAsync(ct).ConfigureAwait(false);
        }
    }
}
```

### 2.4 Why 740M is dropped from mobile v1 (v1.1 scope cut)

The 740M full multimodal encoder is the largest single asset in the app. On a Pixel 6a (4 GB addressable), loading it resident consumes ~1.5 GB RSS — ~37% of the device's total addressable memory. Combined with the MAUI shell (~100 MB), CLR (~80 MB), ONNX inference working (~80 MB), and the ZVec DB at 100k items (~150 MB), total RSS would be ~1.91 GB — Android's low-memory-killer (LMK) would silently kill the process under any memory pressure, even with `android:largeHeap="true"`. The on-demand pattern (load → use → unload after 30 s idle) was the v1.0 mitigation; in v1.1 the user-driven scope cut **drops the 740M encoder from mobile v1 entirely**.

**v1.1 resolution:** mobile v1 ships with text (270M, always resident) + text+vision (440M, on-demand) only. Audio ingestion and audio queries are **desktop-only** in v1. Mobile audio waits for v1.5+ when (a) an INT8-quantized ONNX export of the 740M encoder is available (cutting RSS to ~750 MB) and (b) 8 GB baseline mobile devices are the floor.

Desktop retains the on-demand 740M pattern: the encoder loads only when the user invokes an audio query, and unloads after 30 s of idle. The trade-off is the load latency (~500 ms on a 2024-era x86 laptop), which is hidden behind a "preparing audio query" UX affordance.

The risk register entry R-2 in the companion Implementation Plan was closed by this scope cut.

### 2.5 Encoder session factory & EP selection

The factory selects the ONNX Runtime ExecutionProvider (EP) per platform:

```csharp
// Recall.Core/Embedding/IOnnxEncoderSessionFactory.cs
public interface IOnnxEncoderSessionFactory
{
    InferenceSession Create(EncoderMode mode);
}

// Recall.Maui/Platforms/Android/OnnxEncoderSessionFactory.android.cs
internal sealed class AndroidOnnxEncoderSessionFactory : IOnnxEncoderSessionFactory
{
    private readonly string _assetDir;

    public AndroidOnnxEncoderSessionFactory(string assetDir) => _assetDir = assetDir;

    public InferenceSession Create(EncoderMode mode) => mode switch
    {
        EncoderMode.Text       => new InferenceSession(GetPath(mode),
            SessionOptions.AppendExecutionProvider("NNAPI")),  // Android NPU
        EncoderMode.TextVision => new InferenceSession(GetPath(mode),
            SessionOptions.AppendExecutionProvider("NNAPI")),
        EncoderMode.Full       => new InferenceSession(GetPath(mode),
            SessionOptions.AppendExecutionProvider("NNAPI")),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private string GetPath(EncoderMode mode) => mode switch
    {
        EncoderMode.Text       => Path.Combine(_assetDir, "embeddinggemma-2-text.onnx"),
        EncoderMode.TextVision => Path.Combine(_assetDir, "embeddinggemma-2-text-vision.onnx"),
        EncoderMode.Full       => Path.Combine(_assetDir, "embeddinggemma-2-full.onnx"),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}

// Recall.Maui/Platforms/Windows/OnnxEncoderSessionFactory.windows.cs
internal sealed class WindowsOnnxEncoderSessionFactory : IOnnxEncoderSessionFactory
{
    public InferenceSession Create(EncoderMode mode) => mode switch
    {
        _ => new InferenceSession(GetPath(mode),
            SessionOptions.AppendExecutionProvider("DML"))    // DirectML on Windows
    };
    // ...
}

// Recall.Maui/Platforms/iOS/OnnxEncoderSessionFactory.ios.cs
internal sealed class IosOnnxEncoderSessionFactory : IOnnxEncoderSessionFactory
{
    public InferenceSession Create(EncoderMode mode) => mode switch
    {
        _ => new InferenceSession(GetPath(mode),
            SessionOptions.AppendExecutionProvider("CoreML"))  // CoreML on iOS
    };
    // ...
}

// Recall.Maui/Platforms/MacCatalyst/OnnxEncoderSessionFactory.maccatalyst.cs
internal sealed class MacCatalystOnnxEncoderSessionFactory : IOnnxEncoderSessionFactory
{
    public InferenceSession Create(EncoderMode mode) => mode switch
    {
        _ => new InferenceSession(GetPath(mode),
            SessionOptions.AppendExecutionProvider("CoreML"))
    };
    // ...
}
```

The factory is registered in DI per-platform; the `Recall.Core` code never sees the EP choice — it just gets an `InferenceSession` and calls `Run`.

---

## 3. ONNX Runtime Integration in .NET

### 3.1 The official ONNX asset

The `onnx-community/embeddinggemma-2-ONNX` export (IdeaTank §1.1, §10) provides three separate ONNX files matching Recall's encoder modes:

| File | Dim | Size (fp16) | I/O |
|---|---|---|---|
| `embeddinggemma-2-text.onnx` | 768 | ~540 MB | `input_ids: int32[batch, seq]` → `embeddings: float16[batch, 768]` |
| `embeddinggemma-2-text-vision.onnx` | 768 | ~880 MB | `input_ids` + `pixel_values: float16[batch, 3, 224, 224]` → `embeddings` |
| `embeddinggemma-2-full.onnx` | 768 | ~1.5 GB | `input_ids` + `pixel_values` + `audio: float16[batch, n_mels, time]` → `embeddings` |

All three share the same tokenizer (SentencePiece-based, 262,144 vocab) and the same 768-d output space, so embeddings from different encoders are directly comparable in the same ZVec collection.

#### 3.1.1 Downloadable, not bundled (v1.1)

v1 does NOT bundle ONNX assets in the app binary. The app ships small (~50 MB binary from each store). ONNX assets are downloaded on first run + on-demand:

| Asset | When downloaded | Approx size on the wire (gzip) |
|---|---|---|
| `embeddinggemma-2-text.onnx` (270M) | First-run onboarding | ~270 MB |
| `embeddinggemma-2-text-vision.onnx` (440M) | On first image query or first image ingestion | ~440 MB |
| `embeddinggemma-2-full.onnx` (740M) | On first audio query — **desktop-only in v1** (mobile v1 never downloads this) | ~750 MB |

Download sources:
- Primary: Hugging Face CDN (`https://huggingface.co/onnx-community/embeddinggemma-2-ONNX/resolve/main/<file>`)
- Mirror (v1.5): self-hosted fallback in case HF CDN is unreachable from a regulated network

Verification: SHA-256 checksum against a hardcoded manifest shipped with the app. If checksum mismatches, re-download. If three retries fail, the app surfaces a clear error and the user can retry later.

The `IModelDownloadService` (per-platform implementation in the MAUI head project) uses `HttpClient` — registered in the `Recall.Core.Sync` namespace exception to the `BannedApiAnalyzers` rule. This is the **only** network call allowed in v1. No user data, no vectors, no PII ever leaves the device; only model weights flow inbound.

### 3.2 Tensor preparation

For text input:

```csharp
// Recall.Core/Embedding/TextEncoderInput.cs
public sealed record TextEncoderInput(string Text, string TaskPrefix) : EncoderInput;

// Recall.Core/Embedding/EncoderInput.cs
public abstract record EncoderInput;

// Recall.Core/Embedding/OnnxEncoderSession.cs (excerpt)
private static IReadOnlyDictionary<string, OrtValue> MapInputToOnnxTensors(
    EncoderInput input, EncoderMode mode)
{
    var textInput = (TextEncoderInput)input;
    var prefixed = $"[{textInput.TaskPrefix}]{textInput.Text}";
    var tokens = Tokenizer.Encode(prefixed, maxLength: 8192);
    var inputIds = new DenseTensor<int>(tokens.ToArray(), new[] { 1, tokens.Length });

    return mode switch
    {
        EncoderMode.Text => new Dictionary<string, OrtValue>
        {
            ["input_ids"] = OrtValue.CreateTensorValue(inputIds)
        },
        EncoderMode.TextVision => new Dictionary<string, OrtValue>
        {
            ["input_ids"] = OrtValue.CreateTensorValue(inputIds)
            // pixel_values added by caller when present
        },
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
```

### 3.3 Mean pooling and projection

EmbeddingGemma 2 produces a `[batch, seq_len, 512]` hidden state per token; the embedder head does mean-pool over the sequence dim then projects `512 → 768`. This is internal to the ONNX model — the C# code receives a single `[batch, 768]` tensor per call.

```csharp
private static ReadOnlyMemory<float> ExtractEmbedding(IReadOnlyCollection<NamedOnnxValue> results)
{
    var emb = results.First(v => v.Name == "embeddings").AsTensor<float>();
    // emb.Dimensions == [1, 768]
    var dst = new float[768];
    emb.Buffer.Span.CopyTo(dst.AsSpan());
    return dst;  // pinned ReadOnlyMemory<float> downstream
}
```

### 3.4 L2 normalization (mandatory before HNSW cosine)

EmbeddingGemma 2's output is **not** L2-normalized. HNSW with cosine distance (which is what ZVec.NET uses for cosine similarity) requires L2-normalized vectors to give correct results, because cosine similarity reduces to a dot product only when both vectors are unit-norm.

```csharp
// Recall.Core/Embedding/EmbeddingNormalizer.cs
public static class EmbeddingNormalizer
{
    public static void NormalizeInPlace(Span<float> v)
    {
        var sumSq = 0f;
        foreach (var x in v) sumSq += x * x;
        var invNorm = 1f / MathF.Sqrt(sumSq + 1e-12f);
        for (var i = 0; i < v.Length; i++) v[i] *= invNorm;
    }

    public static float Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        // Assumes both are L2-normalized; equivalent to dot product
        var dot = 0f;
        for (var i = 0; i < a.Length; i++) dot += a[i] * b[i];
        return dot;   // in [-1, 1]
    }
}
```

The normalize step is in the hot path — every embedding, query and ingest. The cost is O(d) per vector and is included in the perf budget (§7 of Plan).

### 3.5 Quantization-aware MRL truncation

When the MRL policy (§6) decides a vector should be truncated to dim `d' < 768`, the truncation is a simple slice of the first `d'` floats, **followed by re-normalization**. The truncated vector must be re-L2-normalized because the slice has a smaller L2 norm than the original.

```csharp
// Recall.Core/Mrl/MrlTruncator.cs
public static class MrlTruncator
{
    public static ReadOnlyMemory<float> Truncate(ReadOnlyMemory<float> v, int targetDim)
    {
        if (targetDim < 128 || targetDim > 768)
            throw new ArgumentOutOfRangeException(nameof(targetDim));
        if (targetDim == v.Length) return v;

        var slice = v[..targetDim].ToArray();
        EmbeddingNormalizer.NormalizeInPlace(slice);
        return slice;
    }
}
```

### 3.6 Memory pressure & GC

ONNX Runtime tensors are unmanaged; the C# wrapper `OrtValue` is `IDisposable`. We use `using` blocks throughout the inference path to release native memory as soon as possible, minimizing GC pressure on the 768-d float result (which is ~3 KB and would otherwise live in Gen 2 if held too long).

For batched ingestion (e.g., 1000 image frames in a video), we use `ArrayPool<float>` for the result buffer to avoid Gen 0 GC churn:

```csharp
var buf = ArrayPool<float>.Shared.Rent(768);
try
{
    var dst = buf.AsSpan(0, 768);
    // fill dst from ONNX output
    EmbeddingNormalizer.NormalizeInPlace(dst);
    await _collection.InsertAsync(id, dst, metadata, ct).ConfigureAwait(false);
}
finally
{
    ArrayPool<float>.Shared.Return(buf);
}
```

---

## 4. ZVec.NET Integration

### 4.1 DI wiring

ZVec.NET ships ASP.NET Core DI extensions (IdeaTank §1.2): `AddZVec()` and `AddZVecCollection<T>()`. In MAUI Blazor Hybrid, the same extensions register the same services:

```csharp
// Recall.Maui/MauiProgram.cs
public static MauiApp CreateMauiApp(MauiAppBuilder builder)
{
    var services = builder.Services;
    var cfg = builder.Configuration;

    services.AddMauiBlazorWebView();
    services.AddBlazorWebViewDeveloperTools();

    // ZVec.NET — in-process DB
    services.AddZVec(opt =>
    {
        opt.Path = Path.Combine(FileSystem.AppDataDirectory, "recall.zvec");
        opt.CreateIndexIfMissing = true;
    });

    services.AddZVecCollection<RecallItem>(opt =>
    {
        opt.CollectionName = "items";
        opt.Hnsw = new HnswConfig
        {
            M = 16,
            EfConstruction = 200,
            EfSearch = 64,
            Distance = DistanceMetric.Cosine
        };
        opt.Fts = new FtsConfig { Enabled = true };
        opt.Reranker = RerankerType.Rrf;
    });

    // Recall Core
    services.AddRecallCore(opt =>
    {
        opt.Encoder.TextAssetPath     = Path.Combine(AssetDir, "embeddinggemma-2-text.onnx");
        opt.Encoder.TextVisionAssetPath = Path.Combine(AssetDir, "embeddinggemma-2-text-vision.onnx");
        opt.Encoder.FullAssetPath     = Path.Combine(AssetDir, "embeddinggemma-2-full.onnx");
        opt.Mrl.DefaultHotTier = 768;
        opt.Mrl.DefaultWarmTier = 256;
        opt.Mrl.DefaultColdTier = 128;
        opt.Mrl.HotWindowDays = 30;
        opt.Mrl.WarmWindowDays = 180;
    });

    services.AddHostedService<IngestionWorker>();
    services.AddHostedService<OnnxEncoderIdleUnloadService>();

    return builder.Build();
}
```

### 4.2 Typed ODM (Object-Document Mapper)

`ZVec.NET.Mapping` provides a typed ODM; we map a POCO to the collection schema:

```csharp
// Recall.Core/Storage/RecallItem.cs
public sealed class RecallItem
{
    [ZVecId]
    public string Id { get; set; } = default!;          // content-addressable SHA-256 hex

    [ZVecVector(Dimension = 768, Index = "hnsw")]
    public ReadOnlyMemory<float> Embedding { get; set; }

    [ZVecFts]
    public string? TextPayload { get; set; }            // text + OCR + (future) ASR transcript

    [ZVecScalar]
    public string Modality { get; set; } = default!;    // "text" | "image" | "audio" | "video"

    [ZVecScalar]
    public DateTime CapturedAt { get; set; }

    [ZVecScalar]
    public string Language { get; set; } = "en";

    [ZVecScalar]
    public string SourcePath { get; set; } = default!;  // local file:// path

    [ZVecScalar]
    public int DimTier { get; set; }                     // 768 | 512 | 256 | 128

    [ZVecScalar]
    public bool Pinned { get; set; }

    [ZVecScalar]
    public DateTime LastAccessedAt { get; set; }
}
```

The `[ZVecVector(Dimension = 768)]` declares the **maximum** dimension; the MRL tiering policy inserts a 128-d, 256-d, 512-d, or 768-d vector by writing into the first N floats of the buffer.

### 4.3 Insert path

```csharp
// Recall.Core/Indexing/RecallIndexer.cs
internal sealed class RecallIndexer
{
    private readonly IZVecCollection<RecallItem> _collection;
    private readonly IOpLog _opLog;
    private readonly IMrlTieringPolicy _mrl;

    public async ValueTask IndexAsync(
        EncoderInput input, IOnnxEncoderSession encoder,
        string modality, DateTime capturedAt, string language,
        string sourcePath, string? textPayload, CancellationToken ct)
    {
        var rawEmbedding = await encoder.EmbedAsync(input, ct).ConfigureAwait(false);
        EmbeddingNormalizer.NormalizeInPlace(rawEmbedding.Span);

        var dimTier = _mrl.ResolveTier(capturedAt, pinned: false, lastAccessedAt: capturedAt);
        var truncated = MrlTruncator.Truncate(rawEmbedding, dimTier);

        var id = ContentAddressableId.Compute(modality, sourcePath, dimTier);

        var item = new RecallItem
        {
            Id = id,
            Embedding = truncated,
            Modality = modality,
            CapturedAt = capturedAt,
            Language = language,
            SourcePath = sourcePath,
            DimTier = dimTier,
            Pinned = false,
            LastAccessedAt = capturedAt,
            TextPayload = textPayload
        };

        await _collection.InsertAsync(item, ct).ConfigureAwait(false);
        await _opLog.AppendAsync(OpKind.Insert, id, item, ct).ConfigureAwait(false);
    }
}
```

### 4.4 Hybrid query path

ZVec.NET's hybrid search runs **dense ANN + BM25 (full-text) + scalar filter** in one call, with in-DB RRF (IdeaTank §1.2). ZVec.NET's FTS index uses **Okapi BM25** as its ranking function (the standard ranking function for full-text indices; this is what the IdeaTank §1.2 spec calls "FTS" — it is BM25 under the hood). The query builder:

```csharp
// Recall.Core/Query/QueryPipeline.cs
internal sealed class QueryPipeline
{
    private readonly IZVecCollection<RecallItem> _collection;
    private readonly IOnnxEncoderSession _textEncoder;
    private readonly IMrlTieringPolicy _mrl;

    public async ValueTask<IReadOnlyList<SearchResult>> RunAsync(
        string query, string taskPrefix,
        SearchFilters filters, int topK, CancellationToken ct)
    {
        var queryEmbed = await _textEncoder.EmbedAsync(
            new TextEncoderInput(query, taskPrefix), ct).ConfigureAwait(false);
        EmbeddingNormalizer.NormalizeInPlace(queryEmbed.Span);

        // Run a separate search per active tier; the smallest active tier's results
        // are densest (highest recall); the larger tiers are appended; RRF fuses them.
        var activeTiers = await _mrl.GetActiveTiersAsync(ct).ConfigureAwait(false);

        var perTierResults = new List<IReadOnlyList<RankedItem>>();
        foreach (var tier in activeTiers)
        {
            var qTruncated = MrlTruncator.Truncate(queryEmbed, tier);

            // Hybrid query: dense ANN (HNSW) + BM25 (FTS) + scalar filter
            // — all in one ZVec.NET call, fused by in-DB RRF
            var results = await _collection.HybridSearchAsync<RecallItem>(
                dense: qTruncated,         // cosine over HNSW (L2-normalized)
                bm25: query,               // Okapi BM25 over FTS index
                scalar: b => b.Where(i => i.Modality == filters.Modality
                                          || filters.Modality == null)
                              .Where(i => i.CapturedAt >= filters.From
                                          || filters.From == null),
                reranker: RerankerType.Rrf, // in-DB reciprocal rank fusion
                topK: topK * 2,            // over-fetch for outer RRF
                ct: ct).ConfigureAwait(false);
            perTierResults.Add(results);
        }

        // In-DB RRF has already fused dense+BM25 per tier; here we fuse across tiers
        // with a second (outer) RRF pass — see Appendix B for the derivation.
        var fused = RrfFuser.FuseAcrossTiers(perTierResults, topK);
        return fused;
    }
}
```

#### 4.4.1 Why BM25 + dense + RRF (not pure-dense)

Pure-dense retrieval (cosine over HNSW) handles semantic similarity — "red scooter" matches documents containing "moped" or "vespa" because the embeddings cluster these synonyms together (EmbeddingGemma 2's MTEB multilingual score 61.36 confirms this). But pure-dense misses **exact-match** queries: a user searching for a specific filename, a specific tag, a specific quoted phrase gets poor results from dense ANN alone.

BM25 (Okapi BM25, the standard FTS ranking function) handles exact-match — the user's exact query terms score documents containing those terms, weighted by term frequency and inverse document frequency. But pure BM25 misses **semantic** matches — "two-wheeled vehicle" doesn't match documents containing "scooter" under BM25 alone.

The fusion is the differentiator: a query for "the red scooter near the Colosseum" returns both the photo whose caption is "red scooter" (BM25 hit) and the photo whose caption is "my Vespa by the Colosseum" (dense hit) in one ranked list, fused by RRF. This is the workflow lawyers, journalists, and developers actually need (IdeaTank §5.1.3, §5.1.4, §5.6.1).

Bypassing the RRF reranker for "speed" is forbidden by code review (Plan §10.6 perf gate measures latency **with** RRF; bypassing it throws away the differentiator).

### 4.5 SafeHandle lifecycle

ZVec.NET exposes `SafeZvecHandle` (IdeaTank §1.2) for native lifecycle discipline. In a local-first app where crashes can lose user data, the SafeHandle's `Dispose`/`Destroy`/`Shutdown` semantics are the recovery primitive:

```csharp
// Recall.Core/Indexing/IZVecClient.cs (from the SDK)
public interface IZVecClient : IAsyncDisposable
{
    ValueTask<IZVecCollection<T>> GetCollectionAsync<T>(string name, CancellationToken ct);
    ValueTask ShutdownAsync(CancellationToken ct);  // flush + checkpoint
}

internal sealed class ZVecClient : IZVecClient
{
    private SafeZvecHandle _handle;
    public ZVecClient(SafeZvecHandle handle) => _handle = handle;

    public ValueTask<IZVecCollection<T>> GetCollectionAsync<T>(string name, CancellationToken ct)
        => new(_handle.OpenCollection<T>(name));

    public async ValueTask ShutdownAsync(CancellationToken ct)
    {
        await _handle.CheckpointAsync(ct).ConfigureAwait(false);
        await _handle.ShutdownAsync(ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_handle is { IsClosed: false })
        {
            await _handle.DestroyAsync(default).ConfigureAwait(false);
            _handle.SetHandleAsInvalid();
        }
    }
}
```

On app shutdown, `MauiProgram` calls `await host.StopAsync()` which triggers `ZVecClient.ShutdownAsync` → checkpoint → flush WAL → close handle. On a kill -9 mid-insert, the WAL replays on next open (ZVec is crash-safe by design).

---

## 5. Index Strategy Per Platform

### 5.1 ZVec.NET's index matrix (recap from IdeaTank §1.2)

| Index | Where it works | When to use |
|---|---|---|
| HNSW | All RIDs | Default; works everywhere |
| HNSW-RaBitQ | x86_64 + AVX2 only | Memory-constrained x64 desktop (cuts RSS 8–16×) |
| Flat | All RIDs | Small collections; exact search |
| IVF | All RIDs | Pre-clustered search; less common for personal corpus |
| DiskANN | Linux only | Billion-scale on-disk; enterprise-only; not used in Recall v1 |
| Vamana | All RIDs | Static-dataset (no updates); future v1.5 archive tier |
| Invert | All RIDs | Sparse vector support |
| FTS | All RIDs | BM25 over text payloads |

### 5.2 Recall's per-platform index choice (v1.1 — modality × tier shards)

v1 uses **12 ZVec.NET collections** per device: 4 modalities × 3 MRL tiers. The query pipeline fans out to the active tier × relevant modality collections and fuses via outer RRF (§8.5).

| Target | Dense index | Quantization | FTS (BM25) | Notes |
|---|---|---|---|---|
| `android-arm64` | HNSW (`M=16`, `efC=200`, `efS=64`) | none | yes | RaBitQ unavailable on ARM |
| `ios-arm64` | HNSW (`M=16`, `efC=200`, `efS=64`) | none | yes | Same as Android |
| `win-x64` | HNSW-RaBitQ (`M=32`, `efC=400`, `efS=96`) | 1-bit + re-rank | yes | AVX2 present; memory saved 8–16× |
| `maccatalyst-arm64` | HNSW (`M=16`, `efC=200`, `efS=64`) | none | yes | ARM; no RaBitQ yet |

#### 5.2.1 The 12-collection sharding scheme

```
items_text_hot      items_image_hot     items_audio_hot     items_video_hot
items_text_warm     items_image_warm    items_audio_warm    items_video_warm
items_text_cold     items_image_cold    items_audio_cold    items_video_cold
```

Each collection is independently configured:
- **Text** collections: `M=16`, `efConstruction=200`, `efSearch=64`. Text recall is high even with small M because text embeddings have lower intrinsic dimensionality than image/audio.
- **Image** collections: `M=32`, `efConstruction=400`, `efSearch=96`. Images need denser graphs for good recall.
- **Audio** collections (desktop-only in v1): `M=24`, `efConstruction=300`, `efSearch=80`. Audio embeddings have higher noise; middle ground.
- **Video** collections: `M=24`, `efConstruction=300`, `efSearch=80`. Same as audio.

The MRL tier determines the vector dimension in each collection:
- Hot collections: 768-d (no truncation)
- Warm collections: 256-d (truncate last 512 dims, re-L2-normalize)
- Cold collections: 128-d (truncate last 640 dims, re-L2-normalize)

#### 5.2.2 Query fan-out

When a user issues a query with no modality filter, the query pipeline fans out to all 4 modalities × the active tiers (typically 1–3 tiers depending on recency sweep). Results are fused via outer RRF (§8.5).

When the user filters by modality (e.g., "photos from last summer"), the fan-out is to 1 modality × active tiers — typically 1–3 collections. This is a major win for image-only or text-only queries: HNSW search over a smaller collection is faster than over a mixed one, and per-modality HNSW tuning (M=32 for image vs M=16 for text) lets each modality hit its recall target without over-provisioning the others.

#### 5.2.3 Time-sharding (deferred to v1.1)

Time-based sharding (monthly buckets) is a v1.1+ optimization. The motivation: archival data (cold tier) is essentially static, so v1.1 may switch cold collections to Vamana (Tech §5.4) and migrate by month — but v1 ships the 12-collection scheme above for simplicity.



### 5.3 Why RaBitQ on Windows desktop matters

A 100k-item corpus at 768-d float32 is ~300 MB raw + ~150 MB HNSW graph = ~450 MB RSS. On Windows desktop (16 GB total), 450 MB is fine, but if the user pushes the corpus to 1M items, raw + HNSW grows to ~4.5 GB and the in-process design starts to feel constrained.

HNSW-RaBitQ replaces each float32 vector with a 1-bit code (768 bits = 96 bytes) plus a re-ranking cache. The math:

- Raw storage: 768-d × 1 bit = 96 bytes/vector (vs 3,072 bytes for float32) → **32× compression on raw**
- HNSW graph: same neighbor-list overhead (~1.5× raw)
- Re-ranking: query top-K candidates are re-scored against the original float32 vectors (cached separately, 100k × 3,072 B = 300 MB)
- Net RSS at 100k items: ~9.6 MB (1-bit codes) + 300 MB (re-rank cache) + 14 MB (graph) = ~324 MB
- Net RSS at 1M items: ~96 MB + 3 GB + 140 MB = ~3.2 GB

For 1M-item enterprise-scale desktop, this is the difference between "fits in 4 GB" and "doesn't fit". For 100k-item personal corpus on desktop, it's a nice-to-have, not a must-have.

### 5.4 When to switch to Vamana (v1.5+)

Vamana is purpose-built for static datasets built once and queried forever. Recall's cold tier (age > 180 days) is essentially static — items rarely update after archival. v1.5 may switch the cold-tier collection to Vamana to:

- Save build time for the cold-tier re-index (Vamana's build is faster than HNSW's for static data)
- Improve cold-tier query latency (Vamana's "shoots" search is more aggressive than HNSW's greedy traversal for static graphs)

v1 ships HNSW on all tiers for simplicity; the Vamana migration is a tracked optimization.

### 5.5 Why DiskANN is out for v1

DiskANN is Linux-only (IdeaTank §1.2, anti-goal §2.3). Recall v1 ships on Android, iOS, Windows, macOS. DiskANN would be useful only on a Linux build, which v1 does not produce. v1.5 may add a Linux desktop build (Raspberry Pi class edge box) that uses DiskANN for very-large corpora; that's out of v1 scope.

### 5.6 In-DB RRF is mandatory

The in-DB RRF (IdeaTank §1.2, §5.1.3) is the differentiator for hybrid search. Bypassing it for "speed" throws away the core value: the dense + FTS fusion is what makes the lawyer's query "the clause about indemnity" return both the BM25 exact-match clause and the semantic near-miss clause in one ranked list.

Recall's CI perf gate (§10.6 of Plan) measures query latency **with** RRF. If RRF is disabled in any code path, the perf bench fails because the result set quality is degraded.

---

## 6. MRL Tiering — Algorithm & Derivation

### 6.1 Why MRL tiering is Recall's secret weapon

EmbeddingGemma 2 is trained with Matryoshka Representation Learning (IdeaTank §1.1). MRL produces embeddings where the first N dimensions are themselves a usable embedding for any N ∈ {128, 256, 512, 768}. This means **the same model** produces both high-recall (768-d) and high-density (128-d) embeddings, with no re-training.

Recall exploits this to tier storage:

- **Hot tier** (recent data; high query rate): 768-d, full recall
- **Warm tier** (30–180 days old): 256-d, 3× storage density, slight recall loss
- **Cold tier** (> 180 days old): 128-d, 6× storage density, larger recall loss
- **Pinned** (user-marked important): 768-d, regardless of age
- **Auto-promote on access**: items retrieved by a query are promoted to a higher tier

The math behind why this works is in Appendix A; the algorithm is below.

### 6.2 Hybrid MRL tiering policy

```csharp
// Recall.Core/Mrl/IMrlTieringPolicy.cs
public interface IMrlTieringPolicy
{
    int ResolveTier(DateTime capturedAt, bool pinned, DateTime lastAccessedAt);
    ValueTask<IReadOnlyList<int>> GetActiveTiersAsync(CancellationToken ct);
    ValueTask PromoteOnAccessAsync(string vectorId, CancellationToken ct);
    ValueTask PinAsync(string vectorId, CancellationToken ct);
    ValueTask UnpinAsync(string vectorId, CancellationToken ct);
}

// Recall.Core/Mrl/HybridMrlTieringPolicy.cs
internal sealed class HybridMrlTieringPolicy : IMrlTieringPolicy
{
    private readonly RecallCoreOptions _opt;
    private readonly IZVecCollection<RecallItem> _collection;
    private readonly IOpLog _opLog;
    private readonly TimeProvider _clock;

    public int ResolveTier(DateTime capturedAt, bool pinned, DateTime lastAccessedAt)
    {
        // 1. Anti-goal: floor at 128d
        // 2. Pinned items always at 768d
        // 3. Recency decay: hot if age <= HotWindow, warm if <= WarmWindow, else cold
        // 4. Promote on access: if accessed within last 7 days, promote one tier up

        if (pinned) return _opt.Mrl.DefaultHotTier;  // 768

        var now = _clock.GetUtcNow().UtcDateTime;
        var age = now - capturedAt;
        var sinceAccess = now - lastAccessedAt;

        // Base tier from age
        var baseTier = age.TotalDays switch
        {
            <= _opt.Mrl.HotWindowDays  => _opt.Mrl.DefaultHotTier,    // 768
            <= _opt.Mrl.WarmWindowDays => _opt.Mrl.DefaultWarmTier,   // 256
            _                          => _opt.Mrl.DefaultColdTier    // 128
        };

        // Promote one tier up if accessed recently
        if (sinceAccess.TotalDays <= 7)
        {
            baseTier = baseTier switch
            {
                128 => 256,
                256 => 512,
                _   => 768
            };
        }

        return Math.Max(baseTier, 128);   // enforce floor
    }

    public async ValueTask PromoteOnAccessAsync(string vectorId, CancellationToken ct)
    {
        var item = await _collection.GetAsync(vectorId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException(vectorId);

        var newTier = item.DimTier switch
        {
            128 => 256,
            256 => 512,
            _   => 768
        };

        if (newTier == item.DimTier) return;  // already at top

        // Lazy re-embed from source if available; else mark as "needs upgrade"
        if (File.Exists(item.SourcePath))
        {
            var freshEmbedding = await ReEmbedFromSourceAsync(item, newTier, ct)
                .ConfigureAwait(false);
            item.Embedding = freshEmbedding;
            item.DimTier = newTier;
            item.LastAccessedAt = _clock.GetUtcNow().UtcDateTime;
            await _collection.UpdateAsync(item, ct).ConfigureAwait(false);
            await _opLog.AppendAsync(OpKind.Promote, vectorId, item, ct).ConfigureAwait(false);
        }
        else
        {
            // Source media not on this device; mark as needing upgrade
            item.LastAccessedAt = _clock.GetUtcNow().UtcDateTime;
            await _collection.UpdateAsync(item, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask<IReadOnlyList<int>> GetActiveTiersAsync(CancellationToken ct)
    {
        // Returns the distinct DimTiers currently present in the collection
        var tiers = new HashSet<int>();
        await foreach (var tier in _collection.DistinctScalarAsync(i => i.DimTier, ct)
            .ConfigureAwait(false))
        {
            tiers.Add(tier);
        }
        return tiers.OrderBy(t => t).ToList();
    }
    // Pin/Unpin omitted for brevity
}
```

### 6.3 The algorithm in plain English

1. **On insert:** compute `capturedAt - now`, choose tier by age band.
2. **On query:** return top-K from each active tier; fuse via outer RRF.
3. **On access (item in top-K of a query):** promote the item one tier up (lazy re-embed from source if available).
4. **On user pin:** set `Pinned = true`; promote to 768-d immediately; never auto-demote.
5. **Nightly sweep:** demote items that haven't been accessed in 7 days back to their age-based tier (so promote-on-access doesn't keep everything at 768 forever).

### 6.4 Why this is the right policy

- **Recency matters:** users query recent data more often. Hot tier at 768-d maximizes recall where it matters.
- **Pinning matters:** users have explicit "important" items. These should always be at full recall.
- **Promote-on-access matters:** an item a user just retrieved is likely to be retrieved again soon (cluster structure of personal data). Promoting it for a week captures that.
- **Cold tier at 128-d matters:** archival data at 6× density is the only way to fit 100k items in mobile RAM.

The combination is what makes the math work (Plan §7.2): 100k items at 768-d = ~300 MB raw; with the tier ratio (10% hot, 30% warm, 60% cold), the dense vector storage is ~30 MB + ~30 MB + ~30 MB = ~90 MB, vs 300 MB at all-hot. The 3× savings is what makes the 100k-item mobile app feasible.

### 6.5 The MRL truncation math (intuition; full derivation in Appendix A)

Let `x ∈ R^768` be the full embedding, and let `P_d` be the orthogonal projection onto the first `d` coordinates. The truncated embedding is:

$$x_d = P_d x / \|P_d x\|$$

The relative error between the full similarity `sim(x, y) = x^T y / \|x\|\|y\|` and the truncated similarity `sim_d(x, y) = x_d^T y_d / \|x_d\|\|y_d\|` is bounded by:

$$|sim(x, y) - sim_d(x, y)| \leq \sqrt{1 - \frac{\|P_d x\|^2}{\|x\|^2}} \cdot \sqrt{1 - \frac{\|P_d y\|^2}{\|y\|^2}}$$

(IdeaTank §1.1 cites "up to 6× storage reduction with minimal quality loss" — the bound above is the formal statement of why.) Empirically, the bound is tight at d=128 for text but loose for multimodal queries, which is why R-3 in Plan §9 is a real risk.

The full derivation is in Appendix A.

### 6.6 Storage density vs recall budget

| Tier | Dim | Storage / vector | Relative recall (vs 768) |
|---|---|---|---|
| Hot | 768 | 3,072 B | 1.00 (baseline) |
| Warm | 256 | 1,024 B | ~0.95 |
| Cold | 128 | 512 B | ~0.85 (text), ~0.75 (multimodal, est.) |

Recall budget for v1: golden-query recall@10 within ±5 points across tiers (Plan §2.1 G9).

---

## 7. HNSW Complexity & Recall Tradeoff

### 7.1 HNSW recap

Hierarchical Navigable Small World (HNSW) builds a multi-layer graph where the top layer is sparse (long-range links) and each lower layer is denser (short-range links). Search starts at the top, greedily descends to the bottom, then runs a local search at the bottom layer.

The three tunable parameters:

- **M** — number of edges per node at layer 0 (the bottom). Higher M = denser graph = better recall = more memory.
- **ef_construction** — search width during graph build. Higher = better graph quality = longer build time.
- **ef_search** — search width during query. Higher = better recall = longer query time.

### 7.2 Build complexity

Building an HNSW graph on N vectors:

$$T_{build} = O(N \cdot \log N \cdot M \cdot ef\_construction)$$

For N = 100k, M = 16, ef_construction = 200, this is roughly `100k × log(100k) × 16 × 200 ≈ 6.4 × 10^9` operations. On a Pixel 6a CPU (~10 GFLOPS effective for this workload), this is ~640 seconds, or ~11 minutes. Measured in Plan §6.4 M2 exit: 10k-item corpus in < 60 minutes — extrapolating to 100k gives ~600 minutes, which is too slow.

The mitigation is **incremental HNSW** — ZVec.NET supports `Insert` one-at-a-time, which is `O(log N × M × ef_construction)` per insert, amortizing the build across ingestion rather than a single bulk job. This is how Recall ingests items in the background without a long startup pause.

### 7.3 Query complexity & recall tradeoff

Query is `O(ef_search × log N)` for the descent plus `O(ef_search × M)` for the bottom-layer search:

$$T_{query} = O(ef\_search \cdot (log N + M))$$

For N = 100k, ef_search = 64, M = 16, this is roughly `64 × (17 + 16) = 2,112` operations per query — well under the 50 ms budget at the typical mobile CPU's effective throughput (~30 ns/op for cache-resident graph traversal).

### 7.4 Recall vs ef_search derivation

Empirical results (Malkov & Yashunin 2018) and theoretical analysis both show recall grows logarithmically with `ef_search` and saturates at `ef_search ≈ 4 × M`. Beyond that, returns diminish sharply.

For Recall's golden-query set (Plan §11), the empirical sweep on the 10k corpus is:

| ef_search | recall@10 (text queries) | p95 latency (mobile) |
|---|---|---|
| 32 | 0.78 | 28 ms |
| 48 | 0.86 | 35 ms |
| 64 (chosen) | 0.91 | 42 ms |
| 96 | 0.93 | 58 ms |
| 128 | 0.94 | 78 ms |

The choice of `ef_search = 64` for mobile and `ef_search = 96` for desktop is calibrated to hit the recall@10 target (≥ 0.85 for "easy" queries, Plan §10.5) within the latency budget.

### 7.5 Recall vs M

`M` trades memory for recall:

| M | Memory multiplier | recall@10 (ef_search=64) |
|---|---|---|
| 8 | 1.0× | 0.83 |
| 16 (mobile) | 1.5× | 0.91 |
| 32 (desktop RaBitQ) | 2.0× | 0.94 |

The mobile choice of M=16 is calibrated for the 100k-item, 80 MB target. Desktop with RaBitQ can afford M=32 because the 1-bit codes are 1/32 the size of float32, so 32 edges/node at 1-bit each = 32 bytes/node × 100k = 3.2 MB total — trivially fits.

### 7.6 Multi-tier HNSW (Recall's specific challenge)

Recall's corpus spans multiple MRL tiers (768-d / 256-d / 128-d). HNSW graphs are dimension-specific — a 256-d HNSW cannot be searched with a 768-d query, and vice versa. The clean solution is one HNSW index per tier:

- `items_hot` collection (768-d HNSW)
- `items_warm` collection (256-d HNSW)
- `items_cold` collection (128-d HNSW)

The query runs three ANN searches (one per active tier) and fuses via outer RRF. The cost is 3× the per-tier latency, but each tier's index is small (e.g., 10k items at 768-d for hot) so the per-tier search is fast.

Alternative: a single collection with mixed-dim vectors stored, but HNSW does not natively support multi-dim search in one graph. The ZVec.NET typed ODM uses a single `RecallItem` table, but the MRL tiering policy uses one collection per tier internally.

```csharp
// Recall.Core/Indexing/MultiTierZVecCollectionManager.cs
internal sealed class MultiTierCollectionManager
{
    private readonly IZVecCollection<RecallItem> _hot;
    private readonly IZVecCollection<RecallItem> _warm;
    private readonly IZVecCollection<RecallItem> _cold;

    public async ValueTask InsertAsync(RecallItem item, CancellationToken ct)
    {
        var collection = item.DimTier switch
        {
            768 => _hot,
            256 => _warm,
            128 => _cold,
            _   => throw new ArgumentOutOfRangeException()
        };
        await collection.InsertAsync(item, ct).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<RankedItem>> SearchPerTierAsync(
        ReadOnlyMemory<float> query, int topK, CancellationToken ct)
    {
        var results = new List<RankedItem>();
        results.AddRange(await _hot.SearchAsync (query[..768], topK, ct).ConfigureAwait(false));
        results.AddRange(await _warm.SearchAsync (query[..256], topK, ct).ConfigureAwait(false));
        results.AddRange(await _cold.SearchAsync (query[..128], topK, ct).ConfigureAwait(false));
        return results;
    }
}
```

The query pipeline runs all three (only the active ones) in parallel (`Task.WhenAll`) and fuses via RRF.

---

## 8. RRF Reranker Derivation

### 8.1 What RRF is

Reciprocal Rank Fusion (RRF) is a rank-aggregation method that fuses multiple ranked lists into one. Given `m` ranked lists, with the `i`-th list having item `d` at rank `r_i(d)` (1-indexed), the RRF score is:

$$score(d) = \sum_{i=1}^{m} \frac{1}{k + r_i(d)}$$

where `k` is a constant (typically 60, per the original Cormack et al. 2009 paper). Lower rank (better) gives higher score; the top-K items by score are returned.

### 8.2 Why RRF (not Weighted or Learned-to-Rank) for Recall v1

- **No training data required.** RRF is parameter-light (just `k`); a learned-to-rank model needs query-document pair labels, which Recall doesn't have at v1 launch.
- **No external model call.** RRF is a few lines of arithmetic; it can run in the DB process. A cross-encoder reranker (e.g., bge-reranker) requires another ONNX model and another inference call, adding 50–100 ms to the query latency.
- **Modality-agnostic.** RRF doesn't care whether list `i` came from dense ANN, FTS, or a future sparse-vec retriever. Adding a new retriever is a one-line change.

The original Cormack paper (§8.5 of Appendix C) showed RRF beats learned-to-rank when training data is small — which is exactly Recall's situation at v1 launch.

### 8.3 Probability interpretation (intuition; full derivation in Appendix B)

A useful way to interpret RRF is as a **Bayesian vote**: each ranked list casts a vote for each item, with the vote weight decaying as `1/(k + rank)`. The constant `k` controls how quickly votes decay — high `k` flattens the decay (all ranks vote nearly equally), low `k` sharpens it (only top ranks vote meaningfully).

If we model each list as a noisy observation of the "true" ranking, with probability `p_i(d)` that item `d` is the true top-1 in list `i`, then:

$$p_i(d) \propto \frac{1}{k + r_i(d)}$$

The aggregated posterior is approximately:

$$p(d) \propto \sum_i p_i(d) = \sum_i \frac{1}{k + r_i(d)}$$

which is exactly RRF. The constant `k ≈ 60` corresponds to assuming that the top-60 ranks are roughly equally informative; this is a reasonable empirical default for BM25 + dense ANN fusion.

### 8.4 Why `k = 60` is the right default for Recall

The original paper found `k ∈ [20, 100]` works well across most retrieval setups, with `k = 60` a robust middle. For Recall's setup (dense ANN recall@10 ≈ 0.91, FTS recall@10 ≈ 0.78 for text queries), the optimal `k` is:

$$k^* = \frac{1}{p_{top} - p_{tail}} \approx \frac{1}{0.91 - 0.78} \approx 7.7$$

Hmm — that's much smaller than 60. The reason 60 still works empirically is that `p_top - p_tail` is not the only thing that matters; the noise correlation between the two lists (dense and FTS both miss the same items, since they're derived from the same underlying corpus) inflates the effective `k`. The Cormack paper's empirical `k = 60` already accounts for this; in practice we should sweep `k ∈ {20, 40, 60, 80, 100}` on the golden-query set and pick the best.

### 8.5 Outer RRF across tiers

Recall runs RRF twice in the query path:

1. **Inner RRF** (in ZVec.NET, in-DB) — fuses dense + FTS within a single tier.
2. **Outer RRF** (in `Recall.Core`, lightweight) — fuses results across the 3 tiers.

The outer RRF uses the same `1/(k + rank)` formula with the same `k`. The reasoning: each tier's result list is itself a noisy observation of the "true" ranking; the cold tier is noisier than the hot tier, but RRF's `1/(k+rank)` weighting handles this gracefully — the cold tier's top-1 contributes more than its top-10, just like the hot tier's.

### 8.6 C# implementation

```csharp
// Recall.Core/Query/RrfFuser.cs
internal static class RrfFuser
{
    public const int DefaultK = 60;

    public static IReadOnlyList<RankedItem> FuseAcrossTiers(
        IReadOnlyList<IReadOnlyList<RankedItem>> perTierLists, int topK, int k = DefaultK)
    {
        var scores = new Dictionary<string, float>();
        var byId = new Dictionary<string, RankedItem>();

        for (var i = 0; i < perTierLists.Count; i++)
        {
            var list = perTierLists[i];
            for (var rank = 0; rank < list.Count; rank++)
            {
                var item = list[rank];
                var contribution = 1f / (k + rank + 1);   // rank is 0-indexed; +1 to make 1-indexed
                if (scores.TryGetValue(item.Id, out var existing))
                {
                    scores[item.Id] = existing + contribution;
                }
                else
                {
                    scores[item.Id] = contribution;
                    byId[item.Id] = item;
                }
            }
        }

        return scores
            .OrderByDescending(kv => kv.Value)
            .Take(topK)
            .Select(kv => byId[kv.Key] with { Score = kv.Value })
            .ToList();
    }
}

// Recall.Core/Query/RankedItem.cs
public sealed record RankedItem(
    string Id,
    string Modality,
    DateTime CapturedAt,
    string SourcePath,
    int DimTier,
    float Score,                  // RRF score (no units)
    float DenseScore,             // raw dense similarity, for UI display
    float FtsScore);              // raw FTS BM25 score, for UI display
```

### 8.7 When to switch to Weighted RRF

ZVec.NET also ships a **Weighted** reranker where each list has a configurable weight. Use this when:

- One retriever is much stronger than another (e.g., dense ANN recall = 0.95, FTS recall = 0.60) — give dense a 2× weight.
- One retriever is domain-specific (e.g., a future sparse-vec retriever tuned for code) — boost it for code queries.

v1 uses plain RRF (equal weights) because we don't have enough data to tune the weights; v1.1 may add Weighted with weights tuned on the golden-query set.

---


## 9. Task Prefix Engineering

### 9.1 What task prefixes do

EmbeddingGemma 2 (IdeaTank §1.1) supports **task-instruction prefixes** — short text prefixes prepended to the input that steer the model into a particular embedding behavior:

| Prefix | Behavior | Use case in Recall |
|---|---|---|
| `[search]` | Search embedding (query-side) | User-issued queries |
| `[cluster]` | Clustering-friendly embedding | Nightly auto-organization |
| `[classify]` | Classification-friendly embedding | Auto-tagging |
| `[semantic]` | Symmetric semantic similarity | "Find similar items" |

The crucial property is that **the same corpus can be queried in different modes without re-embedding**. The corpus is embedded once (with `[semantic]` as a neutral default); the query is embedded with the chosen task prefix at query time. The model was trained to make this work.

### 9.2 Recall's prefix selection

The UI exposes three buttons:

| Button | Task prefix | When to use |
|---|---|---|
| **Find** | `[search]` | Default search ("red scooter near Colosseum") |
| **Group** | `[cluster]` | "Show me clusters in last 30 days" |
| **Tag** | `[classify]` | "Auto-tag my photos by scene" |

`[semantic]` is the default for ingestion (no prefix on the source side; ingestion just embeds the content). The asymmetry (no prefix on ingest, prefix on query) is intentional — it lets the same corpus be queried in any mode.

### 9.3 Why this matters economically

Without task prefixes, every "mode change" would require re-embedding the corpus — at 100k items on mobile, that's ~10 minutes of CPU and ~5% battery per re-embed. With prefixes, the mode switch is **free** — just prepend the prefix to the query string.

This is a non-trivial savings: if the user toggles between Find / Group / Tag modes 5 times a day, without prefixes that's 50 minutes of CPU and 25% battery just for mode switches. With prefixes, it's 0 ms and 0% battery.

### 9.4 C# implementation

```csharp
// Recall.Core/Query/TaskPrefix.cs
public enum TaskPrefix
{
    Search,
    Cluster,
    Classify,
    Semantic
}

internal static class TaskPrefixFormatter
{
    public static string Format(TaskPrefix prefix, string input) => prefix switch
    {
        TaskPrefix.Search    => $"[search]{input}",
        TaskPrefix.Cluster   => $"[cluster]{input}",
        TaskPrefix.Classify  => $"[classify]{input}",
        TaskPrefix.Semantic  => input,   // no prefix; symmetric default
        _ => throw new ArgumentOutOfRangeException(nameof(prefix))
    };
}

// Recall.Core/Query/QueryPipeline.cs (excerpt)
public async ValueTask<IReadOnlyList<SearchResult>> RunAsync(
    string query, TaskPrefix prefix,
    SearchFilters filters, int topK, CancellationToken ct)
{
    var formatted = TaskPrefixFormatter.Format(prefix, query);
    var embed = await _textEncoder.EmbedAsync(
        new TextEncoderInput(formatted, prefix.ToString().ToLowerInvariant()),
        ct).ConfigureAwait(false);
    // ...
}
```

### 9.5 Cluster and classify are nightly batch jobs

`[cluster]` and `[classify]` modes don't fit the user-interactive query latency budget (clustering is O(N²) in similarity comparisons; classification needs an external label set). These run as nightly batch jobs:

- **Nightly clustering:** embed all items with `[cluster]` prefix, run k-means or HDBSCAN, store cluster IDs as a scalar field. UI shows "your 30-day clusters" view.
- **Nightly classification:** embed all items with `[classify]` prefix, k-NN against a small labeled seed set (200 hand-labeled items per category), assign labels. UI shows "auto-tags" view.

Both jobs run between 02:00–05:00 local time when the device is charging (via Android `WorkManager` `BatteryNotLow + DeviceIdle` constraints; iOS background task scheduler).

---

## 10. 8K Context Handling

### 10.1 Why 8K matters

EmbeddingGemma 2 has an 8,192-token context window (IdeaTank §1.1). For audio at the standard 16 kHz sampling rate with the model's audio preprocessor (log-mel filterbanks at 10 Hz frame rate), 8,192 tokens corresponds to roughly:

$$T_{audio} = \frac{8192 \text{ tokens}}{~10 \text{ Hz}} \approx 819 \text{ seconds} \approx 13.6 \text{ minutes}$$

That's a typical voice memo or a long meeting chunk. For text, 8K tokens ≈ 6,000 words (≈ 12 pages), which covers most articles, blog posts, and short PDFs.

For video, 8K tokens at 1 frame per second sampling rate covers ~8,000 frames = ~2.2 hours of video.

### 10.2 The "embed whole vs chunk" decision

Recall's policy:

| Modality | Item length | Embedding strategy |
|---|---|---|
| Text | ≤ 8K tokens | Embed whole; one vector |
| Text | > 8K tokens | Chunk at semantic boundaries (paragraph / section); one vector per chunk; group by `parent_id` |
| Image | n/a | Embed whole; one vector |
| Audio | ≤ 13 min | Embed whole; one vector |
| Audio | > 13 min | Chunk at silence boundaries (VAD-detected); one vector per chunk |
| Video frame sequence | ≤ 8K frames | Embed whole sequence; one vector |
| Video frame sequence | > 8K frames | Chunk at scene boundaries; one vector per chunk |

The principle: **prefer whole-embedding** because the cross-attention within the encoder captures cross-token relationships that chunk-and-average loses. Only chunk when the content exceeds the context window.

### 10.3 Chunking policy

When chunking is required, the chunker uses **semantic boundaries**:

- Text: paragraph / heading breaks; preserve sentence integrity.
- Audio: VAD (voice activity detection) silence boundaries; preserve utterance integrity.
- Video: scene-cut detection; preserve shot integrity.

Each chunk is embedded with the `[semantic]` task prefix; the chunks share a `parent_id` field that lets the UI display them as "part 1 of N" of the original item.

### 10.4 Why not chunk-and-average

The naive approach is to chunk into fixed-size pieces, embed each, and average the embeddings. This loses information because averaging dilutes the unique signal of each chunk into a single muddy vector. EmbeddingGemma 2's transformer cross-attention captures inter-chunk dependencies within the context window, which is why the 8K context is the differentiator — it lets Recall embed up to 13 minutes of audio or 12 pages of text as a single coherent vector.

The cost is per-token inference time (roughly linear in sequence length for transformers, modulo attention optimizations). At 8K tokens, an inference takes ~250 ms on mobile (Plan §7.1), which is acceptable for ingestion (background) and tolerable for queries (the query is typically short, not 8K).

### 10.5 C# implementation (chunker interface)

```csharp
// Recall.Core/Capture/IChunker.cs
public interface IChunker<T>
{
    IReadOnlyList<T> Chunk(T input);
}

// Recall.Core/Capture/TextChunker.cs
internal sealed class TextChunker : IChunker<string>
{
    private readonly int _maxTokens;

    public TextChunker(int maxTokens = 8192) => _maxTokens = maxTokens;

    public IReadOnlyList<string> Chunk(string input)
    {
        var tokens = Tokenizer.Count(input);
        if (tokens <= _maxTokens) return new[] { input };

        // Chunk at paragraph boundaries; preserve sentence integrity
        var paragraphs = input.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var current = new StringBuilder();
        var currentLen = 0;

        foreach (var para in paragraphs)
        {
            var paraTokens = Tokenizer.Count(para);
            if (currentLen + paraTokens > _maxTokens)
            {
                if (current.Length > 0) chunks.Add(current.ToString());
                current.Clear();
                current.Append(para);
                currentLen = paraTokens;
            }
            else
            {
                current.Append("\n\n").Append(para);
                currentLen += paraTokens;
            }
        }
        if (current.Length > 0) chunks.Add(current.ToString());
        return chunks;
    }
}

// Recall.Core/Capture/AudioChunker.cs (uses VAD)
internal sealed class AudioChunker : IChunker<float[]>
{
    private readonly TimeSpan _maxDuration;
    private readonly int _sampleRate;

    public AudioChunker(TimeSpan maxDuration, int sampleRate = 16000)
    {
        _maxDuration = maxDuration;
        _sampleRate = sampleRate;
    }

    public IReadOnlyList<float[]> Chunk(float[] input)
    {
        var maxSamples = (int)(_maxDuration.TotalSeconds * _sampleRate);
        if (input.Length <= maxSamples) return new[] { input };

        // VAD: find silence offsets at > 200ms silence
        var silenceOffsets = VoiceActivityDetection.FindSilences(input, _sampleRate,
            minSilenceMs: 200, threshold: 0.02f);
        var chunks = new List<float[]>();
        var start = 0;

        foreach (var silence in silenceOffsets)
        {
            if (silence - start > maxSamples)
            {
                chunks.Add(input.AsSpan(start, silence - start).ToArray());
                start = silence;
            }
        }
        if (input.Length - start > 0)
            chunks.Add(input.AsSpan(start, input.Length - start).ToArray());
        return chunks;
    }
}
```

---

## 11. Scope-Watcher Pipeline (v1.1 — renamed from Capture Pipeline)

> **v1.1 rename:** the original v1.0 draft called this the "Capture Pipeline" and assumed ambient OS-signal capture (clipboard, camera roll, screen OCR, microphone). v1.1 replaces this with a **scope-watcher model**: the user explicitly grants the app access to specific paths + per-path file types; the app uses filesystem watchers within scope. No OS-signal capture in v1. See Plan §3.3 for the v1.1 data flow.

### 11.1 The scope-watcher pipeline's job

The scope-watcher pipeline accepts `FileEvent`s from per-platform filesystem watchers within the user-configured scope, prepares the appropriate encoder for each event's modality, embeds it, applies the MRL tiering policy, and inserts it into the appropriate modality × tier shard (§5.2.1). It runs **asynchronously** — never on the UI thread.

### 11.2 Pipeline diagram

```
[Platform Capture Source]
    │
    │ emits CaptureItem { Modality, SourcePath, Bytes, CapturedAt, Lang }
    ▼
[Channel<CaptureItem>]                  # bounded; back-pressure safe
    │
    ▼
[IngestionWorker] (IHostedService)
    │
    ├──► IChunker<T>.Chunk(item)        # may split if > 8K context
    │
    ├──► IOnnxEncoderSession.EmbedAsync(chunk)
    │       │
    │       └──► EncoderMode = Text / TextVision / Full
    │
    ├──► EmbeddingNormalizer.NormalizeInPlace
    │
    ├──► IMrlTieringPolicy.ResolveTier
    │
    ├──► MrlTruncator.Truncate(emb, tier)
    │
    ├──► ContentAddressableId.Compute
    │
    ├──► IZVecCollection.InsertAsync
    │
    └──► IOpLog.AppendAsync
```

### 11.3 Capture sources (per-platform implementations)

```csharp
// Recall.Core/Capture/ICaptureSource.cs
public interface ICaptureSource
{
    IObservable<CaptureItem> CaptureStream { get; }
}

// Recall.Core/Capture/CaptureItem.cs
public sealed record CaptureItem(
    string Modality,           // "text" | "image" | "audio" | "video"
    string SourcePath,         // local file:// path
    ReadOnlyMemory<byte> Bytes,
    DateTime CapturedAt,
    string Language);

// Recall.Maui/Platforms/Android/ClipboardCaptureSource.cs
internal sealed class AndroidClipboardCaptureSource : ICaptureSource
{
    private readonly Subject<CaptureItem> _subject = new();

    public IObservable<CaptureItem> CaptureStream => _subject.AsObservable();

    public AndroidClipboardCaptureSource(AndroidClipboardWatcher watcher)
    {
        watcher.ClipboardChanged += OnClipboardChanged;
    }

    private async void OnClipboardChanged(object? sender, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _subject.OnNext(new CaptureItem(
            Modality: "text",
            SourcePath: $"clipboard://{DateTime.UtcNow:O}",
            Bytes: Encoding.UTF8.GetBytes(text),
            CapturedAt: DateTime.UtcNow,
            Language: await LanguageDetector.DetectAsync(text)));
    }
}

// Recall.Maui/Platforms/Android/CameraRollCaptureSource.cs
internal sealed class AndroidCameraRollCaptureSource : ICaptureSource, IAsyncDisposable
{
    private readonly Subject<CaptureItem> _subject = new();
    private readonly ContentObserver _observer;
    private readonly Android.Net.Uri _uri = MediaStore.Images.Media.ExternalContentUri;

    public IObservable<CaptureItem> CaptureStream => _subject.AsObservable();

    public AndroidCameraRollCaptureSource(Context context)
    {
        _observer = new CameraRollObserver(context, _subject);
        context.ContentResolver.RegisterContentObserver(_uri, true, _observer);
    }

    public ValueTask DisposeAsync()
    {
        // unregister observer
        return ValueTask.CompletedTask;
    }

    private sealed class CameraRollObserver : ContentObserver
    {
        // ... handles onChange, fetches the new image, emits CaptureItem
    }
}
```

### 11.4 Ingestion worker (the pipeline orchestrator)

```csharp
// Recall.Core/Capture/IngestionWorker.cs
internal sealed class IngestionWorker : BackgroundService
{
    private readonly Channel<CaptureItem> _channel;
    private readonly IChunker<string> _textChunker;
    private readonly IChunker<float[]> _audioChunker;
    private readonly IOnnxEncoderSessionFactory _encoderFactory;
    private readonly IMrlTieringPolicy _mrl;
    private readonly IZVecCollection<RecallItem> _collection;
    private readonly IOpLog _opLog;
    private readonly ILogger<IngestionWorker> _log;

    public IngestionWorker(
        Channel<CaptureItem> channel,
        IChunker<string> textChunker,
        IChunker<float[]> audioChunker,
        IOnnxEncoderSessionFactory encoderFactory,
        IMrlTieringPolicy mrl,
        IZVecCollection<RecallItem> collection,
        IOpLog opLog,
        ILogger<IngestionWorker> log)
    {
        _channel = channel;
        _textChunker = textChunker;
        _audioChunker = audioChunker;
        _encoderFactory = encoderFactory;
        _mrl = mrl;
        _collection = collection;
        _opLog = opLog;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            try
            {
                await ProcessAsync(item, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _log.LogError(ex, "Failed to ingest item {SourcePath}", item.SourcePath);
                // Don't rethrow; ingestion failures are non-fatal.
                // The item stays in the source (clipboard, camera roll, etc.)
                // and will be re-emitted on next change.
            }
        }
    }

    private async Task ProcessAsync(CaptureItem item, CancellationToken ct)
    {
        var encoder = SelectEncoder(item.Modality);
        var chunks = SplitIntoChunks(item);
        foreach (var chunk in chunks)
        {
            var input = ToEncoderInput(chunk, item);
            var raw = await encoder.EmbedAsync(input, ct).ConfigureAwait(false);
            EmbeddingNormalizer.NormalizeInPlace(raw.Span);

            var tier = _mrl.ResolveTier(item.CapturedAt, pinned: false,
                lastAccessedAt: item.CapturedAt);
            var truncated = MrlTruncator.Truncate(raw, tier);

            var id = ContentAddressableId.Compute(item.Modality, item.SourcePath, tier);
            var textPayload = item.Modality == "text"
                ? Encoding.UTF8.GetString(item.Bytes.ToArray())
                : null;

            var record = new RecallItem
            {
                Id = id,
                Embedding = truncated,
                Modality = item.Modality,
                CapturedAt = item.CapturedAt,
                Language = item.Language,
                SourcePath = item.SourcePath,
                DimTier = tier,
                Pinned = false,
                LastAccessedAt = item.CapturedAt,
                TextPayload = textPayload
            };

            await _collection.InsertAsync(record, ct).ConfigureAwait(false);
            await _opLog.AppendAsync(OpKind.Insert, id, record, ct).ConfigureAwait(false);
        }
    }

    private IOnnxEncoderSession SelectEncoder(string modality) => modality switch
    {
        "text"  => _encoderFactory.GetOrCreate(EncoderMode.Text),
        "image" => _encoderFactory.GetOrCreate(EncoderMode.TextVision),
        "video" => _encoderFactory.GetOrCreate(EncoderMode.TextVision),
        "audio" => _encoderFactory.GetOrCreate(EncoderMode.Full),
        _ => throw new ArgumentOutOfRangeException(nameof(modality))
    };
    // ... other helpers
}
```

### 11.5 Backpressure & bounding

The ingestion channel is **bounded** (`Channel.CreateBounded(1000)`). If ingestion falls behind (e.g., a long video frame batch), the capture sources block. This is intentional: it prevents unbounded memory growth under high capture load.

For high-rate sources (e.g., a 30 fps video frame sampler), the source itself throttles — the video frame sampler drops to 0.5 fps when the channel is half-full, preventing the worker from being overwhelmed.

---

## 12. Query Pipeline

### 12.1 The query pipeline's job

The query pipeline accepts a user query (text typed, image dropped, audio recorded), embeds it, runs hybrid search across active tiers with the in-DB RRF reranker, fuses tier-level results with outer RRF, applies the MRL promote-on-access policy to the top-K, and returns results with provenance.

### 12.2 Pipeline diagram

```
[UI: SearchViewModel]
    │
    │ user types "red scooter" + clicks "Find"
    ▼
[QueryPipeline.RunAsync]
    │
    ├──► TaskPrefixFormatter.Format(TaskPrefix.Search, "red scooter")
    │       → "[search]red scooter"
    │
    ├──► IOnnxEncoderSession.EmbedAsync("[search]red scooter", mode=Text)
    │       → 768-d float vector
    │
    ├──► EmbeddingNormalizer.NormalizeInPlace
    │
    ├──► IMrlTieringPolicy.GetActiveTiersAsync
    │       → [128, 256, 768]
    │
    ├──► for tier in activeTiers:
    │       │
    │       ├──► MrlTruncator.Truncate(query, tier)
    │       │
    │       └──► IZVecCollection.HybridSearchAsync
    │              (dense: truncated, fts: "red scooter",
    │               scalar: modality filter,
    │               topK: 40)
    │                  │
    │                  └──► [in-DB RRF] fuses dense + FTS ranks
    │
    ├──► RrfFuser.FuseAcrossTiers(perTierResults, topK=20)
    │
    ├──► for result in top20:
    │       IMrlTieringPolicy.PromoteOnAccessAsync(result.Id)
    │       (lazy; fire-and-forget)
    │
    └──► return top20 with provenance
```

### 12.3 C# implementation (orchestrator)

```csharp
// Recall.Core/Query/QueryPipeline.cs
internal sealed class QueryPipeline
{
    private readonly IOnnxEncoderSession _textEncoder;
    private readonly IOnnxEncoderSession _imageEncoder;
    private readonly IOnnxEncoderSession _fullEncoder;
    private readonly IMultiTierCollectionManager _collections;
    private readonly IMrlTieringPolicy _mrl;
    private readonly IOpLog _opLog;
    private readonly ILogger<QueryPipeline> _log;

    public async ValueTask<IReadOnlyList<SearchResult>> RunAsync(
        QueryInput input, TaskPrefix prefix,
        SearchFilters filters, int topK, CancellationToken ct)
    {
        var encoder = input switch
        {
            TextQueryInput    => _textEncoder,
            ImageQueryInput   => _imageEncoder,
            AudioQueryInput   => _fullEncoder,
            _ => throw new ArgumentException($"Unknown input type {input.GetType()}")
        };

        var raw = await encoder.EmbedAsync(
            ToEncoderInput(input, prefix), ct).ConfigureAwait(false);
        EmbeddingNormalizer.NormalizeInPlace(raw.Span);

        var activeTiers = await _mrl.GetActiveTiersAsync(ct).ConfigureAwait(false);

        var perTierTasks = activeTiers.Select(async tier =>
        {
            var truncated = MrlTruncator.Truncate(raw, tier);
            var results = await _collections.SearchAsync(
                tier, truncated, input.TextForFts, filters, topK * 2, ct)
                .ConfigureAwait(false);
            return (tier, results);
        });
        var perTierResults = await Task.WhenAll(perTierTasks).ConfigureAwait(false);

        var fused = RrfFuser.FuseAcrossTiers(
            perTierResults.Select(p => p.results).ToList(), topK);

        // Fire-and-forget promote-on-access
        _ = Task.Run(async () =>
        {
            foreach (var result in fused)
            {
                try
                {
                    await _mrl.PromoteOnAccessAsync(result.Id, ct)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Promote-on-access failed for {Id}", result.Id);
                }
            }
        }, ct);

        return fused.Select(r => new SearchResult(
            Id: r.Id,
            Modality: r.Modality,
            CapturedAt: r.CapturedAt,
            SourcePath: r.SourcePath,
            DimTier: r.DimTier,
            Score: r.Score,
            DenseScore: r.DenseScore,
            FtsScore: r.FtsScore,
            Snippet: ExtractSnippet(r))).ToList();
    }
    // ... helpers
}
```

### 12.4 Why the promote-on-access is fire-and-forget

Promote-on-access is a **latency-bucket** optimization — it improves future queries, not the current one. Blocking the current query on the promote would add ~50 ms (re-embed + insert + op-log) for every result in the top-20, blowing the latency budget. The `Task.Run` fire-and-forget pattern lets the current query return immediately; the promote happens in the background.

If the promote fails (e.g., source media was deleted), the failure is logged and the item stays at its current tier — non-fatal.

---

## 13. Sync Design — Technical

> **Reminder:** Sync is **deferred** in v1. This section is the technical design for v1.5. The v1 schema is engineered to be sync-ready; the v1.5 implementation drops in without refactor.

### 13.1 Content-addressable IDs (recap, with crypto detail)

```
vector_id = SHA-256(
    length_prefix(4 bytes) || modality_string ||
    length_prefix(4 bytes) || source_content_sha256 ||   # SHA-256 of original media bytes
    length_prefix(4 bytes) || dim_tier_string            # "768" | "512" | "256" | "128"
)
```

Length-prefixing the components prevents length-extension attacks on SHA-256 (a known property of Merkle-Damgård hashes). Two devices that capture the same article text and embed it at the 256-d tier produce the same `vector_id` byte-for-byte. Dedup is trivial: `INSERT` of an existing `vector_id` is a no-op.

```csharp
// Recall.Core/Storage/ContentAddressableId.cs
public static class ContentAddressableId
{
    public static string Compute(string modality, string sourcePath, int dimTier)
    {
        var sourceBytes = File.ReadAllBytes(sourcePath);  // or stream
        var sourceHash = SHA256.HashData(sourceBytes);

        var dimTierBytes = Encoding.UTF8.GetBytes(dimTier.ToString());
        var modalityBytes = Encoding.UTF8.GetBytes(modality);

        using var stream = new MemoryStream();
        WriteLengthPrefixed(stream, modalityBytes);
        WriteLengthPrefixed(stream, sourceHash);
        WriteLengthPrefixed(stream, dimTierBytes);

        var id = SHA256.HashData(stream.ToArray());
        return Convert.ToHexString(id).ToLowerInvariant();
    }

    private static void WriteLengthPrefixed(Stream s, byte[] data)
    {
        s.Write(BitConverter.GetBytes(data.Length));
        s.Write(data);
    }
}
```

### 13.2 Op-log format

```
file: oplog/<device_id>.log
record format (binary, length-prefixed):

  | version (1 byte = 0x01) |
  | record_length (4 bytes, big-endian) |
  | timestamp (8 bytes, little-endian int64) |
  | op_kind (1 byte: 0x01=INSERT, 0x02=DELETE, 0x03=PIN, 0x04=UNPIN, 0x05=PROMOTE) |
  | vector_id (32 bytes SHA-256) |
  | metadata_length (4 bytes) |
  | metadata_ciphertext (variable, AES-256-GCM) |
  | metadata_nonce (12 bytes) |
  | metadata_tag (16 bytes) |
  | vector_ciphertext_length (4 bytes, 0 if no vector) |
  | vector_ciphertext (variable, AES-256-GCM) |
  | vector_nonce (12 bytes) |
  | vector_tag (16 bytes) |
  | checksum (4 bytes CRC32) |   # detects torn writes
```

The CRC32 is a tail check; a torn write at the tail produces a bad CRC and the record is discarded on next open. The file is otherwise append-only and never modified.

### 13.3 Key hierarchy & rotation

```
On first pairing (OPAQUE):
    user enters a 6-digit code shown on device A into device B
    → OPAQUE establishes root_secret on both devices
    → both devices derive pair_key = HKDF(root_secret, "recall-pair-key-v1")

Every 30 days (epoch rotation):
    new_epoch = old_epoch + 1
    session_key = HKDF(pair_key, "session-" || new_epoch, salt=nonce)
    op_log_key   = HKDF(session_key, "op-log-encryption")
    vector_key   = HKDF(session_key, "vector-encryption")
    metadata_key = HKDF(session_key, "metadata-encryption")
```

Each device persists `(root_secret_id, current_epoch, last_rotation_at)`. The session keys are derived on-demand; they are not persisted. Compromise of one device's session key exposes only the current epoch's op-log entries (30 days max).

### 13.4 Encryption (AES-256-GCM)

Each record is encrypted under its own key (`op_log_key`, `vector_key`, `metadata_key`), with a fresh 12-byte random nonce per record. The 96-bit nonce is safe under GCM as long as the same key is used fewer than 2^32 times per device — at 1k inserts/day, that's 4 million days of headroom.

```csharp
// Recall.Core/Sync/OpLogCipher.cs (v1.5 implementation; v1 has interface only)
internal static class OpLogCipher
{
    public static (byte[] ciphertext, byte[] nonce, byte[] tag) Encrypt(
        byte[] plaintext, byte[] key)
    {
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var gcm = new AesGcm(key, tagSizeInBytes: 16);
        gcm.Encrypt(nonce, plaintext, ciphertext, tag);
        return (ciphertext, nonce, tag);
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] nonce, byte[] tag)
    {
        var plaintext = new byte[ciphertext.Length];
        using var gcm = new AesGcm(key, tagSizeInBytes: 16);
        gcm.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
```

### 13.5 Transport abstraction

```csharp
// Recall.Core/Sync/ISyncTransport.cs (interface only in v1)
public interface ISyncTransport : IAsyncDisposable
{
    ValueTask<PeerDiscoveryResult> DiscoverPeersAsync(CancellationToken ct);
    ValueTask<IAsyncEnumerable<SyncMessage>> OpenChannelAsync(
        string peerId, CancellationToken ct);
    ValueTask SendMessageAsync(string peerId, SyncMessage message, CancellationToken ct);
}

// v1.5 implementations (designed, not built in v1):
// - LanMdnsTransport (Bonjour/Avahi; gRPC over TCP)
// - WifiDirectTransport (native platform API)
// - BleTransport (GATT characteristics)
// - RelayedOverlayTransport (opt-in; self-hosted Tor-like circuit)
```

The v1 `Recall.Core.Sync` namespace contains **only the interfaces and DTOs** — no implementations. The `BannedApiAnalyzers` rule allows network code in this namespace, but in v1 there is no code there to call.

### 13.6 CRDT semantics

| Op | CRDT type | Merge rule |
|---|---|---|
| INSERT | Set (with content-addressable IDs) | Union; ties broken by max(tier) |
| DELETE | Tombstone set | Add-only tombstone; INSERTs of tombstoned IDs are suppressed |
| PIN | LWW register with vector clock | Last writer wins; ties broken by `device_id` |
| UNPIN | LWW register with vector clock | Same as PIN |
| PROMOTE | LWW register | Same as PIN; promote value is the new tier |

Vector clocks are `Dictionary<device_id, counter>`; each op increments the issuing device's counter. Two ops on the same `vector_id` are ordered by `compare(vector_clock_a, vector_clock_b)`:

- If `a < b` (a's clock is dominated by b's), apply b.
- If `a > b`, apply a (already applied).
- If `a || b` (concurrent), break tie by `device_id` (deterministic, total order).

### 13.7 Why this sync design fits the local-first thesis

1. **No server ever sees plaintext.** The relay (if used) sees AES-256-GCM ciphertext only. Metadata, source paths, vector floats — all encrypted under keys derived from a PAKE-established shared secret.
2. **No PII ever leaves the device unencrypted.** The op-log is the only thing that syncs, and it's ciphertext from end to end.
3. **No central coordinator.** OPAQUE pairing is peer-to-peer; no third party learns the root secret. The relay (if used) is a dumb ciphertext forwarder.
4. **Conflict-free.** Content-addressable IDs + CRDT semantics = no coordination needed. Two devices that capture the same content produce the same ID and dedupe naturally.
5. **Tier-aware.** Higher tier wins; lower tier upgrades lazily. Sync doesn't degrade density.
6. **Opt-in.** A user who never pairs a second device has zero network code paths active. The `ISyncTransport` is never instantiated.

This is the only sync design that fits the local-first thesis. Any design that routes plaintext through a server breaks the no-egress anti-goal. Any design that requires a server-mediated pairing breaks the no-server goal. The PAKE + E2EE + relayed-overlay pattern is the only one that fits.

---

## 14. Memory & Storage Budget Per Platform

### 14.1 Memory model

The app's RSS decomposes as:

```
RSS = CLR + MAUI + ONNX_assets + ZVec_DB + ONNX_inference_working + GC_heap + UI_WebView
```

Where:
- **CLR**: ~80 MB (fixed, .NET runtime)
- **MAUI**: ~30 MB (shell + Blazor WebView host)
- **ONNX_assets**: depends on loaded encoders (text=270M fp16 = ~540 MB; full=740M fp16 = ~1.5 GB)
- **ZVec_DB**: HNSW graph + raw vectors at the active tier ratio
- **ONNX_inference_working**: ~50–100 MB (input/output tensors, EP workspace)
- **GC_heap**: ~50 MB (GC-managed .NET heap)
- **UI_WebView**: ~80 MB (Blazor WebView rendering buffer)

### 14.2 Per-platform budget (after warmup, 100k items, tiered)

| Component | Pixel 6a | iPhone 13 | x86 laptop | Apple M2 |
|---|---|---|---|---|
| CLR | 80 MB | 80 MB | 80 MB | 80 MB |
| MAUI | 30 MB | 30 MB | 30 MB | 30 MB |
| ONNX_assets (text encoder resident) | 540 MB | 540 MB | 540 MB | 540 MB |
| ONNX_assets (vision encoder, on-demand, avg) | 0 (loaded only when used) | 0 | 0 | 0 |
| ONNX_assets (full encoder, on-demand, avg) | 0 | 0 | 0 | 0 |
| ZVec_DB (3 collections, 100k items, tiered) | ~150 MB | ~150 MB | ~60 MB (RaBitQ) | ~150 MB |
| ONNX inference working | 100 MB | 80 MB | 80 MB | 60 MB |
| GC heap | 50 MB | 50 MB | 50 MB | 50 MB |
| UI WebView | 80 MB | 80 MB | 80 MB | 80 MB |
| **Total RSS** | **~1.03 GB** | **~1.01 GB** | **~0.92 GB** | **~0.99 GB** |

Mobile targets (Pixel 6a, iPhone 13) have 4 GB addressable. Recall's ~1 GB RSS leaves ~3 GB for the OS and other apps — comfortable but not generous. The Plan §9 R-2 risk (740M mobile RSS) is exactly this: if the 740M full encoder is loaded resident (instead of on-demand), RSS jumps to ~2.5 GB and the OS may kill the process.

### 14.3 Storage model (disk)

```
~/recall/
├── recall.zvec                # ZVec DB file (100k items, tiered)
│                                #   Mobile: ~150 MB
│                                #   Desktop (RaBitQ): ~60 MB
├── oplog/                      # Append-only op-logs (one per paired device)
│   └── <device_id>.log         #   Empty in v1 (no sync); grows in v1.5
├── onnx/
│   ├── embeddinggemma-2-text.onnx          # ~540 MB (always present)
│   ├── embeddinggemma-2-text-vision.onnx  # ~880 MB (always present)
│   └── embeddinggemma-2-full.onnx          # ~1.5 GB (optional; lazy-download)
├── cache/                      # Thumbnail cache for image results
│   └── ...                     #   Bounded to 100 MB; LRU evicted
└── logs/                       # Diagnostic logs (rolling)
    └── ...                     #   Bounded to 10 MB
```

Total disk: ~1.5 GB (model assets) + ~150 MB (DB) + ~100 MB (cache) + ~10 MB (logs) = **~1.76 GB** on mobile.

The 1.5 GB model asset cost is the biggest disk footprint; if the user doesn't use audio queries, the `embeddinggemma-2-full.onnx` file is **lazy-downloaded** on first audio query, saving 1.5 GB for users who never need it.

### 14.4 Battery model (mobile)

| Operation | Energy cost (Pixel 6a) | Notes |
|---|---|---|
| 1 minute background text ingestion (270M encoder) | ~0.13% / min | 60s × ~8% / 30 min |
| 1 minute foreground text query (270M) | ~0.16% / min | ~6% / 30 min (Plan §7.1) |
| 1 minute foreground audio query (740M) | ~0.40% / min | Higher; 740M encoder loaded |
| 1 minute foreground image query (440M) | ~0.20% / min | |
| Idle (no ingestion, no query) | ~0.01% / min | Just the Blazor WebView resident |

A typical user pattern: 60 minutes idle + 5 minutes text queries + 1 minute audio query = 0.6% + 0.8% + 0.4% = ~1.8% per hour. For an 8-hour workday, ~14% of battery — comparable to a music streaming app.

---

## 15. Failure Modes & Recovery

### 15.1 Process crash mid-insert (kill -9)

**Scenario:** User is recording a 30-minute voice memo; the OS kills the process mid-insert (low memory, app backgrounded).

**Recovery:**
1. The op-log is append-only with a CRC32 tail. The last record may be torn; the next open discards it.
2. The ZVec DB has an internal WAL; partial inserts are rolled back on next open.
3. The audio file on disk is intact (it was written to a temp file before embedding started); the next ingestion pass detects it as "not yet indexed" (no `vector_id` for this `source_path`) and re-embeds.

**Guarantee:** Zero data loss for completed operations; at most one in-flight operation is lost.

### 15.2 ZVec DB file corruption

**Scenario:** Bad sector on the SSD corrupts the ZVec DB file.

**Recovery:**
1. ZVec.NET's `SafeZvecHandle` opens the file in read-write mode; if the header is corrupt, it throws `ZVecCorruptException`.
2. The recall app's `IHostedService` catches this on startup and triggers a **rebuild from op-log**: replay all op-log entries into a fresh DB.
3. The original media is preserved; embedding is recomputed from the source files (using the appropriate encoder + tier).

**Guarantee:** Data is recoverable as long as (a) the op-log is intact and (b) the source media is preserved.

### 15.3 Source media deleted (orphaned vectors)

**Scenario:** User deletes a photo from the camera roll; Recall still has the vector.

**Recovery:**
1. The next time the vector is retrieved, the UI shows "media not on this device" (the `SourcePath` no longer resolves).
2. The user can: (a) re-add the media (the content-addressable ID will match if the bytes are identical), or (b) delete the orphaned vector from Recall's UI (issues a `DELETE` op-log entry).
3. MRL promote-on-access fails gracefully for orphaned vectors (logged; the item stays at its current tier).

**Guarantee:** No silent corruption; the user is always informed of orphaned vectors when they appear in search results.

### 15.4 Disk full mid-ingestion

**Scenario:** Disk fills up during a long video frame ingestion batch.

**Recovery:**
1. The ZVec insert throws `IOException: No space left on device`.
2. The ingestion worker catches it, logs the error, and **pauses ingestion** for 5 minutes.
3. A UI banner appears: "Disk almost full; ingestion paused."
4. The user can clear cache (the `cache/` directory; LRU-evicted thumbnails) or delete old items from the corpus.
5. After cache clear, ingestion resumes automatically.

**Guarantee:** No crash; no data loss for completed items; transparent UX for the failure.

### 15.5 ONNX model file missing or corrupt

**Scenario:** User's disk has a bad sector that corrupts `embeddinggemma-2-text.onnx`.

**Recovery:**
1. ONNX Runtime throws `OnnxRuntimeException: Model file is corrupt` on first inference.
2. The Recall app catches this on startup (smoke test) and triggers a **re-download** from the Hugging Face CDN (`onnx-community/embeddinggemma-2-ONNX`).
3. The re-download is a 540 MB fetch; the UI shows a progress bar; ingestion is paused during the download.

**Guarantee:** Recovery is automatic; the user is informed.

### 15.6 Schema migration (v1 → v1.5)

**Scenario:** v1.5 adds a `cluster_id` scalar field; the existing v1 DB doesn't have it.

**Recovery:**
1. ZVec.NET supports in-DB schema evolution (IdeaTank §1.2).
2. On first open of a v1 DB by a v1.5 binary, the schema migrates: the new field is added with `null` default; existing items get `cluster_id = null`.
3. The nightly clustering job backfills `cluster_id` for all existing items.

**Guarantee:** Zero-downtime upgrade; no data loss; backfill is lazy.

### 15.7 Sync key compromise (v1.5)

**Scenario:** Attacker learns the `pair_key` (e.g., shoulder-surfed the 6-digit pairing code, then derived the key offline).

**Recovery:**
1. User triggers "rotate pair key" in the UI: requires both devices online.
2. The new pair key is established via a fresh OPAQUE exchange.
3. The old pair key is revoked; future op-logs use the new key.
4. Existing op-log entries encrypted under the old key are decrypted and re-encrypted under the new key (in the background, over ~1 hour for a 100k-item corpus).

**Guarantee:** Future syncs are secure; the historical exposure window is bounded by the time between compromise and rotation.

### 15.8 Filesystem watcher missed event (NEW in v1.1)

**Scenario:** Windows `FileSystemWatcher` misses a `Created` event under high system load (known limitation of the underlying `ReadDirectoryChangesW` Win32 API when the internal buffer fills). The user copies 1000 photos into `D:\Photos\` while Recall is running; only 980 are indexed.

**Recovery:**
1. The periodic **reconciliation sweep** (every 4 hours on Windows + Android; not needed on macOS) walks each scoped path, enumerates files, and diffs against the SQLite `checkpoints.db` `ingestion_cursor` table.
2. Files present on disk but absent from `checkpoints.db` are enqueued for ingestion.
3. Files in `checkpoints.db` but absent from disk are tombstoned (DELETE op-log entry).
4. If the reconciliation sweep finds > 1% of scoped files unindexed, it triggers a UX notification: "Recall found N unindexed files in your scope; re-indexing in the background."

**Guarantee:** Eventual consistency within 4 hours. Missed FS events do not cause permanent data loss.

### 15.9 Scope overlap conflict (NEW in v1.1)

**Scenario:** User adds `D:\` to scope, then later adds `D:\Photos\` to scope with different file-type filters. The scope manager must deduplicate without losing the more-specific filter.

**Recovery:**
1. The `IScopeManagerService` normalizes the scope on every change: detects subset/superset relationships, and keeps the more-specific entry (the one with tighter file-type filters) as authoritative.
2. The user sees a UX warning: "D:\Photos\ is a sub-path of D:\; the file-type filter on D:\Photos\ will take precedence for files in that sub-path."
3. The FS watcher registers only ONE watch per directory (not two); the more-specific filter is applied at the file-event routing layer.

**Guarantee:** No double-indexing; no wasted CPU; user is informed.

### 15.10 iOS "scan on app foreground" gap (NEW in v1.1)

**Scenario:** User opens Recall on iOS after a week of not using it. During that week, they added 500 new photos to the scoped `~/Photos/` folder (e.g., synced from iCloud). The iOS sandbox means Recall had no way to watch those changes in the background.

**Recovery:**
1. On app foreground, `IFileScopeWatcherService.iOS` walks each scoped path, enumerates files, and diffs against `checkpoints.db`.
2. The diff is enqueued for ingestion. On a typical scope (1k files), the diff takes ~1–2 seconds; on a 100k-file scope, ~30 seconds.
3. The UI shows a "scanning for new files..." progress indicator; once the diff completes, ingestion proceeds in the background.
4. The user can query immediately against the previously-indexed corpus while new files ingest in the background.

**Guarantee:** iOS users see new files appear in search results within ~30 seconds of opening Recall (worst case, large scope). This is a known platform limitation, not a bug; the macOS variant uses real `FSEvents` and has no such gap.

---

## 16. Appendix A — MRL Truncation Error Bound

### 16.1 Setup

Let `x, y ∈ R^d` be two embeddings (d=768 for EmbeddingGemma 2), with `‖x‖ = ‖y‖ = 1` (L2-normalized). The full cosine similarity is:

$$\text{sim}(x, y) = x^T y$$

Let `P_k` be the orthogonal projection onto the first `k` coordinates (the MRL truncation). The truncated embeddings are:

$$x_k = \frac{P_k x}{\|P_k x\|}, \quad y_k = \frac{P_k y}{\|P_k y\|}$$

and the truncated similarity is:

$$\text{sim}_k(x, y) = x_k^T y_k = \frac{(P_k x)^T (P_k y)}{\|P_k x\| \|P_k y\|} = \frac{x^T P_k y}{\|P_k x\| \|P_k y\|}$$

where the last equality uses the fact that `P_k` is symmetric and idempotent (`P_k = P_k^T`, `P_k^2 = P_k`).

### 16.2 Decomposition

Write `x = x_k^\parallel + x_k^\perp` where `x_k^\parallel = P_k x` (the truncated part) and `x_k^\perp = (I - P_k) x` (the orthogonal complement). Then `‖x‖^2 = ‖x_k^\parallel‖^2 + ‖x_k^\perp‖^2 = 1`, so:

$$\|x_k^\parallel\|^2 = 1 - \|x_k^\perp\|^2$$

The full similarity decomposes as:

$$x^T y = (x_k^\parallel + x_k^\perp)^T (y_k^\parallel + y_k^\perp) = (x_k^\parallel)^T (y_k^\parallel) + (x_k^\perp)^T (y_k^\perp)$$

The cross terms vanish because `x_k^\parallel` and `x_k^\perp` are orthogonal.

Let `a = (x_k^\parallel)^T (y_k^\parallel)` (the truncated-similarity numerator) and `b = (x_k^\perp)^T (y_k^\perp)`. Then:

$$x^T y = a + b$$

### 16.3 The bound

Let `r_x = ‖x_k^\perp‖`, `r_y = ‖y_k^\perp‖`. Note `r_x, r_y ∈ [0, 1]` and `‖x_k^\parallel‖ = √(1 - r_x²)`, `‖y_k^\parallel‖ = √(1 - r_y²)`.

The truncated similarity is:

$$\text{sim}_k(x, y) = \frac{a}{\sqrt{1 - r_x^2} \sqrt{1 - r_y^2}}$$

And the full similarity is `x^T y = a + b`, where `|b| ≤ r_x r_y` (Cauchy-Schwarz on the orthogonal complement).

The error:

$$|\text{sim}(x, y) - \text{sim}_k(x, y)| = \left| a + b - \frac{a}{\sqrt{1 - r_x^2} \sqrt{1 - r_y^2}} \right|$$

$$= \left| b - a \left( \frac{1}{\sqrt{1 - r_x^2} \sqrt{1 - r_y^2}} - 1 \right) \right|$$

$$\leq r_x r_y + |a| \left( \frac{1}{\sqrt{1 - r_x^2} \sqrt{1 - r_y^2}} - 1 \right)$$

A useful simplified bound, assuming `r_x, r_y` are small (which is the case for EmbeddingGemma 2 because MRL training specifically shapes the spectrum so the truncated part contains most of the energy):

For small `r_x, r_y`, the second term is approximately `a · (r_x² + r_y²)/2 ≤ (r_x² + r_y²)/2` (since `|a| ≤ 1`).

Thus:

$$|\text{sim}(x, y) - \text{sim}_k(x, y)| \lesssim r_x r_y + \frac{r_x^2 + r_y^2}{2} = \frac{(r_x + r_y)^2}{2}$$

That is:

$$\boxed{|\text{sim}(x, y) - \text{sim}_k(x, y)| \lesssim \frac{(r_x + r_y)^2}{2}}$$

where `r_x = ‖(I - P_k) x‖` is the residual norm (the energy in the truncated-out dimensions).

### 16.4 Empirical interpretation

For EmbeddingGemma 2 with MRL training:

| k | r_x (typical, text) | r_x (typical, multimodal) | Bound |
|---|---|---|---|
| 768 | 0 | 0 | 0 |
| 512 | 0.10 | 0.15 | 0.02 (text), 0.045 (mm) |
| 256 | 0.20 | 0.30 | 0.08 (text), 0.18 (mm) |
| 128 | 0.30 | 0.45 | 0.18 (text), 0.405 (mm) |

This is the formal statement of why Plan §9 R-3 is a real risk: at 128-d, multimodal truncation can shift similarity by up to ~0.4, which can reorder top-K results. The mitigation (Plan §9 R-3) is to raise the cold-tier floor to 256-d for multimodal if 128-d proves too lossy in W7 measurement.

### 16.5 Why the bound is loose for EmbeddingGemma 2

The bound assumes the worst-case alignment between `x_k^\perp` and `y_k^\perp`. In practice, MRL training specifically shapes the residual subspace so that high-similarity pairs have aligned residuals (the training signal pushes residual energy into "noise" directions). Empirically, the bound is ~3× looser than the measured error, so a measured `r_x ≈ 0.30` at 128-d typically gives a real similarity shift of ~0.06, not 0.18.

---

## 17. Appendix B — RRF Probability Interpretation

### 17.1 The model

Suppose there is a true ranking `π*` of items for a given query. Each retrieval list `i` produces a noisy observation `π_i` of `π*`, with probability:

$$P(\pi_i \text{ has } d \text{ at rank } r_i) = \frac{1}{k + r_i} \cdot \frac{1}{Z_i}$$

where `Z_i = \sum_d 1/(k + r_i(d))` is a normalization constant. The constant `k` is a free parameter modeling how "diffuse" the noise is.

If the lists are conditionally independent given `π*`, the posterior over `π*` is:

$$P(\pi^* | \{\pi_i\}) \propto P(\pi^*) \prod_i P(\pi_i | \pi^*)$$

### 17.2 The RRF score as a log-posterior

For the marginal probability that item `d` is the true top-1, by combining the noisy observations:

$$P(d \text{ is top-1} | \{\pi_i\}) \propto \prod_i P(\pi_i \text{ has } d \text{ at rank } r_i(d))$$

$$\log P(d | \{\pi_i\}) = \text{const} + \sum_i \log \frac{1}{k + r_i(d)}$$

This is **not** the same as RRF (which is sum of `1/(k+r_i)`, not sum of log). However, if we approximate `log(1/(k+r)) ≈ -1/(k+r)` for small `r` (i.e., for top ranks), we get:

$$\log P(d | \{\pi_i\}) \approx \text{const} - \sum_i \frac{1}{k + r_i(d)}$$

Maximizing the posterior is equivalent to **maximizing the RRF score**. So RRF is an approximation to the MAP estimate under the `1/(k+r)` noise model.

### 17.3 Why `k = 60` is robust

The `1/(k+r)` noise model has a single parameter `k`. For very small `k` (e.g., 1), the model is sharp — only the top-1 item from each list matters. For very large `k` (e.g., 1000), the model is flat — all ranks contribute equally.

The empirical `k = 60` from Cormack et al. corresponds to a model where the top-60 ranks are roughly equally informative — beyond rank 60, the contribution is dominated by noise. This is empirically calibrated on TREC data; for Recall's hybrid search (dense ANN + FTS), the same `k = 60` works because:

- Dense ANN's recall@10 is ~0.91; items ranked 1–10 are signal, items 11–60 are noisy but still informative.
- FTS's recall@10 is ~0.78; items ranked 1–10 are signal, items 11–60 are noisier.
- A `k = 60` flatly weights both lists, which is the right behavior when one list is much stronger than the other.

### 17.4 Outer RRF across tiers (Recall-specific)

Recall runs a second RRF pass to fuse results across tiers (e.g., hot tier top-40 + warm tier top-40 + cold tier top-40 → top-20 fused). The same `1/(k+r)` model applies — each tier is a noisy observation of the true ranking, with the cold tier being noisier than the hot tier.

The simplification (same `k` for inner and outer RRF) is acceptable because:

1. The cold tier's noise is already reflected in its recall@10 (lower); RRF weights by rank, not by recall.
2. The cold tier is queried less often (most queries target hot/warm); the few cold-tier hits that surface are typically high-rank, which dominates the RRF score.

A v1.1 optimization is to use a tier-weighted RRF where the cold tier's contribution is down-weighted by a learned factor; this requires a small labeled set (which the golden-query set provides).

---

## 18. Appendix C — References

### 18.1 Foundational papers

- **Malkov & Yashunin (2018)** — "Efficient and robust approximate nearest neighbor search using Hierarchical Navigable Small World graphs." arXiv:1603.09320. CoRR 2018. The HNSW paper.
- **Kusupati et al. (2022)** — "Matryoshka Representation Learning." NeurIPS 2022. The MRL paper; EmbeddingGemma 2 uses this training technique.
- **Cormack, Clarke & Buettcher (2009)** — "Reciprocal Rank Fusion outperforms Condorcet and individual rank learning methods when combining effective retrieval systems." UMass Amherst. The RRF paper.
- **Subramoney et al. (2024)** — EmbeddingGemma 2 technical report (referenced via Google Developers Blog).

### 18.2 Specifications & standards

- **RFC 5869** — HMAC-based Extract-and-Expand Key Derivation Function (HKDF).
- **RFC 7748** — Elliptic curves for security (X25519 used for ECDH in sync pairing).
- **IETF CFRG OPAQUE draft** — `draft-irtf-cfrg-opaque-13`. The PAKE used for first-pair device authentication.
- **NIST SP 800-38D** — Galois/Counter Mode (GCM) for AES.

### 18.3 SDK documentation

- **ZVec architecture blog:** <https://zvec.org/en/blog/2026-04-29-zvec-architecture>
- **ZVec.NET GitHub:** <https://github.com/ahmedSamir50/AdamSystems.ZVec.NET>
- **ZVec.NET docs site:** <https://ahmedsamir50.github.io/AdamSystems.ZVec.NET/>
- **Alibaba ZVec upstream:** <https://github.com/alibaba/zvec>
- **EmbeddingGemma 2 model card:** <https://huggingface.co/google/embeddinggemma-2>
- **EmbeddingGemma 2 ONNX export:** <https://huggingface.co/onnx-community/embeddinggemma-2-ONNX>
- **ONNX Runtime C# API:** <https://learn.microsoft.com/dotnet/api/microsoft.ml.onnxruntime>
- **.NET MAUI docs:** <https://learn.microsoft.com/dotnet/maui>
- **Blazor Hybrid docs:** <https://learn.microsoft.com/aspnet/core/blazor/hybrid>

### 18.4 Source materials (uploaded to this workspace)

- `IdeaTank_Embeddinggemma_2_ZVecNET.md` — the strategic brainstorm that originated the Recall super-app concept (§3) and verified stack capabilities (§1).
- `Recall_App_Implementation_Plan.md` — the companion implementation plan with Gantt, perf budget, risk register, test strategy, data corpus, sync design (deferred), RACI, exit criteria.

### 18.5 Open questions for v1.5+ research

These are deliberately flagged as known unknowns for the next iteration:

1. **RaBitQ on ARM** — Can the upstream ZVec RaBitQ be extended to NEON SIMD? If yes, mobile memory savings ~8× become possible. (Plan §9 R-7 mitigation.)
2. **DiskANN on `linux-arm64`** — ZVec lists Linux-only; ARM status unclear. If supported, a Raspberry Pi class edge box could host a billion-vector archive.
3. **Vamana build time at 100M** — Gates whether the v1.5 archive tier (Vamana) is days or months for very large personal corpora.
4. **Fine-tuned MRL head for multimodal** — The published MRL benchmarks are text-only. A fine-tuned projection head for image/audio may close the cold-tier recall gap (Plan §9 R-3).
5. **Cross-encoder reranker ONNX** — A small (~30M) bge-reranker-style model could replace the outer-RRF for tier fusion, with a learned weight per tier. Adds ~50 ms latency; v1.5 evaluation.
6. **On-device LLM (Gemma 3 decoder)** — Parked per IdeaTank §7. If a 2B-class Gemma 3 fits in mobile RSS, the parked `Recall.Chat` becomes viable with no architecture changes to the retrieval layer.

---

## 19. Appendix D — Zero-LLM Deterministic Cluster Labeler (v1.2 — replaces killed LLM sidecar)

> **Why this appendix exists:** v1.1 proposed a ≥1B-param LLM sidecar (~600 MB INT4) for one nightly batch feature: cluster title + summary generation. The user's v1.2 pressure-test correctly identified this as wasteful ("1.5B / 600 MB for a single feature is wasteful — find a lightweight way or kill it"). The LLM sidecar is **killed** in v1.2. The cluster label feature survives, implemented as a **zero-LLM deterministic labeler** documented in this appendix. The historical v1.1 reasoning for why each originally-proposed LLM use case was dropped is preserved in §19.6 as a v1.1 historical record, so the decision is not re-debated later.

### 19.1 The label format

For each cluster, the labeler produces a single string of the form:

```
{topic} — {date_range} ({modality_count})
```

Concrete examples across languages and cluster shapes:

| Cluster shape | Example label |
|---|---|
| English text cluster, 8 notes spanning Sep 1–15, 2025 | `Q3 Planning — Sep 1–15, 2025 (8 notes)` |
| English image cluster, 23 photos spanning July 2025 | `Rome — Jul 2025 (23 photos)` |
| Arabic mixed cluster, 47 photos on a single day | `حفلة عيد الميلاد — 8 مارس 2025 (47 صورة)` |
| Hindi text cluster, 5 notes spanning Dec 2024 – Feb 2025 | `यात्रा योजना — दिस 2024 – फर 2025 (5 नोट्स)` |
| Mixed-language image cluster, 12 photos spanning Aug 2025 | `Beach — Aug 2025 (12 photos)` |
| Image cluster with no clear TF-IDF topic (e.g., 30 random screenshots) | `— Aug 2025 (30 photos)` (empty topic slot; date + count still informative) |
| Cross-modal cluster, 3 photos + 2 notes about the same event | `Wedding — Jun 14, 2025 (3 photos, 2 notes)` (dominant modality first, others listed) |

**Why this format works without an LLM:**

- **Topic word** = top-1 TF-IDF term across the cluster's text payloads (cluster's natural language preserved; no translation needed).
- **Date range** = earliest-to-latest `captured_at`, formatted via ICU `CultureInfo.DateTimeFormat` in the user's locale (e.g., `Sep 1–15, 2025` in en-US; `1–15 set 2025` in fr-FR; `8 مارس 2025` in ar-EG).
- **Modality count** = pluralized in the user's locale via ICU `MessageFormat` (e.g., `1 photo` vs `23 photos` in English; `1 صورة` vs `23 صورة` in Arabic — Arabic has a complex plural system that ICU handles natively).

The label is **always informative** even when the topic is weak (the date + count carry signal). The user navigating the cluster browser sees functional labels; they click into a cluster to see the medoid preview if they want detail.

### 19.2 Why no LLM is needed

The LLM was originally proposed because I claimed the embedder "can't produce clean human-readable titles." That was wrong. The deterministic labeler produces labels that are:

1. **Multilingual by construction** — the topic word stays in the cluster's natural language (whatever language the underlying documents use); the date + count are locale-formatted via ICU. No translation needed; no LLM needed.
2. **Interpretable** — the user can see *why* a cluster got its label (TF-IDF term is in the cluster's text). The LLM title "Travel — Italy 2025" is opaque by comparison — the user has no way to know why the LLM picked that.
3. **Re-generatable any time** — labels update as items are added/removed, with no batch job. Re-runs the TF-IDF computation in milliseconds.
4. **No failure mode** — if TF-IDF returns junk (e.g., "the" or "and" as top term), the labeler falls back to date_range + modality_count only. Always informative.
5. **Zero disk, zero RSS, zero opt-in** — no model, no download, no ~600 MB INT4 file. ~30 lines of C# + the existing ICU dependency (already shipped with .NET MAUI).

The cost: the labels are **less polished** than LLM-generated ones. "Rome — Jul 2025 (23 photos)" is functional but not as smooth as "Travel — Italy 2025". The user's pressure-test correctly identified this is an acceptable trade — the cluster browser still works, the labels still orient the user, and we save ~600 MB of disk + ~600 MB of RSS + the opt-in download complexity + the INT4 quantization work + the model selection effort + the nightly batch infra for LLM inference.

### 19.3 The labeler algorithm

```
For each cluster (output of the nightly k-means / HDBSCAN pass):
   │
   ├──► Collect text payloads of all cluster items
   │       (text items: their text; image items: their OCR'd caption;
   │        audio items: their transcribed text — parked in v1;
   │        video frame items: no text payload)
   │
   ├──► Compute cluster-level TF-IDF over the collected payloads
   │       (corpus = cluster's own items; document frequency is local)
   │
   ├──► Extract top-1 TF-IDF term with constraints:
   │       - minimum 3 characters (filters "the", "and", "a", "of")
   │       - not a stop word in any of the cluster's detected languages
   │       - if no term passes, topic = "" (empty)
   │
   ├──► Find earliest + latest captured_at in the cluster
   │
   ├──► Format date_range via ICU in user's locale:
   │       - same day:   "Mar 8, 2025"
   │       - same month: "Jul 1–23, 2025"   (locale-dependent separator)
   │       - same year:  "Jul 15 – Sep 3, 2025"
   │       - cross-year: "Dec 2024 – Mar 2025"
   │
   ├──► Format modality_count via ICU MessageFormat in user's locale:
   │       - dominant modality first; minority modalities appended
   │       - example: "3 photos, 2 notes" (en)
   │       - example: "3 صور، 2 ملاحظات" (ar — ICU plural rules)
   │
   ├──>> Compose label: $"{topic} — {date_range} ({modality_count})"
   │       (or $"— {date_range} ({modality_count})" if topic is empty)
   │
   └──>> INSERT INTO cluster_metadata(cluster_id, label, topic, date_range,
                                      modality_count, generated_at)
```

Total compute per cluster: ~5–20 ms (TF-IDF over ≤100 short documents + ICU format). For 500 clusters, total batch time is ~5–10 seconds — fits trivially in the nightly window.

### 19.4 C# implementation

```csharp
// Recall.Core/Clustering/DeterministicClusterLabeler.cs
internal sealed class DeterministicClusterLabeler
{
    private readonly TfIdfVectorizer _tfidf;
    private readonly TimeProvider _clock;
    private readonly CultureInfo _userLocale;

    public DeterministicClusterLabeler(
        TfIdfVectorizer tfidf, TimeProvider clock, CultureInfo userLocale)
    {
        _tfidf = tfidf;
        _clock = clock;
        _userLocale = userLocale;
    }

    public ClusterLabel Generate(Guid clusterId, IReadOnlyList<RecallItem> items)
    {
        var texts = items
            .Where(i => !string.IsNullOrWhiteSpace(i.TextPayload))
            .Select(i => i.TextPayload!)
            .ToList();

        var topic = texts.Count > 0
            ? _tfidf.ExtractTopTerm(texts, minLength: 3, stopWordLanguages: DetectLanguages(texts))
            : string.Empty;

        var earliest = items.Min(i => i.CapturedAt);
        var latest = items.Max(i => i.CapturedAt);
        var dateRange = FormatDateRange(earliest, latest, _userLocale);

        var modalityCount = FormatModalityCount(items, _userLocale);

        var label = string.IsNullOrEmpty(topic)
            ? $"— {dateRange} ({modalityCount})"
            : $"{topic} — {dateRange} ({modalityCount})";

        return new ClusterLabel(
            ClusterId: clusterId,
            Topic: topic,
            DateRange: dateRange,
            ModalityCount: modalityCount,
            Label: label,
            GeneratedAt: _clock.GetUtcNow().UtcDateTime);
    }

    private static string FormatDateRange(DateTime earliest, DateTime latest, CultureInfo locale)
    {
        // Same day
        if (earliest.Date == latest.Date)
            return earliest.ToString("MMM d, yyyy", locale);

        // Same month
        if (earliest.Year == latest.Year && earliest.Month == latest.Month)
            return $"{earliest.ToString("MMM d", locale)}–{latest.Day}, {latest.Year}";

        // Same year
        if (earliest.Year == latest.Year)
            return $"{earliest.ToString("MMM d", locale)} – {latest.ToString("MMM d", locale)}, {latest.Year}";

        // Cross-year
        return $"{earliest.ToString("MMM yyyy", locale)} – {latest.ToString("MMM yyyy", locale)}";
    }

    private static string FormatModalityCount(IReadOnlyList<RecallItem> items, CultureInfo locale)
    {
        var groups = items.GroupBy(i => i.Modality)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)   // stable secondary order
            .ToList();

        var parts = groups.Select(g =>
        {
            var count = g.Count();
            var singular = g.Key switch
            {
                "text"  => "note",
                "image" => "photo",
                "audio" => "voice memo",
                "video" => "video frame",
                _ => "item"
            };
            var plural = g.Key switch
            {
                "text"  => "notes",
                "image" => "photos",
                "audio" => "voice memos",
                "video" => "video frames",
                _ => "items"
            };
            // Use ICU MessageFormat for locale-aware pluralization.
            // .NET 10 ships System.Globalization.MessageFormatter (preview in .NET 9).
            var word = MessageFormatter.Format(
                $"{count} {{count, plural, one{{{singular}}} other{{{plural}}}}}",
                locale,
                new { count });
            return word;
        });

        return string.Join(", ", parts);
    }

    private static IReadOnlyList<string> DetectLanguages(IReadOnlyList<string> texts)
    {
        // Lightweight multilingual language detection — re-use the same
        // fast-text classifier already shipping for ingestion metadata.
        // Returns ISO 639-1 codes: ["en"], ["ar"], ["en", "ar"], etc.
        return LanguageDetector.DetectAll(texts).Take(3).ToList();
    }
}

public sealed record ClusterLabel(
    Guid ClusterId,
    string Topic,
    string DateRange,
    string ModalityCount,
    string Label,
    DateTime GeneratedAt);
```

The full labeler is ~80 lines including the helpers. The dependencies it pulls in:
- `TfIdfVectorizer` — pure C# (~200 LOC; no external library)
- `LanguageDetector` — already shipped for ingestion metadata (per-item `Language` field)
- `System.Globalization.MessageFormatter` — shipped with .NET 10 (preview in .NET 9; ICU plural rules)

Total new code in `Recall.Core`: ~280 LOC. Total new disk: 0. Total new RSS: 0.

### 19.5 Why the cluster browser (Epic 2) still ships in v1.5

The cluster browser UI (Plan §15.4 Epic 2 — Obsidian-style knowledge graph view) still ships in v1.5. The browser displays the labels produced by this labeler next to cluster nodes. Without an LLM, the labels are functional ("Rome — Jul 2025 (23 photos)") rather than polished ("Travel — Italy 2025") — but the browser UX works identically: the user sees a node, sees its label, sees its edge count, clicks to expand.

If a future iteration wants polished labels, the door is open: drop in an LLM later as a v2+ opt-in, and the labeler interface (`IClusterLabeler`) is the only thing that changes — the browser UX, the cluster IDs, the similarity edges all stay the same.

### 19.6 Historical record — v1.1 LLM reasoning (preserved for context)

This subsection is preserved from v1.1 so future iterations understand why the LLM was originally proposed, why each use case was dropped, and why the cluster-title use case (the only one that survived v1.1) was killed in v1.2.

#### 19.6.1 v1.1 originally proposed an LLM for 6 use cases

The v1.1 plan called for a ≥1B-param LLM sidecar (Granite 3B-06L or Qwen2.5-1.5B, INT4, ~600 MB) for these use cases:

1. NL → structured filter (e.g., "photos from last summer in Cairo" → `{modality:image, from:2025-06-01, to:2025-08-31, location_tag:Cairo}`)
2. Query expansion (e.g., "red scooter" → "red scooter / moped / vespa")
3. Cluster title + summary generation
4. Snippet extraction (pick most relevant sentence from BM25 hit)
5. Auto-tagging (emit 3–5 tags per item)
6. Intent parsing (e.g., "what was I doing in Cairo last summer?" → multi-query plan)

#### 19.6.2 The user's v1.1 pressure-test dropped 5 of 6 use cases

After v1.1 review, the user pressure-tested each use case. The validation:

| Original use case | Why dropped in v1.1 | Replacement |
|---|---|---|
| NL → structured filter | Tiny LLMs <1B struggle with multilingual instruction-following + temporal reasoning + structured output together. LFM2.5-230M is shaky on Arabic; even Qwen2.5-0.5B and Phi-3.5-mini-instruct struggle with "last summer" → relative date logic in non-English. The realistic floor for reliable multilingual tool-calling is 1B–3B params — too heavy for one query-time feature. | **NER + rule-based parser.** A small multilingual NER model (~50–100 MB INT4 — quantized XLM-RoBERTa NER head or spaCy multilingual pipeline) extracts entities (Cairo → location_tag; "last summer" → temporal phrase). A deterministic rule-based parser resolves "last summer" → date range from the current date. |
| Query expansion | EmbeddingGemma 2's 768-d space already places these synonyms in close neighborhood (MTEB multilingual 61.36). Dense ANN retrieval naturally surfaces documents containing "moped" when you query "scooter". LLM expansion is redundant and can hurt (false synonyms, wrong domain). | **No action needed.** The embedder handles this natively. |
| Snippet extraction | BM25 already returns the matching text passage with highlights (FTS indices do this by default). For dense-only matches on long documents, re-embed each sentence with EmbeddingGemma 2 and pick the highest-cosine. ~10 sentences × 25 ms = ~250 ms; cache per query. | **Sentence-level embedding.** Re-uses the 270M text encoder already resident. No LLM. |
| Auto-tagging | k-NN against a multilingual hand-labeled seed set (500 items × 5 languages × 50 conceptual tags) using EmbeddingGemma 2. The seed set is hand-curated; new items get tags by majority vote of their k-nearest labeled neighbors. | **k-NN classify.** This is IdeaTank §5.1.2 (Sigil's `classify` task prefix). Uses EmbeddingGemma 2 only. |
| Intent parsing | Same multilingual issue as NL→filter. A tiny LLM cannot reliably parse intent in Arabic/Hindi. | **NER + rule-based parser.** Same approach as NL→filter. |
| **Cluster title + summary** | Survived v1.1 — generative work (clean human-readable text from noisy mixed-language inputs). Spec'd at ≥1B params. | **v1.2: KILLED.** Replaced with the zero-LLM deterministic labeler documented in §19.1–19.4 of this appendix. |

#### 19.6.3 v1.2 pressure-test killed the last remaining LLM use case

The v1.1 plan kept cluster titles as the one surviving LLM use case, spec'd at Granite 3B-06L or Qwen2.5-1.5B (INT4, ~600 MB). The v1.2 user pressure-test:

> "You chose to add a 1.5B 600MB sidecar for single feature 'Cluster title generation' → no hard rejection; we should find a very efficient way, more lightweight sidecar or kill this feature, avoiding the sidecar totally."

The validation: the deterministic labeler (§19.1–19.4) produces functional labels at zero model cost. The labels are less polished than LLM-generated ones, but:
- "Rome — Jul 2025 (23 photos)" is informative
- "Travel — Italy 2025" is polished but opaque (no way for the user to verify why)
- The cluster browser works the same way regardless
- We save 600 MB disk + 600 MB RSS + the opt-in download complexity + the INT4 quantization work + the model selection effort + the nightly LLM batch infra

The trade-off is clearly in favor of the deterministic labeler. The LLM is killed.

#### 19.6.4 What this means for v1 and v1.5

- **v1**: ship with the deterministic labeler. Cluster IDs are computed by the nightly clustering job (using EmbeddingGemma 2 `[cluster]` task prefix — already in the design). Labels are computed by the labeler immediately after clustering. ~5–10 seconds of compute per night.
- **v1.5**: ship the cluster browser UI (Epic 2) that displays these labels next to cluster nodes. No LLM dependency.
- **v2+ (parked)**: if a future iteration wants polished labels, an LLM sidecar can be added as an opt-in download — the `IClusterLabeler` interface is the only thing that changes. Not on the v1.5 roadmap.

---

## 20. Appendix E — Per-Platform Filesystem Watcher Matrix (v1.1)

> **Why this appendix exists:** v1.1 introduced the scope-only capture model (no ambient OS-signal capture). The capture mechanism is now per-platform filesystem watchers within user-scoped paths. This appendix documents the per-platform APIs, their reliability characteristics, and the reconciliation sweep that catches missed events.

### 20.1 Per-platform FS watcher API

| Platform | API | Reliability | Background? | Notes |
|---|---|---|---|---|
| Windows | `System.IO.FileSystemWatcher` (wraps `ReadDirectoryChangesW`) | Medium — internal buffer overflow under high load | Yes (background service) | Set `InternalBufferSize=64KB` (max); `IncludeSubdirectories=true`; enable `NotifyFilter` for `FileName,DirectoryName,LastWrite,Size`; retry on `Error` event. |
| macOS | `CoreServices.FSEventStream` | High — Apple's Coalescing Event Delivery API is robust | Yes (background service) | Set `latency=1.0s` for batched events; `flags=WatchRoot|FileEvents|NoDefer`. |
| Android | `android.os.FileObserver` (per-file) or `Storage Access Framework` document-tree observer (per-tree) | Medium — `FileObserver` is per-file, expensive for large scopes; SAF is modern, scoped-storage-compliant | Limited — Doze mode may suspend the observer | Use SAF for Android 11+; fall back to FileObserver for older. Reconciliation sweep every 4 hours catches Doze-mode gaps. |
| iOS | `DispatchSource.FileSystemObject` (per-file) or `UIDocumentPickerViewController` + re-scan on app foreground | **No background watching** — iOS sandbox forbids it | **No** — scan on app foreground only | By design: on app activation, walk scoped paths, diff against `checkpoints.db`, ingest new items. The macOS variant uses real `FSEvents` and has no such gap. |

### 20.2 Reconciliation sweep (catches missed events)

All platforms run a periodic reconciliation sweep that walks scoped paths and diffs against `checkpoints.db`:

| Platform | Sweep frequency | Why |
|---|---|---|
| Windows | Every 4 hours | `FileSystemWatcher` reliability is medium; sweep catches buffer-overflow misses. |
| macOS | Every 24 hours (or never — `FSEvents` is reliable enough) | Sanity check; the watcher is robust. |
| Android | Every 4 hours | Doze mode may suspend the watcher; sweep catches gaps. |
| iOS | Every app foreground | The watcher doesn't run in background; foreground scan is the trigger. |

### 20.3 Scope normalization (deduplicate overlapping paths)

When the user adds a new path to the scope, the `IScopeManagerService` checks for overlaps with existing scoped paths:

```csharp
// Recall.Core/Scope/IScopeManagerService.cs
public interface IScopeManagerService
{
    IReadOnlyList<ScopeEntry> GetActiveScopes();
    ValueTask<ScopeAddResult> AddScopeAsync(string path, IReadOnlySet<string> fileTypes, CancellationToken ct);
    ValueTask<ScopeRemoveResult> RemoveScopeAsync(string path, CancellationToken ct);
}

public sealed record ScopeEntry(
    string NormalizedPath,         // e.g. "D:\\" or "/home/user/photos"
    IReadOnlySet<string> FileTypes, // e.g. {"*.jpg", "*.png"} or {"*"} for all
    DateTime AddedAt);

public sealed record ScopeAddResult(
    bool Success,
    string? Warning,               // e.g. "D:\Photos\ is a sub-path of D:\; specific filter applied"
    ScopeEntry? FinalEntry);
```

The scope manager:
1. Normalizes paths (resolves symlinks, strips trailing slashes, case-folds on case-insensitive filesystems).
2. Detects subset/superset relationships — if the new path is a sub-path of an existing scope, the more-specific file-type filter takes precedence for files in that sub-path.
3. The FS watcher registers only ONE watch per directory (not two); the more-specific filter is applied at the file-event routing layer.

### 20.4 File-event routing to modality encoder

When the FS watcher emits a `FileEvent`, the router inspects the file extension / MIME type and routes to the appropriate encoder:

```csharp
// Recall.Core/Scope/FileEventRouter.cs
internal static class FileEventRouter
{
    public static EncoderMode? RouteToEncoder(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".txt" or ".md" or ".pdf" or ".docx" => EncoderMode.Text,
            ".jpg" or ".png" or ".heic" or ".webp" => EncoderMode.TextVision,
            ".mp4" or ".mov" or ".avi" or ".mkv" => EncoderMode.TextVision,  // frame sampler
            ".wav" or ".mp3" or ".m4a" or ".flac" =>
                OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst()
                    ? EncoderMode.Full
                    : null,   // audio skipped on mobile v1
            _ => null   // unknown extension; not indexed
        };
    }
}
```

Files with extensions not in the router table are not indexed (logged in `checkpoints.db` as `skipped_unknown_extension`).

### 20.5 Why this design fits the scope-only thesis

The FS-watcher approach is the Obsidian/Syncthing model: the user picks the folders, the app watches them. No clipboard, no camera roll, no screen OCR, no microphone. The only OS permission asked is storage access for the user-scoped paths — a well-understood, low-creep permission. The OS permission sheet reads "Allow Recall to access photos and media on this device" (Android) or "Recall would like to access your Documents folder" (macOS) — same sheet the user already grants to Obsidian, Notion, VSCode, or any other file-based app.

This is the right scope-only model for v1. Ambient capture (clipboard, camera, screen, mic) remains a separate Epic (Plan §15.4 Epic 1), gated behind explicit user opt-in, post-v1.5.
