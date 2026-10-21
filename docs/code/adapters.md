## src/Recall.Adapters/Indexing/ZVecTextVectorIndex.cs

```csharp
namespace Recall.Adapters.Indexing;
public sealed class ZVecTextVectorIndex : ITextVectorIndex, IDisposable
    public ZVecTextVectorIndex(string databaseRoot, IIndexPolicy? indexPolicy = null)
    public bool TryInsert(TextIndexInsert insert)
    public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default)
    public void Dispose()
        if (Directory.Exists(rootPath))
        if (!SchemaHasFtsKey(collection.Schema))
        if (knownIds.Contains(insert.ContentId))
        if (collection.Fetch(insert.ContentId, includeVector: false) is not null)
        if (!result.IsSuccess)
        if (collection.Fetch(contentId, includeVector: false) is null)
        if (existing is null)
        if (request.UseInDatabaseRrf)
        if (request.QueryVector512.Length > 0)
        if (schema is null)
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

`IsModelReady` is `File.Exists` of `options.ModelFileName` only. `EnsureLoaded` throws `RecallFaultException(RecallFault.TextModelNotReady)` when the GGUF is missing. `Open` runs once; a non-zero code throws `InvalidOperationException` with message `recall_embed open failed with code CODE.` `Dispose` disposes the encoder, calls `Close` once, then disposes the gate.

`recall_embed_open` return codes used from C#: 0 success; 1 null argument; 2 model load failed; 3 tokenize failed; 4 more than 8192 tokens; 5 encode or decode failed; 6 null embedding pointer from native; 7 output width is not 768; 8 already open; 9 not open. `recall_embed_count_tokens` writes the token count and returns 0; a non-zero count code throws through `RecallEmbedResults.RequireCount`.


## src/Recall.Adapters/Embedding/Llama/LlamaTextTokenCounter.cs

```csharp
namespace Recall.Adapters.Embedding.Llama;
public sealed class LlamaTextTokenCounter : ITextTokenCounter
    public LlamaTextTokenCounter(IRecallEmbedLibrary library, SemaphoreSlim gate)
    public int CountTokens(string text)
```

Blank text throws `ArgumentException`. Embed and count share one `SemaphoreSlim(1, 1)`.

No branch.


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

One JPEG at a time. The first call opens the mmproj after ensuring the text runtime is open, then every call embeds and unloads in `finally`. A non-zero open code throws `InvalidOperationException` with `recall_embed vision open failed with code CODE.` An embed throw still closes the vision handle. `IsModelReady` checks the mmproj file only.

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
public bool TryDelete(string contentId)
public IReadOnlyList<MediaSearchHit> Search(MediaSearchRequest request, CancellationToken cancellationToken = default)
public void Dispose()
if (width != MediaCollections.Width)
if (!MediaCollections.IsMedia(collectionName))
if (collections.ContainsKey(collectionName))
if (Directory.Exists(rootPath))
if (collection.Fetch(insert.ContentId, includeVector: false) is not null)
if (collection.Fetch(contentId, includeVector: false) is null)
if (collection.Delete(contentId).IsSuccess)
if (!MediaCollections.IsMedia(collectionName))
if (!collections.ContainsKey(collectionName))
if (disposed)
```

Only `items_image` and `items_video` at width 768. The schema uses HNSW cosine with the text index policy's `M` and `EfConstruction`, `QuantizeType` stays `Undefined`, and the `words` field uses the standard tokenizer with the lowercase filter. Inserts are idempotent by `ContentId`; `TryDelete` scans both open collections. Search uses the single-query overload (a one-element multi-query is rejected by the native library), maps dense hits, and parses the second fields with invariant culture. A missing collection is created on first use. `Dispose` closes collections and shuts the factory down.
