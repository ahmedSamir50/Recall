## src/Recall.Core/Sync/SyncFeature.cs

```csharp
namespace Recall.Core.Sync;
public static class SyncFeature
public interface ISyncAdapter
```

No branch.


## src/Recall.Core/Sync/LocalOpLogWriter.cs

```csharp
namespace Recall.Core.Sync;
public sealed class LocalOpLogWriter
public sealed record LocalOpLogEntry(OpLogOperation Operation, string ContentId, DateTimeOffset At, bool Sent);
    public LocalOpLogWriter(string? directory = null)
    public void AppendInsert(string contentId)
        if (string.IsNullOrWhiteSpace(directory))
```


## src/Recall.Core/Faults/RecallFault.cs

```csharp
namespace Recall.Core.Faults;
public enum RecallFault
```

No branch.


## src/Recall.Core/Faults/RecallFaultException.cs

```csharp
namespace Recall.Core.Faults;
public class RecallFaultException : Exception
    public RecallFaultException()
    public RecallFaultException(string message)
    public RecallFaultException(string message, Exception innerException)
    public RecallFaultException(RecallFault fault)
```

No branch.


## src/Recall.Core/Vocabulary/RecallVocabulary.cs

```csharp
namespace Recall.Core.Vocabulary;
public enum Modality
public enum StoredWidth
public enum AgeCutoffYears
public enum OpLogOperation
```

No branch.


## src/Recall.Core/Indexing/RecallIndexPolicy.cs

```csharp
namespace Recall.Core.Indexing;
public sealed class RecallIndexPolicy : IIndexPolicy
    public void EnsureStorableWidth(Modality modality, int width)
        if (width == BlockedWidth)
            throw new RecallFaultException(RecallFault.StoredWidthBlocked);
        if (modality == Modality.Text && width != (int)StoredWidth.W512)
            throw new RecallFaultException(RecallFault.TextWidthNot512);
```


## src/Recall.Core/Indexing/TextIndexOpenPolicy.cs

```csharp
namespace Recall.Core.Indexing;
public enum TextIndexOpenKind
public static class TextIndexOpenPolicy
    public static TextIndexOpenKind Decide(bool collectionDirectoryExists, bool hasFtsKey)
        if (!collectionDirectoryExists)
            return TextIndexOpenKind.Create;
        if (!hasFtsKey)
            return TextIndexOpenKind.Replace;
        return TextIndexOpenKind.Open;
```


## src/Recall.Core/Vectors/MatryoshkaVectorMath.cs

```csharp
namespace Recall.Core.Vectors;
public static class MatryoshkaVectorMath
    public static void L2Normalize(Span<float> vector)
    public static float[] CutAndNormalize(ReadOnlySpan<float> full768, int storedWidth)
    public static double L2Length(ReadOnlySpan<float> vector)
        if (sum <= 0d)
        if (full768.Length != NativeWidth)
            throw new RecallFaultException(RecallFault.EmbeddingWidthUnexpected);
        if (storedWidth <= 0 || storedWidth > NativeWidth)
            throw new RecallFaultException(RecallFault.TextWidthNot512);
```


## src/Recall.Core/Vectors/TokenMeanPool.cs

```csharp
namespace Recall.Core.Vectors;
public static class TokenMeanPool
    public static float[] Mean(ReadOnlySpan<float> tokenRows, int hiddenWidth)
        if (hiddenWidth != MatryoshkaVectorMath.NativeWidth || tokenRows.Length == 0 || tokenRows.Length % hiddenWidth != 0)
            throw new RecallFaultException(RecallFault.EmbeddingWidthUnexpected);
        if (rows == 1)
```


## src/Recall.Core/Ports/TextPorts.cs

```csharp
namespace Recall.Core.Ports;
public interface IIndexPolicy
public interface ITextEmbeddingEncoder
public interface ITextVectorIndex
public interface IGrantedTextIngestService
public interface ITextSearchService
public interface ILocalFileOpener
    void EnsureStorableWidth(Modality modality, int width);
    ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default);
    bool TryInsert(TextIndexInsert insert);
    IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default);
    Task IngestGrantedTextAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TextSearchHit>> FindAsync(string query, IReadOnlyList<ScopeFolderGrant> grants, CancellationToken cancellationToken = default);
    bool Exists(string sourcePath);
    Task<LocalFileOpenResult> OpenAsync(string sourcePath, CancellationToken cancellationToken = default);
```

No branch.



```csharp
IIndexNowService
``` 
## src/Recall.Core/Ports/ScopePorts.cs

```csharp
namespace Recall.Core.Ports;
public interface IFileScopeWatcherService
public interface IScopeManagerService
public interface IScopeGrantStore
public enum FolderPickerOutcome
public sealed record FolderPickerResult(FolderPickerOutcome Outcome, string? Path, string? ReopenToken = null);
public interface IFolderPickerService
public interface IGrantedFolderAccessService
public interface IVideoFrameSamplerService
public interface IIdleDetectorService
public type IdleSignal
IdleSignal
public enum IdleSignal
    IReadOnlyList<ScopeFolderGrant> ListGrants();
    ScopeGrantAddResult TryAddGrant(ScopeFolderGrant grant);
    void RemoveGrant(string path);
    void SetGrantFileKinds(string path, GrantFileKinds kinds);
    bool CanContinue();
    IReadOnlyList<ScopeFolderGrant> List();
    void Save(IReadOnlyList<ScopeFolderGrant> grants);
    Task<FolderPickerResult> PickFolderAsync(CancellationToken cancellationToken = default);
    void RestorePersistedAccess();
```

No branch.



```csharp
IScopeChangeSink
``` 
## src/Recall.Core/Ports/OnboardingPorts.cs

```csharp
namespace Recall.Core.Ports;
public interface IOnboardingCompletionStore
    bool IsComplete();
    void MarkComplete();
    void ResetToOnboarding();
```

No branch.


## src/Recall.Core/Ports/DebugPorts.cs

```csharp
namespace Recall.Core.Ports;
public interface IDebugAppReset
    Task ResetAllLocalDataAsync(CancellationToken cancellationToken = default);
```

No branch.


## src/Recall.Core/Ports/ITextTokenCounter.cs

```csharp
namespace Recall.Core.Ports;
public interface ITextTokenCounter
    int CountTokens(string text);
```

No branch.


## src/Recall.Core/DependencyInjection/RecallCoreServiceCollectionExtensions.cs

```csharp
namespace Recall.Core.DependencyInjection;
public static class RecallCoreServiceCollectionExtensions
    public static IServiceCollection AddRecallCore(this IServiceCollection services)
```

No branch.


## src/Recall.Core/Ports/IEncoderFileHash.cs

```csharp
namespace Recall.Core.Ports;
public type IEncoderFileHash
IEncoderFileHash
No branch.
``` 

## src/Recall.Core/Ports/ILocalContentProbe.cs

```csharp
namespace Recall.Core.Ports;
public type LocalContentState
LocalContentState
public type ILocalContentProbe
ILocalContentProbe
No branch.
``` 

## src/Recall.Core/Ports/MediaPorts.cs

```csharp
namespace Recall.Core.Ports;
public interface IMediaVectorIndex
public interface IVisionEmbeddingEncoder
public interface IPictureSearchService
```

`IMediaVectorIndex` has create, insert, delete, and search over a media collection; search takes `MediaSearchRequest`. `IVisionEmbeddingEncoder` fills a caller-owned 768-wide destination from JPEG bytes. `IPictureSearchService` takes JPEG bytes and the grant list. No branch.

