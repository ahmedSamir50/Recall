## src/Recall.Adapters/Indexing/ZVecTextVectorIndex.cs

```csharp
namespace Recall.Adapters.Indexing;
public sealed class ZVecTextVectorIndex : ITextVectorIndex, IDisposable
    public static ZVecCollectionSchema CreateSchema()
    public ZVecTextVectorIndex(string databaseRoot, IIndexPolicy? indexPolicy = null, IIngestCursorStore? cursors = null)
    public bool TryInsert(TextIndexInsert insert)
    public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default)
    public void Dispose()
        if (TextIndexOpenPolicy.Decide(Directory.Exists(rootPath), hasFtsKey: true) == TextIndexOpenKind.Create)
        if (TextIndexOpenPolicy.Decide(true, SchemaHasFtsKey(opened.Schema)) == TextIndexOpenKind.Replace)
        if (Directory.Exists(rootPath))
        if (knownIds.Contains(insert.ContentId))
        if (collection.Fetch(insert.ContentId, includeVector: false) is not null)
        if (!result.IsSuccess)
        if (collection.Fetch(contentId, includeVector: false) is null)
        if (existing is null)
        if (request.UseInDatabaseRrf)
        if (request.QueryVector512.Length > 0)
        if (schema is null)
        if (schema.Fields is not null && schema.Fields.Any(field => string.Equals(field.Name, FtsKeyField, StringComparison.Ordinal)))
        if (doc.Fields.TryGetValue(field, out var value) && int.TryParse(value?.ToString(), out var parsed))
        if (doc.Fields.TryGetValue(ModifiedField, out var value)
```


## src/Recall.Adapters/DependencyInjection/AdapterServiceCollectionExtensions.cs

```csharp
namespace Recall.Adapters.DependencyInjection;
public static class AdapterServiceCollectionExtensions
    public static IServiceCollection AddRecallAdapters(
        this IServiceCollection services,
        string modelDirectory,
        string vectorDatabaseRoot)
```

No branch.


## src/Recall.Adapters/Embedding/Llama/LlamaTextEmbeddingEncoder.cs

```csharp
namespace Recall.Adapters.Embedding.Llama;
public sealed class LlamaTextEmbeddingEncoder : ITextEmbeddingEncoder, IDisposable
    public async ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
    public void Dispose()
            if (code != 0)
```

`recall_embed_embed` return 0 copies 768 floats and L2-normalizes in C#. Any other code throws `InvalidOperationException` with message `recall_embed embed failed with code CODE.` Blank text throws `ArgumentException`. A cancelled token throws `OperationCanceledException` before the native call.


## src/Recall.Adapters/Embedding/Runtime/LlamaTextEmbeddingEncoderLazy.cs

```csharp
namespace Recall.Adapters.Embedding;
public sealed class LlamaTextEmbeddingEncoderLazy : ITextEmbeddingEncoder
    public LlamaTextEmbeddingEncoderLazy(TextModelRuntime runtime)
    public ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
```

No branch.


## src/Recall.Adapters/Embedding/Runtime/LazyTextTokenCounter.cs

```csharp
namespace Recall.Adapters.Embedding;
public sealed class LazyTextTokenCounter : ITextTokenCounter
    public LazyTextTokenCounter(TextModelRuntime runtime)
    public int CountTokens(string text) => runtime.GetCounter().CountTokens(text);
    public IReadOnlyList<int> TokenEnds(string text) => runtime.GetCounter().TokenEnds(text);
```

No branch.


## src/Recall.Adapters/Embedding/Runtime/TextModelRuntime.cs

```csharp
namespace Recall.Adapters.Embedding;
public sealed class TextModelRuntime : IDisposable
    public TextModelRuntime(
        string modelDirectory,
        RecallShellKind shellKind = RecallShellKind.Unknown,
        TextModelDownloadOptions? options = null)
    public LlamaTextEmbeddingEncoder GetEncoder()
    public LlamaTextTokenCounter GetCounter()
    public void Dispose()
        if (disposed != 0)
        if (opened)
        if (encoder is not null)
            if (encoder is not null)
            if (!IsModelReady)
            if (!opened)
                if (code != 0)
```

`IsModelReady` is `File.Exists` of `options.ModelFileName` only. `EnsureLoaded` throws `RecallFaultException(RecallFault.TextModelNotReady)` when the GGUF is missing. `Open` runs once; a non-zero code throws `InvalidOperationException` with message `recall_embed open failed with code CODE.` `Dispose` takes `LlamaNativeGate`, disposes the encoder, and calls `Close` once when this runtime opened the model. It does not dispose the shared gate.

`recall_embed_open` return codes used from C#: 0 success; 1 null argument; 2 model load failed; 3 tokenize failed; 4 more than 7680 tokens; 5 encode or decode failed; 6 null embedding pointer from native; 7 output width is not 768; 8 already open; 9 not open. `recall_embed_count_tokens` writes the token count and returns 0; a non-zero count code throws through `RecallEmbedResults.RequireCount`. `recall_embed_token_ends` tokenizes once, walks `llama_token_to_piece`, and writes each exclusive UTF-8 end up to `RECALL_EMBED_MAX_TOKENS`; a non-zero code throws `InvalidOperationException` with message `recall_embed token ends failed with code CODE.`


