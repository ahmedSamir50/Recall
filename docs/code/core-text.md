## src/Recall.Core/Text/TextIngestEngine.cs

```csharp
namespace Recall.Core.Text;
public sealed class TextIngestEngine
    public TextIngestEngine(
        ITextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        IIndexPolicy indexPolicy,
        ITextTokenCounter tokenCounter,
        LocalOpLogWriter opLog)
    public async Task IngestFileAsync(string filePath, string rawText, DateTimeOffset modifiedAt, CancellationToken cancellationToken)
            if (!dense)
            if (cursors.AnyLivePath(ContentIdBuilder.Build(Modality.Text, sourceHash, StoredWidth.W512, chunk.Ordinal)))
            if (index.TryInsert(insert))
```



```csharp
TextIngestChunk
TextIngestResult
``` 
## src/Recall.Core/Text/TextChunkPlanner.cs

```csharp
namespace Recall.Core.Text;
public static class TextChunkPlanner
public readonly record struct TextChunkPiece(string PrefixedText, string RawText, int Ordinal);
    public static IReadOnlyList<TextChunkPiece> PlanChunks(
        string title,
        string content,
        Func<string, int> countTokens)
        if (countTokens(whole) <= MaxTokenBudget)
            if (end <= start)
            if (countTokens(wrapped) <= MaxTokenBudget)
        if (index > 0 && index < text.Length && char.IsLowSurrogate(text[index]))
        if (start >= text.Length)
        if (end < text.Length && char.IsHighSurrogate(text[start]) && char.IsLowSurrogate(text[end]))
```


## src/Recall.Core/Text/EmbeddingTaskPrefixes.cs

```csharp
namespace Recall.Core.Text;
public static class EmbeddingTaskPrefixes
    public static string WrapSearchQuery(string query)
    public static string WrapTitledDocument(string title, string content)
        if (query.StartsWith(SearchTask, StringComparison.Ordinal))
            throw new RecallFaultException(RecallFault.SearchPrefixAppliedTwice);
        if (content.StartsWith(TitleMarker, StringComparison.Ordinal) && content.Contains(TextJoiner, StringComparison.Ordinal))
            throw new RecallFaultException(RecallFault.DocumentPrefixAppliedTwice);
        if (safeTitle.Length == 0)
```


## src/Recall.Core/Text/ContentIdBuilder.cs

```csharp
namespace Recall.Core.Text;
public static class ContentIdBuilder
    public static string Build(Modality modality, string sourceContentHash, StoredWidth width, int chunkOrdinal)
    public static string HashSourceContent(string rawText)
        if (chunkOrdinal < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkOrdinal));
```


## src/Recall.Core/Text/GrantTextFiles.cs

```csharp
namespace Recall.Core.Text;
public static class GrantTextFiles
    public static void Use(TextIngestOptions options)
    public static bool IsTextFile(string path)
```

No branch.


## src/Recall.Core/Text/TextFileSignature.cs

```csharp
namespace Recall.Core.Text;
public static class TextFileSignature
    public static bool IsSafeText(ReadOnlySpan<byte> sample)
    public static async Task<bool> FileIsSafeTextAsync(string path, CancellationToken cancellationToken)
        if (sample.IsEmpty || HasBlockedMagic(sample))
        if (HasUtf16Bom(sample))
        if (start.Length >= 3 && start[0] == 0xEF && start[1] == 0xBB && start[2] == 0xBF)
        if (StartsWithAscii(start, "#!") || StartsWithAscii(start, "#requires") || StartsWithAscii(start, "<#"))
            if (value == 0)
            if (value < 9 || (value > 13 && value < 32))
        if (sample.Length < ascii.Length)
            if (sample[i] != ascii[i])
        if (sample.Length >= 2 && sample[0] == (byte)'M' && sample[1] == (byte)'Z')
        if (sample.Length >= 4 && sample[0] == 0x7F && sample[1] == (byte)'E' && sample[2] == (byte)'L' && sample[3] == (byte)'F')
        if (sample.Length >= 4 && IsMachO(sample))
        if (sample.Length >= 4 && sample[0] == 0xCA && sample[1] == 0xFE && sample[2] == 0xBA && sample[3] == 0xBE)
        if (sample.Length >= 4 && sample[0] == (byte)'P' && sample[1] == (byte)'K' && sample[2] == 3 && sample[3] == 4)
        if (sample.Length >= 4 && sample[0] == 0 && sample[1] == (byte)'a' && sample[2] == (byte)'s' && sample[3] == (byte)'m')
```


