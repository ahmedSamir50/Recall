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
    public void AppendPin(string contentId)
    public void AppendUnpin(string contentId)
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
public enum PinResult
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
    Task<LocalFileOpenResult> OpenAtAsync(string sourcePath, double startSeconds, CancellationToken cancellationToken = default);
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
public bool TrySetWords(string contentId, string words);
public bool TryRead(string contentId, out MediaStoredVector vector);
public sealed record MediaStoredVector(string ContentId, string CollectionName, float[] Vector, string Words, string EncoderHash, string GrantPath, string SourcePath, string StartSeconds, string EndSeconds, bool PathLive);
public interface IVisionEmbeddingEncoder
public interface IPictureSearchService
public interface IAudioSearchService
public interface IAudioEmbeddingEncoder
public interface IMediaPreview
public bool IsLoaded { get; }
public void EmbedPcm(ReadOnlyMemory<float> mono16k, float[] destination768)
public IReadOnlyList<MediaSearchHit> SearchWords(string collectionName, string query, string filter, int topK, CancellationToken cancellationToken = default)
public Task PlayAsync(string path, double startSeconds, CancellationToken cancellationToken = default)
```

`IMediaVectorIndex` has create, insert, delete, `TrySetWords` on `items_image`, `TryRead`, word search, and search over a media collection; search takes `MediaSearchRequest`; `SearchWords` returns an empty list on a blank query and narrows word matches with the grant filter. `IVisionEmbeddingEncoder` fills a caller-owned 768-wide destination from JPEG bytes. `IPictureSearchService` takes JPEG bytes and the grant list. `IAudioSearchService` takes 16 kHz mono PCM and the grant list and searches long and `_old` audio collections. `IAudioEmbeddingEncoder` embeds 16 kHz mono PCM into a 768-wide destination and exposes `IsLoaded` so the unload step can skip a closed session. `IMediaPreview` shows one media file in the app player and starts at the given seconds. No branch.

## src/Recall.Core/Age/AgeCutGate.cs

```csharp
namespace Recall.Core.Age;
public static class AgeCutGate
public static bool Passes(Modality modality, IReadOnlyList<AgeCutItem> items)
```

`if (modality == Modality.Text)` throws `RecallFault.AgeCutTextForbidden`. `if (items.Count == 0)` returns false. `if (item.Stored768.Length != MatryoshkaVectorMath.NativeWidth || item.Query768.Length != MatryoshkaVectorMath.NativeWidth)` throws `RecallFault.EmbeddingWidthUnexpected`. `if (rank >= 0 && rank < 10)` counts a recall@10 hit. `if (!useCut)` uses the full 768 vector. `if (left.Length == 0 || right.Length == 0)` yields cosine zero. Passes when full-width and 512-width recall@10 differ by at most five points.

## src/Recall.Core/Age/IAgeCutDecision.cs

```csharp
namespace Recall.Core.Age;
public interface IAgeCutDecision
    bool TryPass(Modality modality);
