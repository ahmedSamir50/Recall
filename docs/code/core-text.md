## src/Recall.Core/Text/TextIngestEngine.cs

```csharp
namespace Recall.Core.Text;
public sealed class TextIngestEngine
    public TextIngestEngine(
        ITextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        IIndexPolicy indexPolicy,
        ITextTokenCounter tokenCounter,
        LocalOpLogWriter opLog,
        IIngestCursorStore cursors)
    public async Task<TextIngestResult> IngestFileAsync(string filePath, string rawText, DateTimeOffset modifiedAt, string grantPath, string encoderHash, bool codeFile, CancellationToken cancellationToken)
        var prefixTokenCount = tokenCounter.CountTokens(EmbeddingTaskPrefixes.TitledPrefix(title));
        var ends = tokenCounter.TokenEnds(rawText);
        var patches = DocumentPatchPlanner.Plan(title, documentText, pdfPages, prefixTokenCount, ends);
            if (!NumericSpanPolicy.IsDense(patch.Raw, codeFile))
            if (cursors.AnyLivePath(contentId))
            if (index.TryInsert(insert))
```

One tokenize per document. For a `.pdf` path the engine reads with `PdfPigPageSource`, runs `SplitPdfPages`, and passes `EmbeddablePages` into `DocumentPatchPlanner` so `page_start` and `page_end` are set. `OcrPageNumbers` on the result are written through `OcrPageWaitlist`. For other paths the joined document is the raw text and page spans stay zero. The engine counts the title prefix once, asks the token counter for the document's token ends once, and lets `DocumentPatchPlanner` place the cuts. Every patch keeps the title prefix through `EmbeddingTaskPrefixes.WrapTitledDocument` and carries its page and char spans into the chunk rows. A patch that is not dense keeps its row without a vector.

```csharp
TextIngestChunk
TextIngestResult
``` 
## src/Recall.Core/Text/DocumentPatchPlanner.cs

```csharp
namespace Recall.Core.Text;
public readonly record struct PdfPageText(int PageNumber, string Text);
public readonly record struct PdfPatch(string Raw, int Ordinal, int PageStart, int PageEnd, int CharStart, int CharEnd);
public readonly record struct PdfPageSplit(string JoinedDocument, IReadOnlyList<PdfPageText> EmbeddablePages, IReadOnlyList<int> OcrPageNumbers);
public static class DocumentPatchPlanner
    public const int PatchTargetTokens = 2048;
    public const int LookbackTokens = 512;
    public const int OverlapTokens = 256;
    public const int MaxOverlapTokens = 384;
    public const int HardCapTokens = 7680;
    public static bool EndsPatch(int prefixedTokenCount)
    public static int OpenPage(int? page)
    public static bool IsOcrPage(string pageText)
    public static PdfPageSplit SplitPdfPages(IReadOnlyList<PdfPageText> pages)
        if (IsOcrPage(page.Text))
        if (embeddable.Count == 0)
    public static IReadOnlyList<PdfPatch> Plan(string title, string joinedDocument, IReadOnlyList<PdfPageText>? pages, int prefixTokenCount, IReadOnlyList<int>? tokenEnds)
        if (endCount == 0 || startToken + aim >= endCount)
        if (boundary < 0)
        if (cls > bestClass)
        if (cls == ParagraphClass)
        if (IsSentenceBoundary(document, position))
        if (whitespace < 0 && position < document.Length && char.IsWhiteSpace(document[position]))
        if (position < 1 || position >= document.Length)
        if (position >= 2 && document[position - 1] == '\n' && document[position - 2] == '\n')
        if (document[position - 1] == '\f')
        if (document[position - 1] == '\n' && IsHeadingLine(document, position))
        if (document[position - 1] == '\n')
        if (document[position - 1] == ArabicSemicolon)
        if (document[position - 1] == ArabicComma)
        if (char.IsWhiteSpace(document[position]))
        if (position == 0 || position >= document.Length)
        if (!char.IsWhiteSpace(document[position]))
        if (mark is not ('.' or '!' or '?' or '\u061F' or '\u2026'))
        if (IsAbbreviation(document, position - 1))
        if (next >= document.Length)
        if (document[position - 1] != '\n')
        if (ends[mid] <= position)
        if (token < low || token > high)
        if (token > bestToken)
        if (index + 1 >= document.Length || !IsSentenceBoundary(document, index + 1))
        if (pages is null || pages.Count == 0)
```

