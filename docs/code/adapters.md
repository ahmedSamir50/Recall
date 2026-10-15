## src/Recall.Adapters/Indexing/ZVecTextVectorIndex.cs

```csharp
namespace Recall.Adapters.Indexing;
public sealed class ZVecTextVectorIndex : ITextVectorIndex, IDisposable
    public ZVecTextVectorIndex(string databaseRoot, IIndexPolicy? indexPolicy = null)
    public bool TryInsert(TextIndexInsert insert)
    public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default)
    public void Dispose()
        if (Directory.Exists(rootPath))
        if (knownIds.Contains(insert.ContentId))
        if (collection.Fetch(insert.ContentId, includeVector: false) is not null)
        if (!result.IsSuccess)
            if (!IsUnderGrant(path, request.GrantedPaths))
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


## src/Recall.Adapters/Embedding/Onnx/OnnxTextEmbeddingEncoder.cs

```csharp
namespace Recall.Adapters.Embedding;
public sealed class OnnxTextEmbeddingEncoder : ITextEmbeddingEncoder, IDisposable
    public OnnxTextEmbeddingEncoder(string modelPath, Tokenizer tokenizer, bool ownsTokenizer)
    public async ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
    public void Dispose()
        if (ownsTokenizer)
```


## src/Recall.Adapters/Embedding/Onnx/OnnxTextGraph.cs

```csharp
namespace Recall.Adapters.Embedding;
public static class OnnxTextGraph
    public static int[] EmptyFeatureDimensions(int[] metadataDimensions)
    public static void RequireTextInputs(IEnumerable<string> inputNames)
    public static void RequireFloat(Type elementType)
        if (metadataDimensions.Length == 0)
            throw new ArgumentException("Dimensions must not be empty.", nameof(metadataDimensions));
            if (value == 0)
        if (!hasZero)
        if (!names.Contains(InputIds)
            throw new InvalidOperationException("Text model inputs are missing.");
        if (elementType != typeof(float))
            throw new InvalidOperationException("Feature input must be float.");
```


## src/Recall.Adapters/Embedding/Runtime/LazyOnnxTextEmbeddingEncoder.cs

```csharp
namespace Recall.Adapters.Embedding;
public sealed class LazyOnnxTextEmbeddingEncoder : ITextEmbeddingEncoder
    public LazyOnnxTextEmbeddingEncoder(TextModelRuntime runtime)
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
    public OnnxTextEmbeddingEncoder GetEncoder()
    public HuggingFaceTextTokenCounter GetCounter()
    public void Dispose()
        if (encoder is not null)
            if (encoder is not null)
            if (!IsModelReady)
                throw new RecallFaultException(RecallFault.TextModelNotReady);
            if (!TextTokenizerRuntime.IsSupported(shellKind))
                throw new RecallFaultException(RecallFault.TokenizerNativeUnsupported);
```


## src/Recall.Adapters/Embedding/Tokenizers/TextTokenizerRuntime.cs

```csharp
namespace Recall.Adapters.Embedding;
public static class TextTokenizerRuntime
    public static bool IsSupported(RecallShellKind kind) => kind is RecallShellKind.Windows or RecallShellKind.MacCatalyst or RecallShellKind.Unknown;
```

No branch.


## src/Recall.Adapters/Embedding/Tokenizers/HuggingFaceTextTokenCounter.cs

```csharp
namespace Recall.Adapters.Embedding;
public sealed class HuggingFaceTextTokenCounter : ITextTokenCounter, IDisposable
    public HuggingFaceTextTokenCounter(Tokenizer tokenizer, bool ownsTokenizer)
    public int CountTokens(string text)
    public void Dispose()
        if (ownsTokenizer)
```


## src/Recall.Adapters/Embedding/Tokenizers/HuggingFaceTokenizerFactory.cs

```csharp
namespace Recall.Adapters.Embedding;
```

No public type.

No branch.