## src/Recall.Core/Text/TextIngestOptions.cs

```csharp
namespace Recall.Core.Text;
public sealed class TextIngestOptions
    public string[] ExtensionList()
```

No branch.


## src/Recall.Core/Ingest/GrantedTextIngestService.cs

```csharp
namespace Recall.Core.Ingest;
public sealed class GrantedTextIngestService : IGrantedTextIngestService
    public GrantedTextIngestService(IScopeManagerService scope, IGrantedTextReader reader, TextIngestEngine engine)
    public async Task IngestGrantedTextAsync(CancellationToken cancellationToken = default)
            if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Text) && !grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
```



```csharp
if (grant is null)
if (EncoderRepair.NeedsRepair(row?.EncoderHash, encoderHash))
if (cursors.AnyOtherLivePath(chunk.ContentId, document.Path))
if (index.TryDelete(chunk.ContentId))
else if (IngestCursorPolicy.Decide(row, contentHash) == IngestCursorDecision.Skip)
if (cursors.AnyLivePath(retiredId))
if (index.TryDelete(retiredId))
``` 
## src/Recall.Core/Ingest/GrantedTextDocument.cs

```csharp
namespace Recall.Core.Ingest;
public sealed record GrantedTextDocument(string Path, string FileName, string Text, DateTimeOffset ModifiedAt);
```

No branch.


## src/Recall.Core/Ingest/DirectoryGrantedTextReader.cs

```csharp
namespace Recall.Core.Ingest;
public sealed class DirectoryGrantedTextReader : IGrantedTextReader
    public DirectoryGrantedTextReader(
        IUserSkipStore skips,
        ILocalContentProbe probe,
        IRemoteFileSkipReport remoteSkip,
        IDatasetSkipReport datasetSkip)
    public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(
        ScopeFolderGrant grant,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        if (!Directory.Exists(grant.Path))
        if (Directory.Exists(entry))
        if (FileAdmission.IsBlockedFolder(name, user))
        if (IsDirectoryIgnored(relativeDir, shared))
        if (DatasetFolderSkip.ShouldSkip(entry, isGrantRoot, directChildNames))
        if (GitIgnoreMatcher.IsIgnored(relativeDir, shared))
        if (GitIgnoreMatcher.LastDecision(relativeDir, true, shared) == GitIgnoreDecision.Ignore
        if (FileAdmission.Decide(name, kinds, user, gitIgnored) != FileAdmissionResult.Read)
        if (!LocalContentPolicy.MayReadBytes(probe.Probe(entry)))
        if (!safe)
        if (added > 0)
        if (relative == ".")
        if (File.Exists(ignorePath))
        if (File.Exists(excludePath))
        if (string.IsNullOrEmpty(relativeDir) || relativeDir == ".")
```


## src/Recall.Core/Ingest/IGrantedTextReader.cs

```csharp
namespace Recall.Core.Ingest;
public interface IGrantedTextReader
    IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, CancellationToken cancellationToken);
```

No branch.


## src/Recall.Core/Ingest/FilesystemScopeFileReader.cs