One planner covers notes and PDFs. The target is 2048 body tokens, the lookback is 512, and 7680 is only the ceiling. From the aim, the planner searches back to token `2048 - 512`, takes the latest boundary of the highest class, and snaps to the nearest token end at or before it. Classes, highest first: a paragraph break `\n\n`, a `\f` page break, a heading line of one to six `#` and a space, a sentence end `.`, `!`, `?`, `؟`, or `…` followed by whitespace when the word is not `e.g.`, `i.e.`, `etc.`, `vs.`, `Dr.`, `Mr.`, or `No.` and the next non-space is an ASCII capital or an Arabic letter, a line break `\n`, `؛`, `،`, and any whitespace. `3.5` is not an end. The next patch starts at a sentence start 256 to 384 tokens back, closest to 256; without one it starts at the cut. Only when one paragraph runs past 7680 does the planner cut that paragraph at a sentence end, then whitespace, still on a token end. A blank document throws `ArgumentException`, null token ends throw `ArgumentNullException`, and a negative prefix count throws `ArgumentOutOfRangeException`. A page whose words trip `BrokenArabic.NeedsOcr` is not patched. `SplitPdfPages` joins the remaining pages with `\f`, records their numbers in `OcrPageNumbers`, and leaves a `NeedsOcr` page out of the join with no extra form feed.

## src/Recall.Core/Text/ChunkerStamp.cs

```csharp
namespace Recall.Core.Text;
public static class ChunkerStamp
    public const string Suffix = ":patch-v3";
```

No branch. The stored and filtered text encoder hash is the file SHA-256 plus this suffix. `:patch-v2` was the earlier non-boundary cutter and is never written.

## src/Recall.Core/Text/PdfPigPageSource.cs

```csharp
namespace Recall.Core.Text;
public static class PdfPigPageSource
    public static IReadOnlyList<PdfPageText> Read(Stream stream)
        catch (Exception ex) when (ex is not OperationCanceledException)
```

`Read` opens `UglyToad.PdfPig.PdfDocument`, enumerates `GetPages()` one page at a time, and passes `page.Number` and `PdfTextOrder.Logical(page)` into the page list. It never calls `page.Text`. A page with no Arabic letter returns `ContentOrderTextExtractor.GetText` with `addDoubleNewline: true`, normalized to `\n`. A page that contains Arabic uses `page.GetWords()` so positioning gaps keep spaces between words. Presentation-form runs whose x increases are reversed and NFKC-normalized. A mixed line orders runs by horizontal position. When RTL glyphs sit to the right of every LTR glyph, or the line sits in the right half of the page, runs sort right-to-left; otherwise they sort left-to-right. A null stream throws `ArgumentNullException`. A PDF that PdfPig cannot open, including an encrypted one, throws `InvalidDataException` with message `PDF text could not be read.`

## src/Recall.Core/Text/PdfTextOrder.cs

```csharp
namespace Recall.Core.Text;
internal static class PdfTextOrder
    public static string Logical(Page page)
```

`Logical` is the page text `PdfPigPageSource.Read` stores. A null page throws `ArgumentNullException`. A page with no Arabic letter returns normalized `GetText(page, addDoubleNewline: true)`. An Arabic page walks `GetWords()`, keeps inter-word spaces, reverses presentation-form words when x increases, and reverses mixed run order when RTL words sit right of LTR words on the line.


## src/Recall.Core/Text/EmbeddingTaskPrefixes.cs

```csharp
namespace Recall.Core.Text;
public static class EmbeddingTaskPrefixes
    public static string TitledPrefix(string title)
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
if (!textOrCode && !media)
if (textOrCode)
if (media && audio is not null)
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
        if (!MediaFileKinds.IsAudio(name) && !MediaFileKinds.IsVideo(name))
        if (gitIgnored)
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
    if (grant is not null && audio is not null
    if (scheduler is not null)
    if (audio is not null && (MediaFileKinds.IsAudio(change.Path) || MediaFileKinds.IsVideo(change.Path)))
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
    if (ingestScheduler is not null)
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

## src/Recall.Core/Ingest/IngestScheduler.cs

```csharp
namespace Recall.Core.Ingest;
public enum IngestModel
public readonly record struct IngestJob(IngestModel Model, string Path);
public sealed class IngestScheduler
public void Enqueue(IngestJob job)
public bool ShouldOpen(IngestModel model, DateTimeOffset now)
        if (queue.Count == 0)
        if (queue.Count >= OpenFileThreshold)
        if (queue.Count == 0)