```

Production type `FailClosedAgeCutDecision` loads `AgeCutCorpus` and returns false when the corpus is absent.

## src/Recall.Core/Age/AgeMoveService.cs

```csharp
namespace Recall.Core.Age;
public sealed class AgeMoveService
public bool TryMovePath(string path, Modality modality, DateTimeOffset utcNow)
public void Sweep(DateTimeOffset utcNow)
```

`if (string.IsNullOrWhiteSpace(path))` throws. `if (modality == Modality.Text)` returns false. `if (string.IsNullOrEmpty(encoderHash))` returns false. `if (lastWrite is null)` returns false. `if (mediaRow is null)` returns false. `if (chunks.Count == 0)` returns false. `if (string.IsNullOrEmpty(contentHash))` returns false. `if (cursors.IsPinned(path))` deletes a stray short row when both ids exist and returns false. `if (index.TryRead(longId, out _) && index.TryRead(shortId, out _))` deletes the short id while pinned. `if (hasShort)` keeps only the short row when not pinned. `if (hasLong)` deletes the long row when a short row already exists. `if (!FileAgePolicy.IsPastCutoff(lastWrite, years, utcNow))` returns false. `if (!decision.TryPass(modality))` returns false. `if (!hasLong)` returns false before staging. `if (anyMoved)` sets media width to 512. `if (index.TryRead(shortId, out _))` skips a short id that is already stored. `if (!index.TryRead(longId, out var longVector))` skips a missing long row. `if (stages.Count > 0)` commits the staged cuts. `if (reconciledShortOnly)` reconciles only the short ids.

## src/Recall.Core/Age/PinService.cs

```csharp
namespace Recall.Core.Age;
public sealed class PinService
public PinResult Pin(string path)
public PinResult Unpin(string path)
```

`if (string.IsNullOrWhiteSpace(path))` throws on pin and unpin. `if (modality is null)` returns `NotMedia` or removes pin on unpin. `if (mediaRow is null)` returns `NotMedia` on pin. `if (!facts.Exists(path))` pins without embed and returns `MissingFileStays`. `if (mediaRow.Width == MediaCollections.Width)` returns `AlreadyLong`. `if (!string.IsNullOrEmpty(longId))` appends pin or unpin to the op-log. `if (!cursors.IsPinned(path))` returns `NotPinned`. `if (mediaRow.Width == MediaCollections.OldWidth)` returns `UnpinReturnedShort`. `if (lastWrite is null || !FileAgePolicy.IsPastCutoff(lastWrite, years, DateTimeOffset.UtcNow))` returns `UnpinStaysLong`. `if (!decision.TryPass(modality.Value))` returns `GateFailedStaysLong`. `if (!cursors.IsPinned(row.Path))` skips resume delete when the pin row is gone. `if (chunk is null || string.IsNullOrEmpty(contentHash))` returns `NotMedia` after regrow. `if (!index.TryRead(longId, out _))` rolls back pin on failed regrow. `if (!index.TryRead(longId, out var stored))` skips a missing chunk on unpin cut. `if (MediaFileKinds.IsImage(path))` `if (MediaFileKinds.IsVideo(path))` `if (MediaFileKinds.IsAudio(path))` resolve modality. `if (chunk is null)` yields an empty long id. `if (chunks.Count == 0 || string.IsNullOrEmpty(contentHash))` returns before regrow. `if (string.IsNullOrEmpty(firstLongId))` returns when no long id was stored. `if (string.IsNullOrEmpty(contentHash))` returns when the file hash is blank.

## src/Recall.Core/Age/AgeCutoffActions.cs

```csharp
namespace Recall.Core.Age;
public static class AgeCutoffActions
public static void Set(AgeCutoffPreferenceService preferences, AgeMoveService ageMove, AgeCutoffYears years, DateTimeOffset utcNow)
```

`catch (RecallFaultException ex) when (ex.Fault == RecallFault.AgeCutoffNotAllowed)` rethrows without sweeping. A successful `preferences.Set` calls `ageMove.Sweep`.

## src/Recall.Core/Age/FileAgePolicy.cs

```csharp
namespace Recall.Core.Age;
public static class FileAgePolicy
public static bool IsPastCutoff(DateTimeOffset? lastWriteUtc, AgeCutoffYears years, DateTimeOffset utcNow)
```

Null last-write returns false. `if (lastWriteUtc is null)`. `if (years is not (AgeCutoffYears.Two or AgeCutoffYears.Five or AgeCutoffYears.Seven))` throws `RecallFault.AgeCutoffNotAllowed`. Cutoff is strict less-than `utcNow.AddYears(-(int)years)`.

## src/Recall.Core/Age/AgeCutItem.cs

```csharp
namespace Recall.Core.Age;
public readonly record struct AgeCutItem(int Id, float[] Stored768, float[] Query768)
```

Gate corpus pairs one stored and one query vector per item id.

## src/Recall.Core/Age/AgeCutCorpus.cs

```csharp
namespace Recall.Core.Age;
public static class AgeCutCorpus
public static bool TryLoad(Modality modality, out IReadOnlyList<AgeCutItem> items)
```

`if (modality == Modality.Text)` throws `RecallFault.AgeCutTextForbidden`. Production returns an empty list and false without reading disk.

```csharp
if (TryBuildItems is not null)
if (built is null || built.Count == 0)
if (visionEncoder is null || audioEncoder is null || videoSampler is null || audioSource is null || !Directory.Exists(DemoRoot))
if (paths.Count == 0)
if (ingestSession is not null && MediaContextPolicy.OneContext)
if (vector is null || vector.Length != MediaCollections.Width)
if (leasedVision)
if (!Directory.Exists(folder))
catch (IOException)
catch (UnauthorizedAccessException)
if (frames.Count == 0)
if (windows.Count == 0)
```

## src/Recall.Core/Age/FailClosedAgeCutDecision.cs

```csharp
namespace Recall.Core.Age;
public sealed class FailClosedAgeCutDecision : IAgeCutDecision
public bool TryPass(Modality modality)
```

`if (modality == Modality.Text)` throws. `if (!AgeCutCorpus.TryLoad(modality, out var items))` returns false. Otherwise delegates to `AgeCutGate.Passes`.

## src/Recall.Core/Age/AgeMover.cs

```csharp
namespace Recall.Core.Age;
public readonly record struct AgeStage(...)
public static class AgeMover
public static void Stage(IMediaVectorIndex index, IIngestCursorStore cursors, AgeStage stage)
public static void Finish(IMediaVectorIndex index, IIngestCursorStore cursors, string longId)
```

`if (stage.Vector768.Length != MediaCollections.Width)` throws `RecallFault.EmbeddingWidthUnexpected`. Stage cuts to 512, inserts the short row, and `CommitAgeCut`. Finish deletes the long id and `CompleteAgeCut`. `if (!index.TryDelete(longId))` leaves the long row when delete fails.

## src/Recall.Core/Age/MediaWords.cs

```csharp
namespace Recall.Core.Age;
public static class MediaWords
public static string Join(string? ocr, string? transcript, string? keywords)
```

`if (!string.IsNullOrWhiteSpace(ocr))` `if (!string.IsNullOrWhiteSpace(transcript))` `if (!string.IsNullOrWhiteSpace(keywords))` skip blank parts and join the rest with newline in OCR, transcript, keywords order.

## src/Recall.Core/Age/DiskMediaFileFacts.cs

```csharp
namespace Recall.Core.Age;
public sealed class DiskMediaFileFacts : Ports.IMediaFileFacts
public bool Exists(string path)
public DateTimeOffset? LastWriteUtc(string path)
```

`catch (IOException)` and `catch (UnauthorizedAccessException)` return false or null. `if (!File.Exists(path))` returns null for last-write.

## src/Recall.Core/Ports/IMediaFileFacts.cs

```csharp
namespace Recall.Core.Ports;
public interface IMediaFileFacts
    bool Exists(string path);
    DateTimeOffset? LastWriteUtc(string path);