## src/Recall.Adapters/Embedding/Llama/LlamaTextTokenCounter.cs

```csharp
namespace Recall.Adapters.Embedding.Llama;
public sealed class LlamaTextTokenCounter : ITextTokenCounter
    internal LlamaTextTokenCounter(IRecallEmbedLibrary library)
    public int CountTokens(string text)
    public IReadOnlyList<int> TokenEnds(string text)
        if (byteEnds.Length == 0)
        if (ends.Count > 0 && charCursor < ends[ends.Count - 1])
        if (first is >= 0xC0 and <= 0xDF)
        if (first is >= 0xE0 and <= 0xEF)
        if (first is >= 0xF0 and <= 0xF7)
```

Blank text throws `ArgumentException`. Text embed, token count, vision, and audio share one process-wide `SemaphoreSlim(1, 1)` in `LlamaNativeGate`. The same thread re-enters by incrementing a thread-static depth. `TokenEnds` calls `recall_embed_token_ends` once and converts the exclusive UTF-8 byte ends into UTF-16 char ends in one pass: a 1-to-3 byte sequence adds one char, a 4-byte sequence adds two, and an end that lands inside a code point snaps forward to the next code point start.


## src/Recall.Adapters/InternalsVisibleTo.cs

No public type.

No branch.


## src/Recall.Adapters/Embedding/Llama/RecallEmbedNative.cs

```csharp
namespace Recall.Adapters.Embedding.Llama;
RecallEmbedResults
RequireCount
RecallEmbedNative
        if (code != 0)
        catch (DllNotFoundException)
        catch (EntryPointNotFoundException)
```

P/Invokes `recall_embed_open`, `recall_embed_close`, `recall_embed_count_tokens`, and `recall_embed_embed` with UTF-8 paths and a trailing null. `RecallEmbedResults.RequireCount` throws `InvalidOperationException` with message `recall_embed count failed with code CODE.` when the native count call is non-zero.

No public type.

## src/Recall.Adapters/Embedding/MediaContextProbe.cs

```csharp
namespace Recall.Adapters.Embedding;
public static class MediaContextProbe
        if (Volatile.Read(ref applied) != 0)
        if (!File.Exists(textModelPath) || !File.Exists(mmprojPath))
            if (library.Open(textModelPath) != 0)
            if (library.OpenVision(mmprojPath) != 0)
            if (library.EmbedVision(jpeg, vision) != 0)
```

One-day probe: open text and mmproj, embed a minimal JPEG and a PCM buffer without closing between them. When both succeed, `MediaContextPolicy.OneContext` is true. Missing files return without writing `OneContext` and without latching `applied`. A vision open or embed failure sets `OneContext` false and latches `applied`. The probe closes only a context that same call opened, and it does not call `CloseAudio`. `Mtmd_OneContext_EmbedsVisionThenAudio` locks the success path.

```csharp
if (openCode == 0)
else if (openCode != 8)
if (visionCode == 0)
else if (visionCode != 8)
catch (InvalidOperationException)
if (embedCode != 0)
if (openedVision)
if (openedText)
if (latch)
```

## src/Recall.Adapters/Embedding/Runtime/VisionModelRuntime.cs

```csharp
namespace Recall.Adapters.Embedding;
public sealed class VisionModelRuntime : IVisionEmbeddingEncoder, IDisposable
public VisionModelRuntime(
    string modelDirectory,
    TextModelRuntime textRuntime,
    VisionModelDownloadOptions? options = null)
internal VisionModelRuntime(
    string modelDirectory,
    IRecallEmbedLibrary library,
    VisionModelDownloadOptions? options = null)
public bool IsLoaded { get; private set; }
public bool IsModelReady => File.Exists(Path.Combine(modelDirectory, options.ModelFileName));
public void EmbedJpeg(ReadOnlyMemory<byte> jpeg, float[] destination768)
public void Dispose()
if (!opened)
if (code != 0)
if (opened)
if (disposed != 0)
if (opened)
```

One JPEG at a time. `EmbedJpeg` opens the mmproj after ensuring the text runtime is open only when this call did not find the session already open, and it closes in `finally` only then. A pass that called `OpenSession` leaves the session open. A non-zero open code throws `InvalidOperationException` with `recall_embed vision open failed with code CODE.` An embed throw still closes the vision handle when this call opened it. `IsModelReady` checks the mmproj file only.

```csharp
if (openedHere && opened)
```

## src/Recall.Adapters/Indexing/ZVecMediaVectorIndex.cs