```

Three queues: text, media, and OCR. A pass opens when its queue holds 20 files or 30 seconds have passed since the last enqueue for that model. The watch loop waits `IngestScheduler.OpenIdleDelay` so a quiet queue opens without another watcher event. Cancellation stops the pass and leaves the rest queued. `OpenText` runs inside the pass try, and `CloseText` runs in `finally` only after that open succeeded.

## src/Recall.Core/Ingest/IngestSchedulerRunner.cs

```csharp
namespace Recall.Core.Ingest;
public sealed class IngestPassHooks
public sealed class IngestSchedulerRunner
        if (!scheduler.ShouldOpen(IngestModel.Text, now))
                if (hooks.ProcessTextJobAsync is not null)
        if (!scheduler.ShouldOpen(IngestModel.Media, now))
        if (jobs.Count == 0)
            if (MediaFileKinds.IsAudio(path))
            if (oneContext)
                    if (hooks.ProcessMediaJobAsync is not null)
                if (visionJobs.Count > 0)
                        if (hooks.ProcessMediaJobAsync is not null)
                if (audioJobs.Count > 0)
                        if (hooks.ProcessMediaJobAsync is not null)
            if (visionOpen)
        if (!scheduler.ShouldOpen(IngestModel.Ocr, now))
        if (jobs.Count == 0)
```

Pass order is text, media, then OCR. Media opens vision and audio once per pass, or one shared vision context when `MediaContextPolicy.OneContext` is true. OCR pass 3 calls `OcrPassStub.Run` once for the drained paths.

## src/Recall.Core/Ingest/IngestSchedulerService.cs

```csharp
namespace Recall.Core.Ingest;
public sealed class IngestSchedulerService
public Task RunReadyPassesAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
```

Production hooks dispatch text and media jobs through `ScheduledIngestDispatcher`. OCR hooks are not wired in production until Epic 6.3.

## src/Recall.Core/Ingest/MediaContextPolicy.cs

```csharp
namespace Recall.Core.Ingest;
public static class MediaContextPolicy
public static bool OneContext { get; set; }
```

Set once by `MediaContextProbe` when the local mmproj can embed vision and audio on one open mtmd context.

## src/Recall.Core/Ingest/ScheduledIngestDispatcher.cs

```csharp
namespace Recall.Core.Ingest;
public sealed class ScheduledIngestDispatcher
        if (grant is null)
        if (document is null)
        if (grant is null || audio is null)
        if (MediaFileKinds.IsAudio(path) || MediaFileKinds.IsVideo(path))
            if (grant is null || string.IsNullOrWhiteSpace(grant.Path))
            if (ContentUri.IsContent(grant.Path))
                if (string.Equals(grant.Path, path, StringComparison.OrdinalIgnoreCase))
            if (ScopeGrantPolicy.IsInsideGrant(grant.Path, path))