```csharp
namespace Recall.Core.Ingest;
public type FilesystemScopeFileReader
FilesystemScopeFileReader
public static class FilesystemScopeFileReader
public static async Task<GrantedTextDocument?> TryReadAsync(
    if (string.IsNullOrWhiteSpace(path))
    if (!File.Exists(path))
    if (!string.IsNullOrWhiteSpace(directory))
    if (!string.IsNullOrWhiteSpace(relativeDir) && relativeDir != ".")
    if (segments.Any(segment => FileAdmission.IsBlockedFolder(segment, user)))
    if (!string.IsNullOrWhiteSpace(folderName) && FileAdmission.IsBlockedFolder(folderName, user))
    if (!string.IsNullOrWhiteSpace(relativeCheck))
    if (ignored)
    if (FileAdmission.Decide(fileName, grant.EnabledKinds, user, ignored) != FileAdmissionResult.Read)
    if (!LocalContentPolicy.MayReadBytes(contentState))
    if (!safe)
    catch (Exception ex)
``` 



## src/Recall.Core/Ingest/ForegroundScanService.cs

```csharp
namespace Recall.Core.Ingest;
public type ForegroundScanService
ForegroundScanService
public sealed class ForegroundScanService
public ForegroundScanService(
public async Task ScanAsync(CancellationToken cancellationToken = default)
    if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Text) && !grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
    if (change.Kind == ScopeChangeKind.Created || change.Kind == ScopeChangeKind.Modified || change.Kind == ScopeChangeKind.Renamed)
``` 



## src/Recall.Core/Ingest/IndexNowOfferPolicy.cs

```csharp
namespace Recall.Core.Ingest;
public type IndexNowOfferPolicy
IndexNowOfferPolicy
public static class IndexNowOfferPolicy
public static IReadOnlyList<ScopeFolderGrant> ListOffers(
    if (grants is null || grants.Count == 0)
    if (ReferenceEquals(grant, other))
    if (ScopeGrantPolicy.IsChildPath(ScopeGrantPolicy.Normalize(other.Path), grant.Path))
    if (isChild)
    if (finishedPaths is not null && finishedPaths.Contains(grant.Path))
``` 



## src/Recall.Core/Ingest/IndexNowService.cs

```csharp
namespace Recall.Core.Ingest;
public type IndexNowService
IndexNowService
public sealed class IndexNowService : IIndexNowService
public IndexNowService(IScopeManagerService scope, IIngestCursorStore cursors, IGrantedTextIngestService ingest)
public IReadOnlyList<ScopeFolderGrant> ListOffers()
public async Task IndexAsync(string path, CancellationToken cancellationToken = default)
    if (grant is null || string.IsNullOrWhiteSpace(grant.Path))
    if (cursors.IsGrantFinished(grant.Path) || cursors.IsGrantFinished(normalized))
    if (string.IsNullOrWhiteSpace(path))
``` 
## src/Recall.Core/Ingest/ScopeChangeApplier.cs

```csharp
namespace Recall.Core.Ingest;
public type ScopeChangeApplier
ScopeChangeApplier
public sealed class ScopeChangeApplier
public ScopeChangeApplier(IIngestCursorStore cursors, IGrantedTextIngestService ingest, LocalOpLogWriter opLog)
public async Task ApplyAsync(
    if (string.IsNullOrWhiteSpace(change.Path))
    if (change.Kind == ScopeChangeKind.Deleted)
    if (row is not null && row.State == IngestCursorState.Missing)
    if (change.Kind == ScopeChangeKind.Renamed)
    if (string.IsNullOrWhiteSpace(previous))
    if (grant is not null && document is not null)
    if (previousRow is null)
    if (document is null)
    if (string.Equals(previousRow.ContentHash, nextHash, StringComparison.Ordinal))
    if (grant is not null)
    if (change.Kind == ScopeChangeKind.Created || change.Kind == ScopeChangeKind.Modified)
    if (grant is null)
``` 



## src/Recall.Core/Ingest/ScopeWatchCoordinator.cs