```csharp
namespace Recall.Adapters.Indexing;
public sealed class ZVecMediaVectorIndex : IMediaVectorIndex, IDisposable
public const string EmbeddingField = "embedding";
public const string WordsField = "words";
public const string SourcePathField = "source_path";
public const string FileNameField = "file_name";
public const string PathLiveField = "path_live";
public const string EncoderHashField = "encoder_hash";
public const string GrantPathField = "grant_path";
public const string StartSecondsField = "start_seconds";
public const string EndSecondsField = "end_seconds";
public ZVecMediaVectorIndex(string databaseRoot)
public void Create(string collectionName, int width)
internal static ZVecCollectionSchema BuildSchema(string collectionName, int width)
public bool TryInsert(string collectionName, MediaIndexInsert insert)
public bool TryRead(string contentId, out MediaStoredVector vector)
public bool TryDelete(string contentId)
public void Reconcile(IReadOnlyList<VectorLiveState> states)
public IReadOnlyList<MediaSearchHit> Search(MediaSearchRequest request, CancellationToken cancellationToken = default)
if (string.IsNullOrWhiteSpace(query))
public void Dispose()
if (insert.Vector768.Length != expectedWidth)
if (width != expected)
if (!MediaCollections.IsMedia(collectionName))
if (collections.ContainsKey(collectionName))
if (Directory.Exists(rootPath))
if (!Directory.Exists(rootPath))
if (!TryOpenCollection(request.CollectionName, out var collection) || collection is null)
if (!TryOpenCollection(collectionName, out var collection) || collection is null)
if (doc is null)
if (collection.Fetch(insert.ContentId, includeVector: false) is not null)
if (collection.Fetch(contentId, includeVector: false) is null)
if (collection.Delete(contentId).IsSuccess)
if (existing is null)
if (!MediaCollections.IsMedia(collectionName))
if (!collections.ContainsKey(collectionName))
if (disposed)
```

Each media collection opens at `MediaCollections.WidthFor(collectionName)` (768 for long, 512 for `_old`). The schema uses HNSW cosine with the text index policy's `M` and `EfConstruction`, `QuantizeType` stays `Undefined`, and the `words` field uses the standard tokenizer with the lowercase filter. Create, insert, delete, reconcile, search, stored-field reads, and dispose take one lock so the UI thread and the thread pool cannot use the index together. Inserts are idempotent by `ContentId`; `TryDelete` scans every open collection; `Reconcile` flips only `path_live` on the ids it knows and keeps the stored `encoder_hash`, `grant_path`, and start fields. Search uses the single-query overload (a one-element multi-query is rejected by the native library), maps dense hits, and parses the stored fields with invariant culture. A missing collection is created on first use. `Dispose` closes collections and shuts the factory down.


## src/Recall.Adapters/Media/WindowsVideoFrameSampler.cs

```csharp
namespace Recall.Adapters.Media;
public sealed class WindowsVideoFrameSampler : IVideoFrameSamplerService
```

No video sampling package exists for Windows. A null stream throws `ArgumentNullException`; any other call throws `NotSupportedException` with exactly `Windows video sampling has no package.` No branch.

## src/Recall.Adapters/Embedding/Runtime/AudioModelRuntime.cs

```csharp
namespace Recall.Adapters.Embedding;
public sealed class AudioModelRuntime : IAudioEmbeddingEncoder, IDisposable
public const string OpenAudioFailedFormat = "recall_embed audio open failed with code {0}.";
public const string EmbedAudioFailedFormat = "recall_embed audio embed failed with code {0}.";
public AudioModelRuntime(
public bool IsModelReady => File.Exists(Path.Combine(modelDirectory, options.ModelFileName));
public bool IsLoaded { get; private set; }
public void EmbedPcm(ReadOnlyMemory<float> mono16k, float[] destination768)
if (!opened && !MediaContextPolicy.OneContext)
if (code != 0)
if (embedCode != 0)
public void CloseAudio()
if (opened)
public void Dispose()
if (disposed != 0)
if (opened)
```

```csharp
if (opened || MediaContextPolicy.OneContext)
```

## src/Recall.Adapters/Embedding/MediaContextPolicy.cs

```csharp
namespace Recall.Adapters.Embedding;
public static class MediaContextPolicy
public static bool OneContext { get; set; }
```

## src/Recall.Adapters/Embedding/Llama/LlamaNativeGate.cs

```csharp
namespace Recall.Adapters.Embedding.Llama;
No public type.
    if (depth > 0)
    if (depth > 1)
```

One process-wide `SemaphoreSlim(1, 1)`. A nested call on the thread that already holds it increments a thread-static depth. `Exit` releases only when that depth returns to zero.

## src/Recall.Adapters/Embedding/LlamaIngestModelSession.cs

```csharp
namespace Recall.Adapters.Embedding;
public sealed class LlamaIngestModelSession
public void OpenText()
public void CloseText()
public void OpenVision()
public void CloseVision()
public void OpenAudio()
public void CloseAudio()
public void EnsureProbe()
```

No branch.

The ingest pass opens and closes text, vision, and audio through this session. `EnsureProbe` holds `LlamaNativeGate` while `MediaContextProbe.EnsureApplied` runs.