```

Resolves the grant for a queued path and calls text ingest or `IAudioFileIngest.IndexAsync` for audio and video paths.

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

## src/Recall.Core/Ocr/OcrPageWaitlist.cs

```csharp
namespace Recall.Core.Ocr;
public static class OcrPageWaitlist
public static void Apply(string path, IReadOnlyList<int> pageNumbers, IIngestCursorStore cursors, IngestScheduler? scheduler = null)
if (pageNumbers.Count > 0 && scheduler is not null)
```

`Apply` writes the page numbers into `ocr_page` through `ReplaceOcrPages` and enqueues one `IngestModel.Ocr` job when the list is not empty. Real ONNX OCR is Epic 6.3; pass 3 today runs `OcrPassStub` only. Audio ingest does not wait on this queue.

## src/Recall.Core/Ocr/OcrPassStub.cs

```csharp
namespace Recall.Core.Ocr;
public static class OcrPassStub
public static void Run(Action processJobs)
```

Counts one OCR open per scheduler pass. It does not construct an ONNX session. `Scheduler_SwitchesModelOncePerPass` locks the open count.

## src/Recall.Core/Ingest/WavPcm.cs

```csharp
namespace Recall.Core.Ingest;
public static class WavPcm
public static bool IsWav(ReadOnlyMemory<byte> file)
if (file.Length < 12)
public static float[] Read(ReadOnlyMemory<byte> file)
if (data.Length < 12)
if (!data[..4].SequenceEqual(Riff) || !data[8..12].SequenceEqual(Wave))
if (!TryFindChunk(data, DataId, out var payload))
if (channels == 2)
if (size < 0)
if (chunkId.SequenceEqual(id))
if (fmt.Length < 16)
if ((format != PcmTag && format != ExtensibleTag)
if (format == ExtensibleTag)
if (fmt.Length < 40)
if (cbSize < 22 || !fmt[24..40].SequenceEqual(PcmSubFormat))
```

RIFF/WAVE walk from offset 12 with fmt validation for tag 1 and the 0xFFFE PCM subformat, stereo to mono averaging, and scaled floats that continue through the 16 kHz resampler.

## src/Recall.Core/Ingest/AudioResample.cs

```csharp
namespace Recall.Core.Ingest;
public static class AudioResample
public static float[] To16k(float[] source, int sourceRate)
if (source.Length == 0)
if (sourceRate <= 0)
if (sourceRate == AudioWindows.SampleRate)
```

Linear blend resampling to the 16 kHz audio window rate with long integer position math, a same-rate pass-through, and range guards.

## src/Recall.Core/Ingest/AudioWindows.cs

```csharp
namespace Recall.Core.Ingest;
public readonly record struct AudioWindow(int StartMilliseconds, int EndMilliseconds);
public static class AudioWindows
public const int NativeLengthMilliseconds = 11200;
public const int SampleRate = 16000;
public static int HopMilliseconds(int nativeLengthMilliseconds)
if (nativeLengthMilliseconds < 2)
public static IReadOnlyList<AudioWindow> Plan(int durationMilliseconds)
if (durationMilliseconds <= 0)
if (durationMilliseconds <= NativeLengthMilliseconds)
if (durationMilliseconds - start >= NativeLengthMilliseconds)
if (windows.Count > 0)
public static string Seconds(int milliseconds) =>
```

Windows of 11200 ms native length, a 5600 ms hop that is half the native length, a short-tail extension of the last window, and the invariant-culture seconds text.

## src/Recall.Core/Ingest/SiblingTranscript.cs

```csharp
namespace Recall.Core.Ingest;
public static class SiblingTranscript
public const bool ReadsEmbeddedTrack = false;
public static bool IsCaptionFile(string fileName)
if (string.IsNullOrEmpty(fileName))
public static bool Matches(string mediaFileName, string candidateFileName)
if (string.IsNullOrEmpty(mediaFileName) || string.IsNullOrEmpty(candidateFileName))
if (!IsCaptionFile(candidateFileName))
if (string.Equals(candidateStem, mediaStem, StringComparison.OrdinalIgnoreCase))
if (tagDot < 0)
if (!string.Equals(baseStem, mediaStem, StringComparison.OrdinalIgnoreCase))
public static IReadOnlyList<string> Order(IReadOnlyList<string> fileNames)
if (fileNames is null)
public static string Join(IReadOnlyList<string> orderedTexts)
if (orderedTexts is null)
public static string ExtractCues(string extension, string raw)
if (raw is null)
if (trimmed.Length == 0 || IsAllDigits(trimmed) || trimmed.Contains("-->", StringComparison.Ordinal))
if (line.TrimStart().StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase))
if (line.TrimStart().StartsWith("NOTE", StringComparison.OrdinalIgnoreCase)
if (skipBlock)
if (line.Trim().Length == 0)
if (line.Trim().Length == 0 || line.Contains("-->", StringComparison.Ordinal))
if (line.Trim().Length == 0 || SbvTimestamp.IsMatch(line.Trim()))
if (!line.TrimStart().StartsWith("Dialogue:", StringComparison.OrdinalIgnoreCase))
if (fields.Length <= 9)
catch (XmlException)
if (first[index] != second[index])
```

Caption file matching by stem and language tag, UTF-8 byte ordering, and cue extraction for srt, vtt, sbv, ass, ssa, and ttml sidecars with no embedded track.