```csharp
namespace Recall.Core.Ingest;
public type ScopeWatchCoordinator
ScopeWatchCoordinator
public sealed class ScopeWatchCoordinator : IScopeChangeSink, IDisposable
public ScopeWatchCoordinator(
public ScopeChangeBuffer Buffer => buffer;
public bool IsRunning => loopTask is not null && !loopTask.IsCompleted;
public void Report(ScopeChange change)
public void RequestRescan()
public void OnCreated(RecallShellKind kind)
public void OnResumed(RecallShellKind kind)
public void Stop()
public async Task PumpOnceAsync(CancellationToken cancellationToken = default)
public void Dispose()
    if (kind != RecallShellKind.Windows && kind != RecallShellKind.Android && kind != RecallShellKind.MacCatalyst && kind != RecallShellKind.Ios)
    if (ScopeWatcherKindPolicy.Choose(kind) == ScopeWatcherKind.WindowsFileSystem)
    if (!IsRunning)
    if (wake.CurrentCount == 0)
    catch (Exception ex)
    if (buffer.NeedsRescan)
    if (!IdleIngestGate.ShouldRun(idle.Current))
    catch (OperationCanceledException)
    if (grant is null && change.Kind == ScopeChangeKind.Renamed && !string.IsNullOrWhiteSpace(change.PreviousPath))
    if (grant is not null && !string.IsNullOrWhiteSpace(change.Path))
    if (string.IsNullOrWhiteSpace(path))
    if (grant is null || string.IsNullOrWhiteSpace(grant.Path))
    if (ContentUri.IsContent(grant.Path))
    if (string.Equals(grant.Path, path, StringComparison.OrdinalIgnoreCase))
    if (ScopeGrantPolicy.IsInsideGrant(grant.Path, path))
    if (IsRunning)
    if (disposed)
```


## src/Recall.Core/Ingest/DisabledIdleDetectorService.cs

```csharp
namespace Recall.Core.Ingest;
public type DisabledIdleDetectorService
DisabledIdleDetectorService
public IdleSignal Current => IdleSignal.Idle;
No branch.
``` 

## src/Recall.Core/Ingest/IdleIngestGate.cs

```csharp
namespace Recall.Core.Ingest;
public type IdleIngestGate
IdleIngestGate
public const int DesktopIdleAfterSeconds = 30;
public static bool ShouldRun(IdleSignal signal) => signal == IdleSignal.Idle;
public static IdleSignal FromLastInput(TimeSpan idleFor) =>
public static IdleSignal ForForeground(bool applicationActive) =>
No branch.
``` 

## src/Recall.Core/Ingest/LocalOnlyContentProbe.cs

```csharp
namespace Recall.Core.Ingest;
public type LocalOnlyContentProbe
LocalOnlyContentProbe
public LocalContentState Probe(string path) => LocalContentState.Local;
No branch.
``` 

## src/Recall.Core/Text/EncoderRepair.cs

```csharp
namespace Recall.Core.Text;
public type EncoderRepair
EncoderRepair
public static bool NeedsRepair(string? storedHash, string? currentHash) =>
No branch.
``` 

## src/Recall.Core/Text/KeywordFold.cs

```csharp
namespace Recall.Core.Text;
public type KeywordFold
KeywordFold
public static string Fold(string raw)
public static IReadOnlyList<string> Tokens(string folded)
if (rune.Value is 0x0640 or (>= 0x064B and <= 0x065F) or 0x0670)
if (rune.Value is 0x0622 or 0x0623 or 0x0625 or 0x0671)
if (IsHan(rune))
if (Rune.IsLetter(rune))
if (buffer.Length == 0)
``` 

## src/Recall.Core/Text/NumericSpanPolicy.cs

```csharp
namespace Recall.Core.Text;
public type NumericSpanPolicy
NumericSpanPolicy
public static bool IsDense(string raw, bool codeFile)
if (codeFile)
if (Rune.IsWhiteSpace(rune))
if (Rune.IsLetter(rune))
``` 

## src/Recall.Core/Text/WidthOnEdit.cs

```csharp
namespace Recall.Core.Text;
public type WidthOnEdit
WidthOnEdit
public static StoredWidth Choose(Modality modality, StoredWidth current, bool pinned, bool pastCutoff)
if (modality == Modality.Text)
if (current == StoredWidth.W512 && !pinned)
if (current == StoredWidth.W512 && pinned)
if (current == StoredWidth.W768 && !pastCutoff)
``` 