```

Disk paths for age move and pin; missing or inaccessible files read as absent.

## src/Recall.Core/Ports/IEgressProbe.cs

```csharp
namespace Recall.Core.Ports;
public interface IEgressProbe
    Task<bool> HasNonLocalByteAsync(CancellationToken cancellationToken);
```

No branch.

## src/Recall.Core/Privacy/ProxyLogEgressProbe.cs

```csharp
namespace Recall.Core.Privacy;
public sealed class ProxyLogEgressProbe : IEgressProbe
    public const string DownloadFinishedMarker = "download-finished";
    public static readonly TimeSpan WatchWindow = TimeSpan.FromMinutes(60);
    public ProxyLogEgressProbe(string? logPath)
    public async Task<bool> HasNonLocalByteAsync(CancellationToken cancellationToken)
        if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            if (string.IsNullOrWhiteSpace(line))
            if (parts.Length != 3)
            if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp))
            if (parts[1].Length == 0)
            if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var byteCount))
            if (byteCount < 0)
        if (markerLines.Count == 0)
        if (markerLines.Count > 1)
        if (marker.ByteCount != 0)
        if (end - start < 60_000)
            if (record.Timestamp < start)
            if (string.Equals(record.Address, DownloadFinishedMarker, StringComparison.Ordinal))
            if (IsLocalAddress(record.Address))
            if (record.ByteCount > 0)
```