## src/Recall.Core/Ingest/FrameCadence.cs

```csharp
namespace Recall.Core.Ingest;
public static class FrameCadence
public const double PeriodSeconds = 2;
public static IReadOnlyList<double> Starts(double durationSeconds)
public static double End(double startSeconds, double durationSeconds)
if (durationSeconds <= 0)
```

A two-second cadence yields starts at 0, 2, 4, and so on while the start is below the duration. Zero or negative duration yields no starts. The end is clamped to the duration. No OCR or audio path is involved.

## src/Recall.Core/Text/PdfSlicePolicy.cs

```csharp
namespace Recall.Core.Text;
public static class PdfSlicePolicy
public const int TokenCap = 8192;
public const int PageCap = 10;
public static bool EndsSlice(int tokens, int pages) => tokens >= TokenCap || pages >= PageCap;
```

Both caps are provisional. A slice ends when either the token cap or the page cap is reached. No branch.

## src/Recall.Core/Ocr/OcrModelFiles.cs

```csharp
namespace Recall.Core.Ocr;
public static class OcrModelFiles
public static class Detector
public static class EnglishReader
public static class ArabicReader
public const string LocalName = "ppocrv6-small-det.onnx";
public const string LocalName = "ppocrv6-small-rec.onnx";
public const string LocalName = "arabic-ppocrv5-mobile-rec.onnx";
```

The detector and English reader are PP-OCRv6 small. Arabic is the PP-OCRv5 mobile recognizer. Each `Url` is a pinned Hugging Face resolve base and carries an `S1075` suppression. `LocalNames` is exactly these three files; there is no fourth model. No branch.

## src/Recall.Core/Ocr/OcrReaderGate.cs

```csharp
namespace Recall.Core.Ocr;
public static class OcrReaderGate
public static bool CanLoad(string? sha256) => !string.IsNullOrWhiteSpace(sha256);
if (string.IsNullOrWhiteSpace(sha256))
```

A blank SHA-256 returns false. Any other value loads only when it equals the detector, English reader, or Arabic reader hash. The gate never constructs an inference session.

## src/Recall.Core/Ocr/OcrSessionSet.cs

```csharp
namespace Recall.Core.Ocr;
public sealed class OcrSessionSet : IDisposable
public bool DetectorLoaded { get; private set; }
public bool EnglishReaderLoaded { get; private set; }
public bool ArabicReaderLoaded { get; private set; }
public void Load()
public void Dispose()
```

`Load` marks the detector, English reader, and Arabic reader as loaded. `Dispose` clears all three. No branch.

## src/Recall.Core/Ocr/OcrLineChoice.cs

```csharp
namespace Recall.Core.Ocr;
public static class OcrLineChoice
public static string Choose(string? englishText, double englishScore, string? arabicText, double arabicScore)
if (english.Length == 0 && arabic.Length == 0)
if (englishText is null)
if (arabicText is null)
if (arabicScore > englishScore + 1e-6)
```

Null text becomes an empty string. Two empty lines return empty. One empty line returns the other reader's text. Otherwise Arabic wins only when its score exceeds English by more than `1e-6`; a tie keeps English.

## src/Recall.Core/Ocr/BrokenArabic.cs

```csharp
namespace Recall.Core.Ocr;
public static class BrokenArabic
public static bool NeedsOcr(string? word)
if (string.IsNullOrEmpty(word))
if (rune.Value == Replacement)
if (run >= ReplacementRun)
if (hasArabicLetter && hasReplacementRun)
if (IsArabicLetter(rune))
```

A null or empty word is not broken. A run of three or more U+FFFD replacement characters triggers OCR only after an Arabic letter has already appeared. Arabic is tested with `Rune.IsLetter` plus `UnicodeRanges.Arabic`; `Rune.GetUnicodeScript` is not used. A run shorter than three does not trigger.

