# Catalog additions

Headings for source files that were not yet in the code catalog. Each section names the public types and quotes the branch lines the catalog test requires.

## src/Recall.Core/Media/MediaPreviewSeekPolicy.cs

```csharp
MediaPreviewSeekPolicy
```

## src/Recall.Core/Ocr/DbBoxExtractor.cs

```csharp
DbBoxExtractor
if (width <= 0 || height <= 0)
if (map.Length != width * height)
if (map.Length == 0)
if (!mask[index] || visited[index])
if (cx < minX)
if (cx > maxX)
if (cy < minY)
if (cy > maxY)
if (count >= MinPixels)
if (score < MinScore)
if (boxes.Count == 0)
if (boxes.Count > MaxBoxes)
if (x < 0 || y < 0 || x >= width || y >= height)
```

## src/Recall.Core/Ocr/OcrBox.cs

```csharp
OcrBox
```

## src/Recall.Core/Ocr/OcrCharacterErrorRate.cs

```csharp
OcrCharacterError
OcrCharacterErrorRate
if (reference.Length == 0)
if (string.IsNullOrEmpty(text))
if (char.IsWhiteSpace(ch))
if (pendingSpace)
if (ch is >= '\u0660' and <= '\u0669')
if (ch is >= '\u06F0' and <= '\u06F9')
if (ch is '\u066B')
if (ch is '\u066C' or '\u060C' or '\u061B' or '-' or ':' or '—' or '–' or '(' or ')' or '[' or ']')
```

## src/Recall.Core/Ocr/OcrGoldenPages.cs

```csharp
OcrGoldenPages
if (count < 1)
if (text.Length < count)
```

## src/Recall.Core/Ocr/OcrPageRouter.cs

```csharp
OcrLinePair
OcrPageRouter
if (lines.Count == 0)
if (i < sampleCount || pageReaderScore < LowConfidence)
if (string.IsNullOrEmpty(chosen))
if (builder.Length > 0 && !char.IsWhiteSpace(builder[^1]))
```

## src/Recall.Core/Ocr/OcrRunProgress.cs

```csharp
OcrRunProgress
if (!Enabled)
if (second <= 0 || second == lastHeartbeatSecond || second % 15 != 0)
```

## src/Recall.Core/Ingest/IngestPassMetrics.cs

```csharp
IngestPassMetric
IngestPassMetrics
if (count < MetricRingSize)
```

## src/Recall.Core/Ports/IOcrEngine.cs

```csharp
IOcrEngine
```

## src/Recall.Core/Ports/IResourceMeter.cs

```csharp
ResourceSample
IResourceMeter
if (workingSetBytes < 0)
if (cpuPercent is < 0 or > 100)
if (gpuPercent is < 0 or > 100)
```

## src/Recall.Core/Resources/ResourceSampleMath.cs

```csharp
ResourceSampleMath
if (values.Count == 0)
```

## src/Recall.Core/Scope/SkipListFilter.cs

```csharp
SkipListFilter
if (string.IsNullOrWhiteSpace(query))
```

## src/Recall.Core/Search/FtsQueryString.cs

```csharp
FtsQueryString
if (query.Length == 0)
```

## src/Recall.Maui/Platform/Bench/AndroidAudioDecodeSession.cs

```csharp
AndroidAudioDecodeSession
if (File.Exists(Path.Combine(external ?? string.Empty, "tone.m4a")))
else if (File.Exists(Path.Combine(appFilesDirectory, "tone.m4a")))
if (root is null)
```

## src/Recall.Maui/Platform/Bench/AndroidTextBenchSession.cs

```csharp
AndroidTextBenchSession
if (IsBenchRoot(external, options.ModelFileName))
else if (IsBenchRoot(appFilesDirectory, options.ModelFileName))
if (root is null)
if (!string.Equals(hostLine, "phone", StringComparison.OrdinalIgnoreCase)
if (!File.Exists(Path.Combine(modelDirectory, options.ModelFileName)))
catch (Exception ex)
if (tempRoot is not null && Directory.Exists(tempRoot))
if (File.Exists(path))
```

## src/Recall.Maui/Platform/Ocr/AndroidPdfPageRenderer.cs

```csharp
AndroidPdfPageRenderer
if (pageNumber < 1)
if (pageNumber > renderer.PageCount)
if (!bitmap.Compress(Bitmap.CompressFormat.Png!, 100, stream))
```

## src/Recall.Maui/Platform/Ocr/ApplePdfPageRenderer.cs

```csharp
ApplePdfPageRenderer
if (pageNumber < 1)
if (pageNumber > document.PageCount)
```

## src/Recall.Maui/Platform/Ocr/MauiPdfPageRenderer.cs

```csharp
MauiPdfPageRenderer
```

## src/Recall.Maui/Platform/Ocr/OcrInferenceYaml.cs

```csharp
No public type.
if (!inDict)
if (line.Trim() == DictHeader)
if (!line.StartsWith(EntryPrefix, StringComparison.Ordinal))
if (dict.Count == 0)
if (entry.Length >= 2
```

## src/Recall.Maui/Platform/Ocr/OcrRaster.cs

```csharp
No public type.
```

## src/Recall.Maui/Platform/Ocr/OcrRasterDecoder.cs

```csharp
No public type.
```

## src/Recall.Maui/Platform/Ocr/OnnxOcrEngine.cs

```csharp
OnnxOcrEngine
OcrLineRead
if (ShouldSkipPath(path))
if (pending.Count == 0)
if (path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
if (pageTexts.Count == 0)
if (string.IsNullOrEmpty(text))
if (Volatile.Read(ref activeJobs) > 0)
if (sessions.Loaded)
if (!sessions.Loaded)
if (now - lastJob < OcrIdleUnload)
if (pageIndex > 0)
if (string.Equals(text, "cpu", StringComparison.OrdinalIgnoreCase))
if (int.TryParse(threadText, out var threads) && threads > 0)
if (IsArabicLetter(rune))
else if (IsLatinLetter(rune))
if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
if (!File.Exists(path))
if (!string.IsNullOrEmpty(pageText))
if (row.Retired || row.Missing || row.Ordinal != 0)
if (matches == 1)
if (pageTexts.Count == 0 || row is null)
if (boxes.Count == 0)
if (string.IsNullOrEmpty(line.Text))
if (builder.Length > 0 && !char.IsWhiteSpace(builder[^1]))
if (lineReads.Count > LineReadRingSize)
if (output is DenseTensor<float> dense
if (results.Count != 1)
if (x < canvas.ContentWidth)
catch (Exception exception) when (provider == OcrExecutionProvider.DirectMl && IsProviderUnavailable(exception))
```

The engine keeps the three sessions resident between passes (`Sessions`), unloads them after `OcrIdleUnload` with no job, and disposes them on `Dispose`. `Provider` records the provider that actually created the sessions; a DirectML failure retries the same sessions on CPU. The per-session intra-op thread budget is `IntraOpThreads(provider, cores, pageWorkers)` — the ceiling split of the CPU thread budget over `PageWorkers`, with `RECALL_OCR_INTRA_OP_THREADS` still overriding it. PDF pages run through `Parallel.ForEachAsync` with `PageWorkers` (one under hard memory pressure) and are joined by page number. Every line is read by both readers and decided by `OcrLineChoice.Merge`; the pass keeps at most `LineReadRingSize` `OcrLineRead` entries. The detector map is read straight from the session's backing array when it is contiguous (`ReadContiguousMap`), and the input builders resample through the unchecked `OcrBilinear` path with packed normalize arithmetic.

## src/Recall.Maui/Platform/Ocr/OcrSessionSet.cs

```csharp
OcrSessionSet
if (provider == OcrExecutionProvider.DirectMl && OperatingSystem.IsWindows())
```

`Load` creates the detector, English reader, and Arabic reader `InferenceSession`s with one thread policy for all three and appends the DirectML provider at device 0 on Windows. `Dispose` disposes all three and clears them.

## src/Recall.Maui/Platform/Ocr/WindowsPdfPageRenderer.cs

```csharp
WindowsPdfPageRenderer
WindowsPdfDocument
if (pageNumber < 1)
if (pageNumber < 1 || pageNumber > PageCount)
```

## src/Recall.Maui/Platform/Ocr/WindowsTextPageRenderer.cs

```csharp
WindowsTextPageRenderer
if (width < 640 || height < 640)
```

## src/Recall.Maui/Platform/Resources/ProcessResourceMeter.cs

```csharp
ProcessResourceMeter
if (disposed != 0)
if (cpuSampleReady)
if (wallSeconds > 0)
if (!PerformanceCounterCategory.Exists("GPU Engine"))
if (names.Length == 0)
if (!gpuCounters.TryGetValue(name, out var counter))
```

## src/Recall.UI/Layout/IngestMissingSidebar.razor

Razor view for this path.

## src/Recall.UI/Layout/IngestMissingSidebar.razor.cs

```csharp
IngestMissingSidebar
if (disposing)
```

## src/Recall.UI/Layout/ResourceSampleFormat.cs

```csharp
ResourceSampleFormat
```

## src/Recall.UI/Layout/ResourceStrip.razor

Razor view for this path.

        else if (chromeStatus == IngestChromeStatus.Running)
        else if (chromeStatus == IngestChromeStatus.Stopped)
## src/Recall.UI/Layout/ResourceStrip.razor.cs

```csharp
ResourceStrip
if (disposing)
```

## tests/Recall.IntegrationTests/AgeCutDemoProofTests.cs

```csharp
AgeCutDemoProofTests
if (Directory.Exists(root))
if (!passes)
```

## tests/Recall.IntegrationTests/Fixtures/RealPdfFixture.cs

```csharp
RealPdfFixture
if (text.Length > 0)
if (ContentOrderTextExtractor.GetText(page).Length >= 12000)
if (File.Exists(temp))
```

## tests/Recall.IntegrationTests/GoldenLanguageProofTests.cs

```csharp
GoldenLanguageProofTests
if (!Directory.Exists(root))
catch (IOException)
```

## tests/Recall.IntegrationTests/Infrastructure/DemoDatasetPaths.cs

```csharp
DemoDatasetPaths
if (!Directory.Exists(Root))
```

## tests/Recall.IntegrationTests/Infrastructure/InMemoryAgeCutoffPreferenceStore.cs

```csharp
No public type.
```

## tests/Recall.IntegrationTests/Infrastructure/MediaPreviewSeekProof.cs

```csharp
MediaPreviewSeekProof
if (!MediaPreviewSeekPolicy.ShouldSeek(startSeconds, duration))
```

## tests/Recall.IntegrationTests/Infrastructure/OcrCharacterErrorRate.cs

```csharp
No public type.
if (reference.Length == 0)
```

## tests/Recall.IntegrationTests/OcrDemoProofTests.cs

```csharp
OcrDemoProofTests
if (FakeOcrEngine.CreateCount > 0)
if (terms.Any(t => stored.Contains(t, StringComparer.OrdinalIgnoreCase)))
```

## tests/Recall.IntegrationTests/OcrGoldenCatalog.cs

```csharp
No public type.
if (!string.IsNullOrWhiteSpace(configured))
if (!File.Exists(manifest))
if (string.IsNullOrWhiteSpace(line))
if (header)
if (fields.Count < 6)
if (!File.Exists(path))
if (!File.Exists(groundTruthPath))
if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
if (character == '"')
if (character == ',' && !quoted)
if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
```

## tests/Recall.IntegrationTests/OcrGoldenReportTests.cs

```csharp
OcrGoldenReportTests
if (index % 10 == 9)
if (Directory.Exists(tempDir))
if (cer.Rate > ceiling)
catch (IOException ex) when (ex.Message.Contains("being used", StringComparison.Ordinal))
```

## tests/Recall.IntegrationTests/PdfPatchProofTests.cs

```csharp
PdfPatchProofTests
if (Directory.Exists(root))
if (Directory.Exists(oplogDir))
```

## tests/Recall.IntegrationTests/PreviewSeekProofTests.cs

```csharp
PreviewSeekProofTests
if (File.Exists(Path.Combine(dir.FullName, "Recall.slnx")))
```

## tests/Recall.IntegrationTests/RealPdfPatchTests.cs

```csharp
RealPdfPatchTests
```

## tests/Recall.IntegrationTests/WindowsAudioProofTests.cs

```csharp
WindowsAudioProofTests
```

## tests/Recall.IntegrationTests/WindowsClipProofTests.cs

```csharp
WindowsClipProofTests
if (Environment.OSVersion.Version.Build < FrameCadence.WindowsMinimumBuild)
```

## tests/Recall.Maui.UnitTests/AndroidCompressedProofTests.cs

```csharp
AndroidCompressedProofTests
if (!AdbProcess.IsAvailable())
if (!devices.Output.Contains("device", StringComparison.Ordinal) || devices.Output.Trim().Equals("List of devices attached", StringComparison.Ordinal))
if (report.ExitCode == 0 && report.Output.Contains("sampleRate=", StringComparison.Ordinal))
catch (System.ComponentModel.Win32Exception)
```

## tests/Recall.Maui.UnitTests/CloseOpenProofsTests.cs

```csharp
CloseOpenProofsTests
if (Environment.OSVersion.Version.Build < FrameCadence.WindowsMinimumBuild)
```

## tests/Recall.Maui.UnitTests/OcrFixtureReference.cs

```csharp
No public type.
```

## tests/Recall.Maui.UnitTests/OcrRuntimeTests.cs

```csharp
OcrRuntimeTests
if (page.ReferenceCount != accepted.PageTokens || page.Edits > accepted.PageEdits)
if (accepted.LatinTokens > 0 && (latin.ReferenceCount != accepted.LatinTokens || latin.Edits > accepted.LatinEdits))
if (accepted.ArabicTokens > 0 && (arabic.ReferenceCount != accepted.ArabicTokens || arabic.Edits > accepted.ArabicEdits))
if (!logCleared)
```

## tests/Recall.Maui.UnitTests/OcrWordErrorRate.cs

```csharp
No public type.
if (referenceTokens.Count == 0)
if (!available.TryGetValue(token, out var count) || count == 0)
if (reference.Count == 0)
if (ch is >= '\u0600' and <= '\u06FF')
if (normalized is null)
if (current.Length > 0)
if (ch is '\u0640' or >= '\u064B' and <= '\u065F' or '\u0670')
if (char.IsLetterOrDigit(ch) || ch is >= '\u0600' and <= '\u06FF')
```

## tests/Recall.Maui.UnitTests/OcrWordErrorRateTests.cs

```csharp
OcrWordErrorRateTests
```

## tests/Recall.UnitTests/CloseOpenCoreBranchTests.cs

```csharp
CloseOpenCoreBranchTests
```

## tests/Recall.UnitTests/CloseOpenIngestUiTests.cs

```csharp
CloseOpenIngestUiTests
if (head.Contains("BuiltInSkips", StringComparison.Ordinal))
if (head.Contains("skips.Extensions", StringComparison.Ordinal)
```

## tests/Recall.UnitTests/CloseOpenOcrTests.cs

```csharp
CloseOpenOcrTests
```

## tests/Recall.UnitTests/IngestMediaStartTests.cs

```csharp
IngestMediaStartTests
if (Calls == 1)
```

## tests/Recall.UnitTests/ModelDownloadRowPolicyTests.cs

```csharp
ModelDownloadRowPolicyTests
if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
```

## tests/Recall.UnitTests/OcrPackLanguageTests.cs

```csharp
OcrPackLanguageTests
```

## src/Recall.UI/Layout/ShellStatusLabel.cs

```csharp
ShellStatusLabel
```

The rail and the footer use `Ingest_Running` while ingest or a model download is running, and `Status_Ready` only when that status is Done. Stopped does not say Ready.

## src/Recall.UI/Components/Shell/ShellRouteMatch.cs

```csharp
ShellRouteMatch
if (string.Equals(route, href, StringComparison.Ordinal))
```

`/device` and `/pin` keep the Preferences item active in the rail and the bottom tabs.

## src/Recall.UI/Pages/Library/Pin.razor.cs

```csharp
Pin
```

Back returns to `/preferences`.

## tests/Recall.UnitTests/ShellChromeTests.cs

```csharp
ShellChromeTests
```

## src/Recall.Core/Ingest/IngestCancellation.cs

```csharp
IngestCancellation
```

`PauseOnlyReading` moves a `Reading` row to `Paused` on search cancel and leaves `Finished`, `Missing`, and `Paused` unchanged.

## src/Recall.Core/Ingest/IngestRetryClock.cs

```csharp
IngestRetryClock
```

Schedules the next retry attempt from a finish time and minutes, and gates a tick on gate open, GPU cool, model ready, and time.

## src/Recall.Core/Ingest/IngestRetryPolicy.cs

```csharp
IngestRetryPolicy
```

`AllowAutomatic` is true for fewer than three logged failures per path; the manual retry button ignores the cap.

## src/Recall.Core/Ingest/LeftoverPolicy.cs

```csharp
LeftoverAction
LeftoverPolicy
if (hasLiveChunks)
if (!fileExists)
```

End-of-pass cleanup: live chunks finish the row, a missing file becomes `Missing`, otherwise the row is paused.

## src/Recall.Core/Ports/IGpuTemperature.cs

```csharp
IGpuTemperature
```

## src/Recall.Core/Ports/NullGpuTemperature.cs

```csharp
NullGpuTemperature
```

Non-Windows and test hosts return no GPU temperature so ingest does not thermal-pause.

## src/Recall.Core/Preferences/MemoryRetryDelayPreferenceStore.cs

```csharp
MemoryRetryDelayPreferenceStore
```

In-memory retry minutes for unit tests and headless Core.

## src/Recall.Core/Preferences/RetryDelayPolicy.cs

```csharp
RetryDelayPolicy
```

Default 5 minutes, allowed range 1–60 inclusive.

## src/Recall.Core/Preferences/RetryDelayPreferenceService.cs

```csharp
RetryDelayPreferenceService
if (!RetryDelayPolicy.Allowed(minutes))
```

## src/Recall.Core/Runtime/GpuCooldownPolicy.cs

```csharp
GpuCooldownPolicy
```

`HotCelsius` is 80. `Blocks` is true only when the reading is above 80, not at 80, and not when the reading is absent.

## src/Recall.Maui/Ingest/IngestRetryHostedService.cs

```csharp
IngestRetryHostedService
if (loopTask is not null)
catch
if (ingest.PassRunning)
if (unfinished.Count == 0)
if (eligible.Count == 0)
if (!IngestRetryClock.ShouldRun(now, next, gateOpen, gpuHot, modelReady))
```

Fifteen-second tick over unfinished cursor paths; defers without disk walk when the device is not ready.

## src/Recall.Maui/Platform/Preferences/MauiRetryDelayPreferenceStore.cs

```csharp
MauiRetryDelayPreferenceStore
```

Store key `recall.preferences.retryMinutes`.

## src/Recall.Maui/Platform/Resources/WindowsGpuTemperature.cs

```csharp
WindowsGpuTemperature
AbsentGpuTemperature
if (!process.Start())
if (!process.WaitForExit(2000))
catch
if (string.IsNullOrEmpty(line))
catch
```

Windows runs `nvidia-smi` with a two-second wait; other RIDs use `AbsentGpuTemperature`.

## src/Recall.UI/Pages/Library/Activity.razor.cs

```csharp
Activity
```

`/activity` lists the newest 200 local ingest log entries.



## src/Recall.Core/Ports/GrantFilePorts.cs

``csharp
using Recall.Core.Scope;

namespace Recall.Core.Ports;

public interface IGrantFileUrl
{
    string? Build(string sourcePath, IReadOnlyList<ScopeFolderGrant> grants);

    bool TryResolveUri(Uri uri, IReadOnlyList<ScopeFolderGrant> grants, out string? sourcePath);
}

public interface IFolderReveal
{
    bool Reveal(string sourcePath);
}
``n
## src/Recall.Core/Scope/GrantFileUrlPolicy.cs

``csharp
namespace Recall.Core.Scope;

public static class GrantFileUrlPolicy
{
    public const string Scheme = "recall-file";

    public static bool PathInGrants(string filePath, IReadOnlyList<ScopeFolderGrant> grants)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(grants);
        if (filePath.StartsWith(ContentUri.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return grants.Count > 0;
        }

        string full;
        try
        {
            full = Path.GetFullPath(filePath);
        }
        catch
        {
            return false;
        }

        for (var index = 0; index < grants.Count; index++)
        {
            if (IsUnderGrant(full, grants[index].Path))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUnderGrant(string fullPath, string grantPath)
    {
        if (string.IsNullOrWhiteSpace(grantPath))
        {
            return false;
        }

        string root;
        try
        {
            root = Path.GetFullPath(grantPath);
        }
        catch
        {
            return false;
        }

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(prefix, comparison) || string.Equals(fullPath, root, comparison);
    }
}
``n
## src/Recall.Core/Search/Bm25Highlight.cs

``csharp
using Recall.Core.Text;

namespace Recall.Core.Search;

public readonly record struct HighlightMark(int Start, int Length);

public static class Bm25Highlight
{
    public static IReadOnlyList<HighlightMark> Marks(string excerpt, string query)
    {
        ArgumentNullException.ThrowIfNull(excerpt);
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var tokens = KeywordFold.Tokens(KeywordFold.Fold(query));
        if (tokens.Count == 0)
        {
            return [];
        }

        var marks = new List<HighlightMark>();
        foreach (var token in tokens)
        {
            if (string.IsNullOrEmpty(token))
            {
                continue;
            }

            var start = 0;
            while (start <= excerpt.Length - token.Length)
            {
                var index = excerpt.IndexOf(token, start, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    break;
                }

                marks.Add(new HighlightMark(index, token.Length));
                start = index + token.Length;
            }
        }

        return marks;
    }
}
``n
## src/Recall.Core/Search/MatchStrength.cs

``csharp
namespace Recall.Core.Search;

public static class MatchStrength
{
    public static int? Percent(double similarity) =>
        similarity is >= 0 and <= 1 ? (int)Math.Round(similarity * 100) : null;
}
``n
## src/Recall.Core/Search/PictureExactFile.cs

``csharp
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.Core.Vectors;
using Recall.Core.Vocabulary;

namespace Recall.Core.Search;

public static class PictureExactFile
{
    public static IReadOnlyList<MediaSearchHit> Promote(
        IReadOnlyList<MediaSearchHit> hits,
        string queryHash,
        IReadOnlyList<IngestFileRow> finishedRows)
    {
        ArgumentNullException.ThrowIfNull(hits);
        ArgumentNullException.ThrowIfNull(finishedRows);
        if (string.IsNullOrWhiteSpace(queryHash))
        {
            return hits;
        }

        var match = finishedRows.FirstOrDefault(row =>
            row.State == IngestCursorState.Finished
            && string.Equals(row.ContentHash, queryHash, StringComparison.Ordinal)
            && MediaFileKinds.IsImage(row.Path));
        if (match is null)
        {
            return hits;
        }

        var list = hits.ToList();
        var index = list.FindIndex(hit => string.Equals(hit.SourcePath, match.Path, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            var contentId = ContentIdBuilder.Build(Modality.Image, queryHash, StoredWidth.W768, 0);
            list.Insert(0, new MediaSearchHit(contentId, match.Path, 0, 0, 1, 1));
            return list;
        }

        if (index == 0)
        {
            return hits;
        }

        var promoted = list[index];
        list.RemoveAt(index);
        list.Insert(0, promoted);
        return list;
    }
}
``n
## src/Recall.Core/Search/ResultActionPolicy.cs

``csharp
using Recall.Core.Scope;

namespace Recall.Core.Search;

public enum ResultActionKind
{
    OpenOnly = 0,
    View = 1,
    Play = 2,
}

public static class ResultActionPolicy
{
    public static ResultActionKind Decide(string sourcePath)
    {
        if (MediaFileKinds.IsImage(sourcePath))
        {
            return ResultActionKind.View;
        }

        if (MediaFileKinds.IsVideo(sourcePath) || MediaFileKinds.IsAudio(sourcePath))
        {
            return ResultActionKind.Play;
        }

        return ResultActionKind.OpenOnly;
    }
}
``n
## src/Recall.Core/Search/SearchDeadline.cs

``csharp
namespace Recall.Core.Search;

public static class SearchDeadline
{
    public static readonly TimeSpan FindTimeout = TimeSpan.FromSeconds(45);
}
``n
## src/Recall.Core/Search/SearchGroups.cs

``csharp
namespace Recall.Core.Search;

public enum SearchGroupKind
{
    Text = 0,
    Image = 1,
    Video = 2,
    Audio = 3,
}

public readonly record struct SearchResultGroup(string GroupKey, IReadOnlyList<TextFindCard> Cards);

public static class SearchGroups
{
    private static readonly (SearchGroupKind Kind, string GroupKey)[] Order =
    [
        (SearchGroupKind.Text, "Search_Group_Text"),
        (SearchGroupKind.Image, "Search_Group_Image"),
        (SearchGroupKind.Video, "Search_Group_Video"),
        (SearchGroupKind.Audio, "Search_Group_Audio"),
    ];

    public static IReadOnlyList<SearchResultGroup> Split(IReadOnlyList<TextFindCard> cards)
    {
        var buckets = new Dictionary<SearchGroupKind, List<TextFindCard>>();
        foreach (var card in cards)
        {
            var kind = TextFindCardKind.KindFromPath(card.SourcePath);
            if (!buckets.TryGetValue(kind, out var list))
            {
                list = [];
                buckets[kind] = list;
            }

            list.Add(card);
        }

        var groups = new List<SearchResultGroup>();
        foreach (var (kind, groupKey) in Order)
        {
            if (!buckets.TryGetValue(kind, out var list) || list.Count == 0)
            {
                continue;
            }

            groups.Add(new SearchResultGroup(groupKey, list));
        }

        return groups;
    }
}
``n
## src/Recall.Core/Search/TextFindCard.cs

``csharp
using System.Diagnostics.CodeAnalysis;
using Recall.Core.Ingest;
using Recall.Core.Scope;

namespace Recall.Core.Search;

public sealed record TextFindCard(
    string SourcePath,
    string FileName,
    string KindKey,
    string LocalTime,
    string ReasonKey,
    bool Unavailable,
    string RawNote = "",
    double StartSeconds = 0,
    bool ShowPin = false,
    int? MatchPercent = null,
    bool ShowMeaningHelp = false);

public sealed record TextFindView(IReadOnlyList<TextFindCard> Cards, bool HasHits)
{
    public const int ImmediateCardCount = 5;

    public static TextFindView Empty { get; } = new([], false);

    [SuppressMessage("Major Code Smell", "S2365:Properties should not copy collections", Justification = "Keeps Immediate and Deferred for existing unit tests.")]
    public IReadOnlyList<TextFindCard> Immediate => Cards.Take(ImmediateCardCount).ToList();

    [SuppressMessage("Major Code Smell", "S2365:Properties should not copy collections", Justification = "Keeps Immediate and Deferred for existing unit tests.")]
    public IReadOnlyList<TextFindCard> Deferred => Cards.Skip(ImmediateCardCount).ToList();
}

public static class TextFindCardKind
{
    public static SearchGroupKind KindFromPath(string sourcePath)
    {
        if (MediaFileKinds.IsImage(sourcePath))
        {
            return SearchGroupKind.Image;
        }

        if (MediaFileKinds.IsVideo(sourcePath))
        {
            return SearchGroupKind.Video;
        }

        if (MediaFileKinds.IsAudio(sourcePath))
        {
            return SearchGroupKind.Audio;
        }

        return SearchGroupKind.Text;
    }

    public static string KindKeyFromPath(string sourcePath)
    {
        if (MediaFileKinds.IsImage(sourcePath))
        {
            return "Search_Kind_Image";
        }

        if (MediaFileKinds.IsVideo(sourcePath))
        {
            return "Search_Kind_Video";
        }

        if (MediaFileKinds.IsAudio(sourcePath))
        {
            return "Search_Kind_Audio";
        }

        if (FileAdmission.IsCodeFile(sourcePath))
        {
            return "Search_Kind_Code";
        }

        return "Search_Kind_Text";
    }
}
``n
## src/Recall.Maui/Platform/Files/GrantFileSchemeRegistration.cs

``csharp
namespace Recall.Maui.Platform.Files;

public static partial class GrantFileSchemeRegistration
{
    public static void Register(IServiceProvider services, object? platformWebView) =>
        RegisterCore(services, platformWebView);

    private static partial void RegisterCore(IServiceProvider services, object? platformWebView);
}
``n
## src/Recall.Maui/Platform/Files/GrantFileSchemeRegistration.Other.cs

``csharp
#if !WINDOWS
namespace Recall.Maui.Platform.Files;

public static partial class GrantFileSchemeRegistration
{
    private static partial void RegisterCore(IServiceProvider services, object? platformWebView)
    {
    }
}
#endif
``n
## src/Recall.Maui/Platform/Files/GrantFileSchemeRegistration.Windows.cs

``csharp
#if WINDOWS
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml.Controls;
using Recall.Core.Ports;
using Recall.Core.Scope;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Recall.Maui.Platform.Files;

public static partial class GrantFileSchemeRegistration
{
    private static bool registered;

    private static partial void RegisterCore(IServiceProvider services, object? platformWebView)
    {
        if (platformWebView is not BlazorWebView blazorWebView)
        {
            return;
        }

        blazorWebView.HandlerChanged += (_, _) =>
        {
            if (registered || blazorWebView.Handler?.PlatformView is not WebView2 webView2)
            {
                return;
            }

            registered = true;
            _ = HookAsync(services, webView2);
        };
    }

    private static async Task HookAsync(IServiceProvider services, WebView2 webView2)
    {
        await webView2.EnsureCoreWebView2Async();
        if (webView2.CoreWebView2 is null)
        {
            return;
        }

        var core = webView2.CoreWebView2;
#pragma warning disable S1075
        core.AddWebResourceRequestedFilter("recall-file://*", CoreWebView2WebResourceContext.All);
#pragma warning restore S1075
        core.WebResourceRequested += (_, e) =>
        {
            if (!e.Request.Uri.StartsWith(GrantFileUrlPolicy.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var scope = services.GetService(typeof(IScopeManagerService)) as IScopeManagerService;
            var grants = scope?.ListGrants() ?? [];
            var grantUrls = services.GetService(typeof(IGrantFileUrl)) as IGrantFileUrl;
            if (grantUrls is null || !Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var uri))
            {
                return;
            }

            if (!grantUrls.TryResolveUri(uri, grants, out var path) || path is null)
            {
                e.Response = core.Environment.CreateWebResourceResponse(null, 403, "Forbidden", string.Empty);
                return;
            }

            try
            {
                var stream = File.OpenRead(path);
                e.Response = core.Environment.CreateWebResourceResponse(
                    stream.AsRandomAccessStream(),
                    200,
                    "OK",
                    $"Content-Type: {ContentTypeForPath(path)}");
            }
            catch (IOException)
            {
                e.Response = core.Environment.CreateWebResourceResponse(null, 404, "Not Found", string.Empty);
            }
        };
    }

    private static string ContentTypeForPath(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            ".mkv" => "video/x-matroska",
            ".avi" => "video/x-msvideo",
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".wav" => "audio/wav",
            ".flac" => "audio/flac",
            ".ogg" => "audio/ogg",
            ".aac" => "audio/aac",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream",
        };
}
#endif
``n
## src/Recall.Maui/Platform/Files/MauiFolderReveal.cs

``csharp
using Recall.Core.Ports;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiFolderReveal : IFolderReveal
{
    public bool Reveal(string sourcePath) => TryReveal(sourcePath);

    private partial bool TryReveal(string sourcePath);
}
``n
## src/Recall.Maui/Platform/Files/MauiFolderReveal.Other.cs

``csharp
#if !WINDOWS
using System.Diagnostics;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiFolderReveal
{
#if MACCATALYST
    private partial bool TryReveal(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"-R \"{sourcePath}\"",
                UseShellExecute = false,
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
#else
    private partial bool TryReveal(string sourcePath) => false;
#endif
}
#endif
``n
## src/Recall.Maui/Platform/Files/MauiFolderReveal.Windows.cs

``csharp
#if WINDOWS
using System.Diagnostics;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiFolderReveal
{
    private partial bool TryReveal(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
#pragma warning disable S4036
                FileName = "explorer.exe",
#pragma warning restore S4036
                Arguments = $"/select,\"{sourcePath}\"",
                UseShellExecute = true,
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
#endif
``n
## src/Recall.Maui/Platform/Files/MauiGrantFileUrl.cs

``csharp
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Maui.Platform.Files;

public sealed class MauiGrantFileUrl : IGrantFileUrl
{
    public string? Build(string sourcePath, IReadOnlyList<ScopeFolderGrant> grants)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || grants is null || grants.Count == 0)
        {
            return null;
        }

        if (!GrantFileUrlPolicy.PathInGrants(sourcePath, grants))
        {
            return null;
        }

        if (!ContentUri.IsContent(sourcePath) && !File.Exists(sourcePath))
        {
            return null;
        }

        var encoded = Uri.EscapeDataString(sourcePath);
        return $"{GrantFileUrlPolicy.Scheme}://local/{encoded}";
    }

    public bool TryResolveUri(Uri uri, IReadOnlyList<ScopeFolderGrant> grants, out string? sourcePath)
    {
        sourcePath = null;
        if (grants is null || !string.Equals(uri.Scheme, GrantFileUrlPolicy.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var payload = uri.Host.Equals("local", StringComparison.OrdinalIgnoreCase)
            ? uri.AbsolutePath.TrimStart('/')
            : $"{uri.Host}{uri.AbsolutePath}".TrimStart('/');

        if (payload.Length == 0)
        {
            return false;
        }

        string path;
        try
        {
            path = Uri.UnescapeDataString(payload);
        }
        catch
        {
            return false;
        }

        if (!GrantFileUrlPolicy.PathInGrants(path, grants))
        {
            return false;
        }

        if (!ContentUri.IsContent(path) && !File.Exists(path))
        {
            return false;
        }

        sourcePath = path;
        return true;
    }
}
``n
## src/Recall.UI/Components/Find/ActionMenu.razor

``csharp
@namespace Recall.UI.Components.Find
@inherits RecallComponentBase
@using Recall.Core.Search

<div class="fixed inset-0 z-40" @onclick="OnClose">
    <div class="absolute g2 rounded-xl p-2 shadow-lg min-w-[12rem]" style="top:30%;left:50%;transform:translate(-50%,-50%)" @onclick:stopPropagation="true" role="menu">
        @if (!Card.Unavailable)
        {
            <button type="button" class="btn btn-g w-full justify-start mb-1" role="menuitem" @onclick="() => OnOpen.InvokeAsync(Card)">@T("Search_Open")</button>
            <button type="button" class="btn btn-g w-full justify-start mb-1" role="menuitem" @onclick="() => OnReveal.InvokeAsync(Card)">@T("Search_OpenPath")</button>
            @if (ResultActionPolicy.Decide(Card.SourcePath) is ResultActionKind.View)
            {
                <button type="button" class="btn btn-g w-full justify-start" role="menuitem" @onclick="() => OnViewPlay.InvokeAsync(Card)">@T("Search_View")</button>
            }
            else if (ResultActionPolicy.Decide(Card.SourcePath) is ResultActionKind.Play)
            {
                <button type="button" class="btn btn-g w-full justify-start" role="menuitem" @onclick="() => OnViewPlay.InvokeAsync(Card)">@T("Search_Play")</button>
            }
            @if (Card.ShowPin)
            {
                <button type="button" class="btn btn-g w-full justify-start" role="menuitem" @onclick="() => OnPin.InvokeAsync(Card)">@T("Pin_Keep")</button>
            }
        }
    </div>
</div>


``n
## src/Recall.UI/Components/Find/ActionMenu.razor.cs

``csharp
using Microsoft.AspNetCore.Components;
using Recall.Core.Search;

namespace Recall.UI.Components.Find;

public partial class ActionMenu
{
    [Parameter, EditorRequired]
    public TextFindCard Card { get; set; } = null!;

    [Parameter]
    public EventCallback<TextFindCard> OnOpen { get; set; }

    [Parameter]
    public EventCallback<TextFindCard> OnReveal { get; set; }

    [Parameter]
    public EventCallback<TextFindCard> OnViewPlay { get; set; }

    [Parameter]
    public EventCallback<TextFindCard> OnPin { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }
}
``n
## src/Recall.UI/Components/Find/ResultModal.razor

``csharp
@namespace Recall.UI.Components.Find
@inherits RecallComponentBase
@using Recall.Core.Search
@using Recall.Core.Scope

@if (Visible)
{
    <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:color-mix(in srgb,var(--c0) 55%,transparent)">
        <div class="g2 rounded-2xl max-w-4xl w-full max-h-[90vh] flex flex-col overflow-hidden rise" role="dialog" aria-modal="true" aria-label="@T("Search_View")">
            <div class="flex items-center justify-end p-2 shrink-0">
                <button type="button" class="btn btn-g" @onclick="OnClosed">@T("Search_Close")</button>
            </div>
            <div class="flex-1 min-h-0 overflow-auto p-3 flex items-center justify-center">
                @if (PickedImage is not null)
                {
                    <img class="max-h-[75vh] max-w-full object-contain rounded-lg" alt="" src="@LensDataUri" />
                }
                else if (MediaUrl is not null && Card is not null && MediaFileKinds.IsVideo(Card.SourcePath))
                {
                    <video @key="MediaUrl" class="max-h-[75vh] max-w-full rounded-lg" controls autoplay src="@MediaUrl"></video>
                }
                else if (MediaUrl is not null && Card is not null && MediaFileKinds.IsAudio(Card.SourcePath))
                {
                    <audio @key="MediaUrl" class="w-full" controls autoplay src="@MediaUrl"></audio>
                }
                else if (MediaUrl is not null)
                {
                    <img class="max-h-[75vh] max-w-full object-contain rounded-lg" alt="" src="@MediaUrl" />
                }
            </div>
        </div>
    </div>
}


``n
## src/Recall.UI/Components/Find/ResultModal.razor.cs

``csharp
using Microsoft.AspNetCore.Components;
using Recall.Core.Search;

namespace Recall.UI.Components.Find;

public partial class ResultModal
{
    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public TextFindCard? Card { get; set; }

    [Parameter]
    public byte[]? PickedImage { get; set; }

    [Parameter]
    public string? MediaUrl { get; set; }

    [Parameter]
    public EventCallback OnClosed { get; set; }

    private string? LensDataUri =>
        PickedImage is null ? null : "data:image/jpeg;base64," + Convert.ToBase64String(PickedImage);
}
``n
## tests/Recall.IntegrationTests/SearchProofIntegrationTests.cs

``csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recall.Adapters.Embedding;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.IntegrationTests.Infrastructure;

namespace Recall.IntegrationTests;

public sealed class SearchProofIntegrationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "recall_search_proof_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        try
        {
            Directory.Delete(root, true);
        }
        catch
        {
            // best effort
        }
    }

    [Fact]
    public async Task ExactImage_SameBytes_IsHitZero()
    {
        Environment.SetEnvironmentVariable("RECALL_EMBED_CPU_ONLY", "1");
        var modelDir = IntegrationTextModelPaths.ResolveModelDirectory();
        var mmproj = MediaContextProbePaths.ResolveMmprojFile();
        var visionDir = Path.GetDirectoryName(mmproj) ?? modelDir;
        MediaContextProbe.EnsureApplied(Path.Combine(modelDir, IntegrationTextModelPaths.ModelFileName), mmproj);

        Directory.CreateDirectory(root);
        await using var provider = IntegrationTestServiceCollection.Build(modelDir, Path.Combine(root, "zvec"), visionDir);
        var grantPath = Path.Combine(root, "pics");
        Directory.CreateDirectory(grantPath);
        var grant = new ScopeFolderGrant(grantPath, "pics", GrantFileKinds.Image);
        provider.GetRequiredService<IScopeManagerService>().TryAddGrant(grant);
        var grants = new List<ScopeFolderGrant> { grant };

        var paths = new string[6];
        for (var i = 0; i < paths.Length; i++)
        {
            paths[i] = Path.Combine(grantPath, $"img{i + 1}.jpg");
            await File.WriteAllBytesAsync(paths[i], DistinctJpeg(i + 2), TestContext.Current.CancellationToken);
        }

        var vision = provider.GetRequiredService<IVisionFileIngest>();
        foreach (var path in paths)
        {
            await vision.IndexImageAsync(grant, path, TestContext.Current.CancellationToken);
        }

        var queryBytes = await File.ReadAllBytesAsync(paths[0], TestContext.Current.CancellationToken);
        var hits = await provider.GetRequiredService<IPictureSearchService>()
            .FindAsync(queryBytes, grants, TestContext.Current.CancellationToken);

        hits.Should().NotBeEmpty();
        hits[0].SourcePath.Should().Be(paths[0]);
    }

    [Fact]
    public async Task ExactWord_BeatsFiveOtherFiles()
    {
        Environment.SetEnvironmentVariable("RECALL_EMBED_CPU_ONLY", "1");
        var modelDir = IntegrationTextModelPaths.ResolveModelDirectory();
        Directory.CreateDirectory(root);
        await using var provider = IntegrationTestServiceCollection.Build(modelDir, Path.Combine(root, "zvec"));
        var grantPath = Path.Combine(root, "notes");
        Directory.CreateDirectory(grantPath);
        var grant = new ScopeFolderGrant(grantPath, "notes", GrantFileKinds.Text);
        provider.GetRequiredService<IScopeManagerService>().TryAddGrant(grant);
        var grants = new List<ScopeFolderGrant> { grant };

        const string keyword = "xylophonequartz";
        var topics = new[]
        {
            "Urban bicycle commuting includes helmet habits, lane positioning, and weather planning for safe daily rides.",
            "Sourdough baking needs a lively starter, long ferments, careful scoring, and steady oven steam for open crumb.",
            "Marine biology surveys track coral bleaching, fish counts, plankton blooms, and water temperature along reefs.",
            "Database normalization reduces update anomalies, preserves integrity, and keeps reporting schemas maintainable.",
            "Garden composting balances greens and browns, aerates piles weekly, and keeps moisture steady for breakdown.",
        };

        var targetPath = Path.Combine(grantPath, "six.txt");
        for (var i = 0; i < topics.Length; i++)
        {
            var path = Path.Combine(grantPath, $"note{i + 1}.txt");
            await File.WriteAllTextAsync(path, topics[i], TestContext.Current.CancellationToken);
        }

        await File.WriteAllTextAsync(
            targetPath,
            $"Instrument essays mention resonance, mallets, and tone. Only this file contains the token {keyword} in the whole set.",
            TestContext.Current.CancellationToken);

        await provider.GetRequiredService<IGrantedTextIngestService>()
            .IngestGrantedTextAsync(TestContext.Current.CancellationToken);

        var hits = await provider.GetRequiredService<ITextSearchService>()
            .FindAsync(keyword, grants, TestContext.Current.CancellationToken);

        hits.Count.Should().BeGreaterThan(1);
        hits[0].SourcePath.Should().Be(targetPath);
    }

    private static byte[] DistinctJpeg(int dimension)
    {
        var jpeg = (byte[])MediaContextProbe.MinimalJpeg().Clone();
        var sof = FindMarker(jpeg, 0xC0);
        jpeg[sof + 5] = (byte)(dimension >> 8);
        jpeg[sof + 6] = (byte)(dimension & 0xFF);
        jpeg[sof + 7] = (byte)(dimension >> 8);
        jpeg[sof + 8] = (byte)(dimension & 0xFF);
        return jpeg;
    }

    private static int FindMarker(byte[] jpeg, byte marker)
    {
        for (var i = 0; i < jpeg.Length - 1; i++)
        {
            if (jpeg[i] == 0xFF && jpeg[i + 1] == marker)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"JPEG marker 0x{marker:X2} not found.");
    }
}
``n

## src/Recall.Maui/MainPage.xaml.cs

``csharp
using Recall.Core.Ports;
using Recall.Maui.Platform.Files;

namespace Recall.Maui;

public partial class MainPage : ContentPage
{
    private bool schemeRegistered;

	public MainPage()
	{
		InitializeComponent();
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();
		if (Handler is null)
		{
			return;
		}

        var services = Handler.MauiContext?.Services;
		services?.GetService<IGrantedFolderAccessService>()?.RestorePersistedAccess();
        if (!schemeRegistered && services is not null)
        {
            schemeRegistered = true;
            GrantFileSchemeRegistration.Register(services, blazorWebView);
        }
	}
}

``n
## src/Recall.Core/Search/PictureSearchService.cs

``csharp
using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.Core.Vectors;

namespace Recall.Core.Search;

public sealed class PictureSearchService : IPictureSearchService
{
    private readonly IVisionEmbeddingEncoder encoder;
    private readonly IMediaVectorIndex index;
    private readonly IEncoderFileHash encoderHash;
    private readonly IIngestCursorStore cursors;

    public PictureSearchService(
        IVisionEmbeddingEncoder encoder,
        IMediaVectorIndex index,
        IEncoderFileHash encoderHash,
        IIngestCursorStore cursors)
    {
        this.encoder = encoder;
        this.index = index;
        this.encoderHash = encoderHash;
        this.cursors = cursors;
    }

    public Task<IReadOnlyList<MediaSearchHit>> FindAsync(
        ReadOnlyMemory<byte> jpeg,
        IReadOnlyList<ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default)
    {
        if (jpeg.IsEmpty || grants is null)
        {
            return Task.FromResult<IReadOnlyList<MediaSearchHit>>([]);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var grantedPaths = grants
            .Where(grant => grant.EnabledKinds.HasFlag(GrantFileKinds.Image) || grant.EnabledKinds.HasFlag(GrantFileKinds.Video))
            .Select(grant => grant.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hash = encoderHash.VisionHash ?? string.Empty;
        var filter = TextSearchFilter.Build(grantedPaths, hash);
        if (filter.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<MediaSearchHit>>([]);
        }

        var vector = new float[MediaCollections.Width];
        encoder.EmbedJpeg(jpeg, vector);
        var vector512 = MatryoshkaVectorMath.CutAndNormalize(vector, MediaCollections.OldWidth);

        var lists = new List<IReadOnlyList<MediaSearchHit>>(MediaCollections.PictureCollections.Length);
        foreach (var collection in MediaCollections.PictureCollections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = MediaCollections.WidthFor(collection) == MediaCollections.OldWidth ? vector512 : vector;
            var request = new MediaSearchRequest(collection, query, filter, RecallIndexCaps.RawListCap);
            var hits = index.Search(request, cancellationToken);
            lists.Add(CollapseCollection(hits));
        }

        var fused = AgeRrf.Fuse(lists);
        var queryHash = ContentIdBuilder.HashBytes(jpeg.Span);
        var finishedRows = ListFinishedImageRows(grants);
        var promoted = PictureExactFile.Promote(fused, queryHash, finishedRows);
        return Task.FromResult<IReadOnlyList<MediaSearchHit>>(promoted.Take(RecallIndexCaps.FileCap).ToList());
    }

    private IReadOnlyList<IngestFileRow> ListFinishedImageRows(IReadOnlyList<ScopeFolderGrant> grants)
    {
        var rows = new List<IngestFileRow>();
        foreach (var grant in grants)
        {
            if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Image))
            {
                continue;
            }

            foreach (var row in cursors.ListFiles(grant.Path))
            {
                if (row.State == IngestCursorState.Finished && MediaFileKinds.IsImage(row.Path))
                {
                    rows.Add(row);
                }
            }
        }

        return rows;
    }

    private static IReadOnlyList<MediaSearchHit> CollapseCollection(IReadOnlyList<MediaSearchHit> hits) =>
        hits
            .GroupBy(hit => hit.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => Collapse.EarliestStart(group.ToList()))
            .ToList();
}

``n
## src/Recall.Core/Search/TextSearchService.cs

``csharp
using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.Core.Vectors;

namespace Recall.Core.Search;

public sealed class TextSearchService : ITextSearchService
{
    private readonly ITextEmbeddingEncoder encoder;
    private readonly ITextVectorIndex index;
    private readonly IIndexPolicy indexPolicy;
    private readonly IIngestCursorStore cursors;
    private readonly IEncoderFileHash encoderHash;
    private readonly IMediaVectorIndex media;
    private readonly IIngestPreemption preemption;

    private readonly IVisionEmbeddingEncoder? visionEncoder;

    public TextSearchService(
        ITextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        IIndexPolicy indexPolicy,
        IIngestCursorStore cursors,
        IEncoderFileHash encoderHash,
        IMediaVectorIndex media,
        IVisionEmbeddingEncoder? visionEncoder = null,
        IIngestPreemption? preemption = null)
    {
        this.encoder = encoder;
        this.index = index;
        this.indexPolicy = indexPolicy;
        this.cursors = cursors;
        this.encoderHash = encoderHash;
        this.media = media ?? throw new ArgumentNullException(nameof(media));
        this.visionEncoder = visionEncoder;
        this.preemption = preemption ?? new IngestPreemption();
    }

    private void ReconcilePending(IReadOnlyList<ScopeFolderGrant> grants)
    {
        if (cursors.PendingCount() > 0 && cursors.TryEnterWrite(out var lease) && lease is not null)
        {
            try
            {
                var pending = cursors.ListPending(RecallIndexCaps.RawListCap);
                if (pending.Count > 0)
                {
                    var states = pending
                        .Select(id => new VectorLiveState(
                            id,
                            cursors.AnyLivePath(id),
                            encoderHash.TextHash ?? string.Empty,
                            ResolveGrantPath(grants)))
                        .ToList();
                    index.Reconcile(states);
                    media.Reconcile(states);
                    cursors.RemovePending(pending);
                }

                lease.Commit();
            }
            catch
            {
                lease.Rollback();
                throw;
            }
        }
    }

    internal IVisionEmbeddingEncoder? VisionEncoder => visionEncoder;

    public async Task<IReadOnlyList<TextSearchHit>> FindAsync(
        string query,
        IReadOnlyList<ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || grants is null)
        {
            return [];
        }

        var grantedPaths = BuildGrantedPathSet(grants);
        var mediaGrantPaths = BuildMediaGrantSet(grants);
        if (grantedPaths.Count == 0 && mediaGrantPaths.Count == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();
        preemption.SearchStarted();
        try
        {
            ReconcilePending(grants);

            var textHits = new List<TextSearchHit>();
            if (grantedPaths.Count > 0)
            {
                textHits.AddRange(await FindTextAsync(query, grantedPaths, grants, cancellationToken).ConfigureAwait(false));
            }

            var ranked = new List<TextSearchHit>(textHits.Take(RecallIndexCaps.GroupCap));
            if (mediaGrantPaths.Count > 0)
            {
                var present = new HashSet<string>(textHits.Select(h => h.SourcePath), StringComparer.OrdinalIgnoreCase);
                foreach (var hit in SearchMediaWords(query, mediaGrantPaths, RecallIndexCaps.GroupCap, cancellationToken))
                {
                    if (!present.Add(hit.SourcePath))
                    {
                        continue;
                    }

                    ranked.Add(hit);
                }
            }

            return ranked;
        }
        finally
        {
            preemption.SearchFinished();
        }
    }

    private async Task<IReadOnlyList<TextSearchHit>> FindTextAsync(
        string query,
        HashSet<string> grantedPaths,
        IReadOnlyList<ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default)
    {
        var hash = encoderHash.TextHash ?? string.Empty;
        var filter = TextSearchFilter.Build(grantedPaths, hash);
        var prefixed = EmbeddingTaskPrefixes.WrapSearchQuery(query);
        var full768 = await encoder.EmbedPrefixedTextAsync(prefixed, cancellationToken).ConfigureAwait(false);
        var storedWidth = (int)indexPolicy.TextStoredWidth;
        var query512 = MatryoshkaVectorMath.CutAndNormalize(full768.Span, storedWidth);
        var bm25Only = cursors.ListBm25Only();
        var useInDbRrf = bm25Only.Count == 0;
        var request = new TextSearchRequest(query, query512, grantedPaths, hash, filter, useInDbRrf, RecallIndexCaps.RawListCap);
        IReadOnlyList<TextSearchHit> ranked;
        if (useInDbRrf)
        {
            ranked = index.Search(request, cancellationToken);
        }
        else
        {
            var denseRequest = request with { UseInDatabaseRrf = false };
            var dense = index.Search(denseRequest, cancellationToken);
            var ftsRequest = denseRequest with { QueryVector512 = ReadOnlyMemory<float>.Empty };
            var bm25Hits = index.Search(ftsRequest, cancellationToken);
            var mergedBm25 = Bm25OnlyRank.Merge(bm25Hits, bm25Only, query, grants);
            ranked = HybridFusion.Fuse(dense, mergedBm25);
        }

        var byPath = new Dictionary<string, List<TextSearchHit>>(StringComparer.OrdinalIgnoreCase);
        foreach (var hit in ranked)
        {
            var aliases = cursors.ListLiveAliases(hit.ContentId);
            var expandedHits = aliases.Count > 0
                ? SearchHitMarks.Expand(hit, aliases)
                : [hit];
            foreach (var expanded in expandedHits)
            {
                if (!grantedPaths.Contains(expanded.GrantPath))
                {
                    continue;
                }

                if (!byPath.TryGetValue(expanded.SourcePath, out var list))
                {
                    list = [];
                    byPath[expanded.SourcePath] = list;
                }

                list.Add(expanded);
            }
        }

        return byPath.Values
            .Select(Collapse.MaxRrf)
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.Ordinal)
            .Take(RecallIndexCaps.GroupCap)
            .ToList();
    }

    internal IReadOnlyList<TextSearchHit> SearchMediaWords(
        string query,
        IReadOnlySet<string> mediaGrantPaths,
        CancellationToken cancellationToken) =>
        SearchMediaWords(query, mediaGrantPaths, RecallIndexCaps.FileCap, cancellationToken);

    internal IReadOnlyList<TextSearchHit> SearchMediaWords(
        string query,
        IReadOnlySet<string> mediaGrantPaths,
        int fileBudget,
        CancellationToken cancellationToken)
    {
        if (fileBudget <= 0)
        {
            return [];
        }

        var mediaQuery = string.Join(' ', KeywordFold.Tokens(KeywordFold.Fold(query)));
        var lists = new List<IReadOnlyList<MediaSearchHit>>();
        foreach (var collection in MediaCollections.WordCollections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hash = MediaCollections.EncoderHashFor(collection, encoderHash);
            var mediaFilter = TextSearchFilter.Build(mediaGrantPaths, hash);
            if (mediaFilter.Length == 0)
            {
                continue;
            }

            var mediaHits = media.SearchWords(collection, mediaQuery, mediaFilter, RecallIndexCaps.RawListCap, cancellationToken);
            lists.Add(mediaHits
                .GroupBy(hit => hit.SourcePath, StringComparer.OrdinalIgnoreCase)
                .Select(group => Collapse.EarliestStart(group.ToList()))
                .ToList());
        }

        var winners = AgeRrf.Fuse(lists).Take(fileBudget).ToList();
        var hits = new List<TextSearchHit>(winners.Count);
        foreach (var hit in winners)
        {
            hits.Add(new TextSearchHit(
                hit.ContentId,
                hit.SourcePath,
                Path.GetFileName(hit.SourcePath),
                LastWriteUtc(hit.SourcePath),
                TextMatchReason.WordsOnMedia,
                hit.Score,
                0,
                "",
                "",
                false,
                hit.StartSeconds));
        }

        return hits;
    }

    private static DateTimeOffset LastWriteUtc(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (info.Exists)
            {
                return new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);
            }
        }
        catch (IOException)
        {
            return DateTimeOffset.UnixEpoch;
        }
        catch (UnauthorizedAccessException)
        {
            return DateTimeOffset.UnixEpoch;
        }

        return DateTimeOffset.UnixEpoch;
    }

    private static HashSet<string> BuildGrantedPathSet(IReadOnlyList<ScopeFolderGrant> grants)
    {
        return grants
            .Where(grant => grant.EnabledKinds.HasFlag(GrantFileKinds.Text) || grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
            .Select(grant => grant.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> BuildMediaGrantSet(IReadOnlyList<ScopeFolderGrant> grants)
    {
        return grants
            .Where(grant => grant.EnabledKinds.HasFlag(GrantFileKinds.Audio)
                || grant.EnabledKinds.HasFlag(GrantFileKinds.Video)
                || grant.EnabledKinds.HasFlag(GrantFileKinds.Image))
            .Select(grant => grant.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveGrantPath(IReadOnlyList<ScopeFolderGrant> grants) =>
        grants.FirstOrDefault()?.Path ?? string.Empty;

}

``n
## src/Recall.UI/Pages/Find/Home.razor.cs

``csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Net;
using System.Text;
using Recall.Core.Age;
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Text;
using Recall.UI.Components.Find;
using Recall.UI.Localization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Recall.UI.Pages;

#pragma warning disable S8949 // Only the audio file picker uses audioPickCts; other awaits are unrelated.
public partial class Home
{
  private const string PictureWaitingKey = "Search_PictureWaiting";
  private const string AudioWaitingKey = "Search_AudioWaiting";
  private const string WorkingKey = "Search_Working";
  private const string TimedOutKey = "Search_TimedOut";
  private const int HoldMilliseconds = 500;

  private static readonly string PictureAccept = string.Join(",", DatasetFolderSkip.ImageExtensions);

  private static readonly string[] AudioAcceptExtensions =
      MediaFileKinds.AudioAccept.Split(',', StringSplitOptions.RemoveEmptyEntries);

  private static bool ShowPictureControl =>
      RecallText.HasValue(PictureWaitingKey, CultureInfo.GetCultureInfo("en"))
      && RecallText.HasValue(PictureWaitingKey, CultureInfo.GetCultureInfo("ar"));

  private static bool ShowAudioControl =>
      RecallText.HasValue(AudioWaitingKey, CultureInfo.GetCultureInfo("en"))
      && RecallText.HasValue(AudioWaitingKey, CultureInfo.GetCultureInfo("ar"));

  [Inject]
  private ITextSearchService Search { get; set; } = null!;

  [Inject]
  private ILocalFileOpener Opener { get; set; } = null!;

  [Inject]
  private IRemoteFileSkipReport RemoteSkips { get; set; } = null!;

  [Inject]
  private IDatasetSkipReport DatasetSkips { get; set; } = null!;

  [Inject]
  private IPictureSearchService Pictures { get; set; } = null!;

  [Inject]
  private IAudioSearchService Audios { get; set; } = null!;

  [Inject]
  private IAudioSource AudioSource { get; set; } = null!;

  [Inject]
  private IAudioOffReport AudioOff { get; set; } = null!;

  [Inject]
  private IModelDownloadService ModelDownloads { get; set; } = null!;

  [Inject]
  private PinService Pins { get; set; } = null!;

  [Inject]
  private IIngestCursorStore Cursors { get; set; } = null!;

  [Inject]
  private IGrantFileUrl GrantUrls { get; set; } = null!;

  [Inject]
  private IFolderReveal FolderReveal { get; set; } = null!;

  private string? query;
  private byte[]? pickedPicture;
  private string? lensStatusKey;
  private IReadOnlyList<MediaSearchHit> pictureHits = [];
  private bool pictureDownloadStarted;
  private float[]? pickedAudio;
  private string? pickedAudioName;
  private string? audioLensStatusKey;
  private IReadOnlyList<MediaSearchHit> audioHits = [];
  private bool audioDownloadStarted;
  private int audioPickGeneration;
  private int pictureGeneration;
  private CancellationTokenSource? audioPickCts;
  private CancellationTokenSource? findCts;
  private string? faultKey;
  private TextFindView textView = TextFindView.Empty;
  private bool hasSearched;
  private bool findWorking;
  private TextFindCard? menuCard;
  private CancellationTokenSource? holdCts;
  private bool modalVisible;
  private TextFindCard? modalCard;
  private string? modalMediaUrl;

  private IReadOnlyList<TextFindCard> DisplayCards
  {
    get
    {
      var list = new List<TextFindCard>(textView.Cards);
      list.AddRange(TextFindSession.BuildMedia(pictureHits, Opener.Exists));
      list.AddRange(TextFindSession.BuildMedia(audioHits, Opener.Exists));
      return list;
    }
  }

  private IReadOnlyList<SearchResultGroup> ResultGroups => SearchGroups.Split(DisplayCards);

  private bool HasAnyResults => DisplayCards.Count > 0;

  private bool HasLensActive => pickedPicture is not null || pickedAudioName is not null;

  private bool ShowEmptyPrompt => !findWorking && !HasAnyResults && !hasSearched && !HasLensActive
      && lensStatusKey is null && audioLensStatusKey is null && faultKey is null;

  private bool ShowStatusLine => findWorking || lensStatusKey == TimedOutKey || audioLensStatusKey == TimedOutKey
      || lensStatusKey == WorkingKey || audioLensStatusKey == WorkingKey;

  private string? StatusLineKey => findWorking ? WorkingKey : lensStatusKey ?? audioLensStatusKey;

  private string? LensDataUri => pickedPicture is null ? null : "data:image/jpeg;base64," + Convert.ToBase64String(pickedPicture);

  private Task OnKeyDownAsync(KeyboardEventArgs args)
  {
    if (!string.Equals(args.Key, "Enter", StringComparison.Ordinal))
    {
      return Task.CompletedTask;
    }

    return SubmitAsync();
  }

  private async Task SubmitAsync()
  {
    if (!TextFindSession.ShouldQuery(query))
    {
      textView = TextFindView.Empty;
      faultKey = null;
      hasSearched = false;
      return;
    }

    await CancelFindAsync().ConfigureAwait(false);
    findCts = new CancellationTokenSource();
    var linked = CancellationTokenSource.CreateLinkedTokenSource(findCts.Token);
    linked.CancelAfter(SearchDeadline.FindTimeout);
    findWorking = true;
    menuCard = null;

    try
    {
      RemoteSkips.Clear();
      DatasetSkips.Clear();
      var hits = await Search.FindAsync(query!.Trim(), Ui.Current.Grants, linked.Token);
      textView = TextFindSession.Build(hits, Opener.Exists);
      faultKey = null;
      hasSearched = true;
    }
    catch (OperationCanceledException) when (findCts.IsCancellationRequested)
    {
      textView = TextFindView.Empty;
      faultKey = TimedOutKey;
      hasSearched = true;
    }
    catch (Exception ex)
    {
      faultKey = TextFindSession.FaultKey(ex);
      textView = TextFindView.Empty;
      hasSearched = true;
    }
    finally
    {
      findWorking = false;
      linked.Dispose();
    }
  }

  private async Task ClearPictureAsync()
  {
    pictureGeneration++;
    await CancelFindAsync().ConfigureAwait(false);
    pickedPicture = null;
    pictureHits = [];
    lensStatusKey = null;
    pictureDownloadStarted = false;
    menuCard = null;
    await CloseModalAsync();
  }

  private async Task ClearAudioAsync()
  {
    audioPickGeneration++;
    await CancelAudioPickAsync().ConfigureAwait(false);
    await CancelFindAsync().ConfigureAwait(false);
    pickedAudio = null;
    pickedAudioName = null;
    audioHits = [];
    audioLensStatusKey = null;
    audioDownloadStarted = false;
    menuCard = null;
    await CloseModalAsync();
  }

  [SuppressMessage("Major Code Smell", "S5693:Limit the content length of HTTP requests", Justification = "The picture read stream is capped at 32 MiB.")]
  private async Task OnPictureSelectedAsync(InputFileChangeEventArgs args)
  {
    var file = args.File;
    if (file is null || !DatasetFolderSkip.ImageExtensions.Contains(Path.GetExtension(file.Name), StringComparer.OrdinalIgnoreCase))
    {
      return;
    }

    var generation = ++pictureGeneration;
    audioPickGeneration++;
    await CancelAudioPickAsync().ConfigureAwait(false);
    pickedAudio = null;
    pickedAudioName = null;
    audioHits = [];
    audioLensStatusKey = null;
    await using var stream = file.OpenReadStream(maxAllowedSize: 32L * 1024 * 1024);
    using var buffer = new MemoryStream();
    await stream.CopyToAsync(buffer);
    if (generation != pictureGeneration)
    {
      return;
    }

    pickedPicture = buffer.ToArray();
    lensStatusKey = WorkingKey;
    findWorking = true;
    await RunPictureSearchAsync(generation);
  }

  [SuppressMessage("Major Code Smell", "S5693:Limit the content length of HTTP requests", Justification = "The audio read stream is capped at 1024 MiB.")]
  private async Task OnAudioSelectedAsync(InputFileChangeEventArgs args)
  {
    var file = args.File;
    if (file is null || !AudioAcceptExtensions.Contains(Path.GetExtension(file.Name), StringComparer.OrdinalIgnoreCase))
    {
      return;
    }

    pictureGeneration++;
    pickedPicture = null;
    pictureHits = [];
    lensStatusKey = null;
    pickedAudioName = file.Name;

    var generation = ++audioPickGeneration;
    await CancelAudioPickAsync().ConfigureAwait(false);
    audioPickCts = new CancellationTokenSource();
    var token = audioPickCts.Token;

    var bytes = new byte[file.Size];
    try
    {
      await using var stream = file.OpenReadStream(maxAllowedSize: 1024L * 1024 * 1024, cancellationToken: token);
      var offset = 0;
      while (offset < bytes.Length)
      {
        var read = await stream.ReadAsync(bytes.AsMemory(offset), token).ConfigureAwait(false);
        if (read == 0)
        {
          break;
        }

        offset += read;
      }
    }
    catch (OperationCanceledException)
    {
      return;
    }

    if (generation != audioPickGeneration)
    {
      return;
    }

    try
    {
      var pcm = await Task.Run(
          () =>
              WavPcm.IsWav(bytes)
                  ? WavPcm.Read(bytes)
                  : AudioSource.ReadCompressed(bytes),
          token).ConfigureAwait(false);
      if (generation != audioPickGeneration)
      {
        return;
      }

      pickedAudio = pcm;
    }
    catch (OperationCanceledException)
    {
      return;
    }
    catch (Exception)
    {
      if (generation != audioPickGeneration)
      {
        return;
      }

      pickedAudio = null;
      audioLensStatusKey = "Search_Fault";
      audioDownloadStarted = false;
      return;
    }

    audioLensStatusKey = WorkingKey;
    findWorking = true;
    await RunAudioSearchAsync(generation, token).ConfigureAwait(false);
  }

  private async Task RunAudioSearchAsync(int generation, CancellationToken cancellationToken)
  {
    if (pickedAudio is null || generation != audioPickGeneration)
    {
      return;
    }

    audioHits = [];
    await CancelFindAsync().ConfigureAwait(false);
    findCts = new CancellationTokenSource();
    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, findCts.Token);
    linked.CancelAfter(SearchDeadline.FindTimeout);

    var decision = AudioQueryGate.Decide(ModelDownloads.GetAudioDownloadSnapshot().Phase);
    if (decision == PictureQueryDecision.Wait)
    {
      audioLensStatusKey = AudioWaitingKey;
      findWorking = false;
      if (!audioDownloadStarted)
      {
        audioDownloadStarted = true;
        try
        {
          await ModelDownloads.DownloadAudioModelAsync(null, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
          audioDownloadStarted = false;
          return;
        }
        catch (Exception)
        {
          if (generation != audioPickGeneration)
          {
            return;
          }

          audioLensStatusKey = "Search_AudioFailed";
          audioDownloadStarted = false;
          return;
        }
      }

      decision = AudioQueryGate.Decide(ModelDownloads.GetAudioDownloadSnapshot().Phase);
    }

    if (generation != audioPickGeneration)
    {
      return;
    }

    if (decision == PictureQueryDecision.Wait)
    {
      return;
    }

    if (decision == PictureQueryDecision.Fail)
    {
      audioLensStatusKey = "Search_AudioFailed";
      findWorking = false;
      audioDownloadStarted = false;
      return;
    }

    try
    {
      findWorking = true;
      audioLensStatusKey = WorkingKey;
      var hits = await Audios.FindAsync(pickedAudio, Ui.Current.Grants, linked.Token).ConfigureAwait(false);
      if (generation != audioPickGeneration)
      {
        return;
      }

      audioHits = hits;
      audioLensStatusKey = hits.Count == 0 ? "Search_NoHitsLead" : null;
    }
    catch (OperationCanceledException)
    {
      if (generation != audioPickGeneration)
      {
        return;
      }

      audioHits = [];
      audioLensStatusKey = TimedOutKey;
      audioDownloadStarted = false;
    }
    catch (Exception)
    {
      if (generation != audioPickGeneration)
      {
        return;
      }

      audioLensStatusKey = "Search_Fault";
      audioDownloadStarted = false;
    }
    finally
    {
      findWorking = false;
      linked.Dispose();
    }
  }

  private async Task RunPictureSearchAsync(int generation)
  {
    if (pickedPicture is null || generation != pictureGeneration)
    {
      return;
    }

    pictureHits = [];
    await CancelFindAsync().ConfigureAwait(false);
    findCts = new CancellationTokenSource();
    findCts.CancelAfter(SearchDeadline.FindTimeout);

    var decision = PictureQueryGate.Decide(ModelDownloads.GetVisionDownloadSnapshot().Phase);
    if (decision == PictureQueryDecision.Wait)
    {
      lensStatusKey = PictureWaitingKey;
      findWorking = false;
      if (!pictureDownloadStarted)
      {
        pictureDownloadStarted = true;
        try
        {
          await ModelDownloads.DownloadVisionModelAsync(null);
        }
        catch (Exception)
        {
          if (generation != pictureGeneration)
          {
            return;
          }

          lensStatusKey = "Search_PictureFailed";
          return;
        }
      }

      if (generation != pictureGeneration || pickedPicture is null)
      {
        return;
      }

      decision = PictureQueryGate.Decide(ModelDownloads.GetVisionDownloadSnapshot().Phase);
    }

    if (generation != pictureGeneration || pickedPicture is null)
    {
      return;
    }

    if (decision == PictureQueryDecision.Wait)
    {
      return;
    }

    if (decision == PictureQueryDecision.Fail)
    {
      lensStatusKey = "Search_PictureFailed";
      findWorking = false;
      return;
    }

    try
    {
      findWorking = true;
      lensStatusKey = WorkingKey;
      var hits = await Pictures.FindAsync(pickedPicture, Ui.Current.Grants, findCts.Token);
      if (generation != pictureGeneration)
      {
        return;
      }

      pictureHits = hits;
      lensStatusKey = hits.Count == 0 ? "Search_NoHitsLead" : null;
    }
    catch (OperationCanceledException)
    {
      if (generation != pictureGeneration)
      {
        return;
      }

      pictureHits = [];
      lensStatusKey = TimedOutKey;
    }
    catch (Exception)
    {
      if (generation != pictureGeneration)
      {
        return;
      }

      lensStatusKey = "Search_Fault";
    }
    finally
    {
      findWorking = false;
    }
  }

  private void OpenMenu(TextFindCard card) => menuCard = card;

  private void CloseMenu() => menuCard = null;

  private void OnCardPointerDown(TextFindCard card)
  {
    _ = CancelHoldAsync();
    holdCts = new CancellationTokenSource();
    var token = holdCts.Token;
    _ = Task.Run(
        async () =>
        {
          try
          {
            await Task.Delay(HoldMilliseconds, token).ConfigureAwait(false);
            await InvokeAsync(() =>
            {
              if (token.IsCancellationRequested)
              {
                return;
              }

              menuCard = card with { };
              StateHasChanged();
            }).ConfigureAwait(false);
          }
          catch (OperationCanceledException)
          {
            // Hold cancelled before the menu opened.
          }
        },
        token);
  }

  private void OnCardPointerUp() => _ = CancelHoldAsync();

  private async Task OpenCardAsync(TextFindCard card)
  {
    CloseMenu();
    if (card.Unavailable)
    {
      return;
    }

    var result = MediaHitStart.Seeks(card.StartSeconds)
        ? await Opener.OpenAtAsync(card.SourcePath, card.StartSeconds)
        : await Opener.OpenAsync(card.SourcePath);
    if (result == LocalFileOpenResult.Failed)
    {
      faultKey = TextFindSession.OpenFaultKey(result);
    }
    else if (result == LocalFileOpenResult.Missing)
    {
      MarkUnavailable(card);
    }
  }

  private Task RevealCardAsync(TextFindCard card)
  {
    CloseMenu();
    if (!FolderReveal.Reveal(card.SourcePath))
    {
      faultKey = "Search_OpenPathUnsupported";
    }

    return Task.CompletedTask;
  }

  private async Task ViewOrPlayAsync(TextFindCard card)
  {
    CloseMenu();
    if (card.Unavailable)
    {
      return;
    }

    modalCard = card;
    modalMediaUrl = GrantUrls.Build(card.SourcePath, Ui.Current.Grants);
    modalVisible = true;
    await InvokeAsync(StateHasChanged);
  }

  private async Task CloseModalAsync()
  {
    modalVisible = false;
    modalCard = null;
    modalMediaUrl = null;
    await InvokeAsync(StateHasChanged);
  }

  private void MarkUnavailable(TextFindCard card)
  {
    var unavailable = card with { Unavailable = true };
    textView = new TextFindView(
        textView.Cards.Select(c => string.Equals(c.SourcePath, card.SourcePath, StringComparison.OrdinalIgnoreCase) ? unavailable : c).ToList(),
        textView.HasHits);
  }

  private void PinKeep(TextFindCard card)
  {
    CloseMenu();
    if (card.Unavailable || !card.ShowPin)
    {
      return;
    }

    if (Cursors.IsPinned(card.SourcePath))
    {
      Pins.Unpin(card.SourcePath);
      return;
    }

    Pins.Pin(card.SourcePath);
  }

  private static bool IsTextGroupCard(TextFindCard card) =>
      TextFindCardKind.KindFromPath(card.SourcePath) == SearchGroupKind.Text;

  private MarkupString ExcerptMarkup(TextFindCard card)
  {
    if (string.IsNullOrEmpty(card.RawNote) || string.IsNullOrWhiteSpace(query))
    {
      return (MarkupString)WebUtility.HtmlEncode(card.RawNote);
    }

    var marks = Bm25Highlight.Marks(card.RawNote, query.Trim());
    if (marks.Count == 0)
    {
      return (MarkupString)WebUtility.HtmlEncode(card.RawNote);
    }

    var ordered = marks.OrderBy(m => m.Start).ToList();
    var builder = new StringBuilder();
    var cursor = 0;
    foreach (var mark in ordered)
    {
      if (mark.Start < cursor)
      {
        continue;
      }

      builder.Append(WebUtility.HtmlEncode(card.RawNote[cursor..mark.Start]));
      builder.Append("<mark>");
      builder.Append(WebUtility.HtmlEncode(card.RawNote.Substring(mark.Start, mark.Length)));
      builder.Append("</mark>");
      cursor = mark.Start + mark.Length;
    }

    builder.Append(WebUtility.HtmlEncode(card.RawNote[cursor..]));
    return (MarkupString)builder.ToString();
  }

  private static string? ThumbnailUrl(IGrantFileUrl grantUrls, TextFindCard card, IReadOnlyList<ScopeFolderGrant> grants)
  {
    if (IsTextGroupCard(card) || card.Unavailable)
    {
      return null;
    }

    return grantUrls.Build(card.SourcePath, grants);
  }

  private async Task CancelAudioPickAsync()
  {
    if (audioPickCts is null)
    {
      return;
    }

    await audioPickCts.CancelAsync().ConfigureAwait(false);
    audioPickCts.Dispose();
    audioPickCts = null;
  }

  private async Task CancelFindAsync()
  {
    if (findCts is null)
    {
      return;
    }

    await findCts.CancelAsync().ConfigureAwait(false);
    findCts.Dispose();
    findCts = null;
  }

  private async Task CancelHoldAsync()
  {
    if (holdCts is null)
    {
      return;
    }

    await holdCts.CancelAsync().ConfigureAwait(false);
    holdCts.Dispose();
    holdCts = null;
  }
}
``n

## tests/Recall.UnitTests/LastRunRepairTests.cs

``csharp
using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Faults;
using Recall.Core.Ingest;
using Recall.Core.Preferences;
using Recall.Core.Runtime;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.UI.Pages;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class LastRunRepairTests
{
    private static string NewDatabase() =>
        Path.Combine(Path.GetTempPath(), "recall-last-run-" + Guid.NewGuid().ToString("N") + ".db");

    [Fact]
    public void Decide_FinishedSameHash_IsSkip() =>
        IngestCursorPolicy.Decide(new IngestFileRow("a", "h1", IngestCursorState.Finished, "g", ""), "h1", false)
            .Should().Be(IngestCursorDecision.Skip);

    [Fact]
    public void Decide_ReadingSameHashWithChunks_IsSkip() =>
        IngestCursorPolicy.Decide(new IngestFileRow("a", "h1", IngestCursorState.Reading, "g", ""), "h1", true)
            .Should().Be(IngestCursorDecision.Skip);

    [Fact]
    public void Decide_ReadingSameHashNoChunks_IsRead() =>
        IngestCursorPolicy.Decide(new IngestFileRow("a", "h1", IngestCursorState.Reading, "g", ""), "h1", false)
            .Should().Be(IngestCursorDecision.Read);

    [Fact]
    public void Decide_BlankHash_ThrowsArgumentException()
    {
        Action act = () => IngestCursorPolicy.Decide(null, "  ", false);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Leftover_Chunks_Finishes() =>
        LeftoverPolicy.Decide(true, true).Should().Be(LeftoverAction.Finish);

    [Fact]
    public void Leftover_MissingFile_IsMissing() =>
        LeftoverPolicy.Decide(false, false).Should().Be(LeftoverAction.Missing);

    [Fact]
    public void Leftover_PresentWithoutChunks_Pauses() =>
        LeftoverPolicy.Decide(false, true).Should().Be(LeftoverAction.Pause);

    [Fact]
    public void PauseOnlyReading_Finished_StaysFinished() =>
        IngestCancellation.PauseOnlyReading(IngestCursorState.Finished).Should().Be(IngestCursorState.Finished);

    [Fact]
    public void PauseOnlyReading_Reading_BecomesPaused() =>
        IngestCancellation.PauseOnlyReading(IngestCursorState.Reading).Should().Be(IngestCursorState.Paused);

    [Fact]
    public void MarkStopped_WritesPaused()
    {
        var store = new SqliteIngestCursorStore(NewDatabase());
        store.MarkReading("/g/a", "hash", "/g");
        store.MarkStopped("/g/a", IngestStopKeys.MemoryBudget);
        var row = store.GetFile("/g/a") ?? throw new InvalidOperationException("row");
        row.State.Should().Be(IngestCursorState.Paused);
    }

    [Fact]
    public void MarkStopped_FinishedRow_StaysFinished()
    {
        var store = new SqliteIngestCursorStore(NewDatabase());
        store.FinishPath("/g/a", "hash", "/g", "enc", [new ChunkWrite("id0", 0)]);
        store.MarkStopped("/g/a", IngestStopKeys.MemoryBudget);
        var row = store.GetFile("/g/a") ?? throw new InvalidOperationException("row");
        row.State.Should().Be(IngestCursorState.Finished);
    }

    [Fact]
    public void FinishExisting_NoChunks_ThrowsInvalidOperationException()
    {
        var store = new SqliteIngestCursorStore(NewDatabase());
        store.MarkReading("/g/a", "hash", "/g");
        Action act = () => store.FinishExisting("/g/a");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsSeen_IncludesPaused() =>
        IngestKindCountPolicy.IsSeen(IngestCursorState.Paused).Should().BeTrue();

    [Fact]
    public void IsFinished_Paused_IsFalse() =>
        IngestKindCountPolicy.IsFinished(IngestCursorState.Paused).Should().BeFalse();

    [Fact]
    public void Percent_TwoFinishedOfTwo_Stays100AfterAnotherFilePauses()
    {
        var store = new FakeIngestCursorStore();
        store.MarkFinished("/g/a.txt", "h1", "/g", "e");
        store.MarkFinished("/g/b.txt", "h2", "/g", "e");
        store.MarkReading("/g/c.txt", "h3", "/g");
        store.MarkStopped("/g/c.txt", IngestStopKeys.Interrupted);
        var count = store.Count(GrantFileKinds.Text);
        count.Finished.Should().Be(2);
        count.Seen.Should().Be(3);
        count.Percent.Should().Be(66);
    }

    [Fact]
    public void Gpu_Null_DoesNotBlock() => GpuCooldownPolicy.Blocks(null).Should().BeFalse();

    [Fact]
    public void Gpu_80_DoesNotBlock() => GpuCooldownPolicy.Blocks(80).Should().BeFalse();

    [Fact]
    public void Gpu_81_Blocks() => GpuCooldownPolicy.Blocks(81).Should().BeTrue();

    [Fact]
    public void Retry_FiveMinutes_IsNext()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        IngestRetryClock.Next(start, 5).Should().Be(start.AddMinutes(5));
    }

    [Fact]
    public void ShouldRun_BeforeNext_IsFalse()
    {
        var next = DateTimeOffset.UtcNow.AddMinutes(5);
        IngestRetryClock.ShouldRun(DateTimeOffset.UtcNow, next, true, false, true).Should().BeFalse();
    }

    [Fact]
    public void ShouldRun_GateClosed_IsFalse() =>
        IngestRetryClock.ShouldRun(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(-1), false, false, true).Should().BeFalse();

    [Fact]
    public void ShouldRun_GpuHot_IsFalse() =>
        IngestRetryClock.ShouldRun(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(-1), true, true, true).Should().BeFalse();

    [Fact]
    public void ShouldRun_ModelNotReady_IsFalse() =>
        IngestRetryClock.ShouldRun(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(-1), true, false, false).Should().BeFalse();

    [Fact]
    public void ShouldRun_OpenAndCool_IsTrue() =>
        IngestRetryClock.ShouldRun(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(-1), true, false, true).Should().BeTrue();

    [Fact]
    public void Defer_AddsTheSameMinutes()
    {
        var now = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
        IngestRetryClock.Defer(now, 5).Should().Be(now.AddMinutes(5));
    }

    [Fact]
    public void AllowAutomatic_ThirdFailure_IsFalse() =>
        IngestRetryPolicy.AllowAutomatic(3).Should().BeFalse();

    [Fact]
    public void AllowAutomatic_Second_IsTrue() =>
        IngestRetryPolicy.AllowAutomatic(2).Should().BeTrue();

    [Fact]
    public void RetryDelay_0_Throws()
    {
        var service = new RetryDelayPreferenceService(new MemoryRetryDelayPreferenceStore());
        Action act = () => service.Set(0);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.RetryDelayNotAllowed);
    }

    [Fact]
    public void RetryDelay_61_Throws()
    {
        var service = new RetryDelayPreferenceService(new MemoryRetryDelayPreferenceStore());
        Action act = () => service.Set(61);
        act.Should().Throw<RecallFaultException>().Which.Fault.Should().Be(RecallFault.RetryDelayNotAllowed);
    }

    [Fact]
    public void RetryDelay_5_Stores5()
    {
        var store = new MemoryRetryDelayPreferenceStore();
        var service = new RetryDelayPreferenceService(store);
        service.Set(5);
        store.Get().Should().Be(5);
    }

    [Fact]
    public void RetryDelay_60_Stores60()
    {
        var store = new MemoryRetryDelayPreferenceStore();
        var service = new RetryDelayPreferenceService(store);
        service.Set(60);
        store.Get().Should().Be(60);
    }

    [Fact]
    public void Promote_AbsentMatchingHash_InsertsPathAtZero()
    {
        var hash = "abc";
        var rows = new[] { new IngestFileRow("/g/pic.jpg", hash, IngestCursorState.Finished, "/g", "enc") };
        var hits = new List<MediaSearchHit> { new("x", "/g/other.jpg", 0, 0, 0.5, 0.5) };
        var promoted = PictureExactFile.Promote(hits, hash, rows);
        promoted[0].SourcePath.Should().Be("/g/pic.jpg");
        promoted[0].Similarity.Should().Be(1);
    }

    [Fact]
    public void Promote_NoMatch_SameOrder()
    {
        var hits = new List<MediaSearchHit>
        {
            new("a", "/g/a.jpg", 0, 0, 0.5, 0.5),
            new("b", "/g/b.jpg", 0, 0, 0.4, 0.4),
        };
        PictureExactFile.Promote(hits, "missing", []).Select(h => h.SourcePath)
            .Should().Equal("/g/a.jpg", "/g/b.jpg");
    }

    [Fact]
    public void Promote_AlreadyFirst_StaysFirst()
    {
        var hash = "abc";
        var rows = new[] { new IngestFileRow("/g/pic.jpg", hash, IngestCursorState.Finished, "/g", "enc") };
        var hits = new List<MediaSearchHit>
        {
            new("a", "/g/pic.jpg", 0, 0, 0.9, 0.9),
            new("b", "/g/b.jpg", 0, 0, 0.4, 0.4),
        };
        PictureExactFile.Promote(hits, hash, rows).Select(h => h.SourcePath)
            .Should().Equal("/g/pic.jpg", "/g/b.jpg");
    }

    [Fact]
    public void Marks_ExactToken_ReturnsSpan()
    {
        var marks = Bm25Highlight.Marks("hello world", "world");
        marks.Should().ContainSingle();
        marks[0].Start.Should().Be(6);
        marks[0].Length.Should().Be(5);
    }

    [Fact]
    public void Marks_NoToken_Empty() => Bm25Highlight.Marks("hello", "zzz").Should().BeEmpty();

    [Fact]
    public void Marks_EmptyQuery_Empty() => Bm25Highlight.Marks("hello", "").Should().BeEmpty();

    [Fact]
    public void MatchStrength_One_Is100() => MatchStrength.Percent(1).Should().Be(100);

    [Fact]
    public void MatchStrength_Half_Is50() => MatchStrength.Percent(0.5).Should().Be(50);

    [Fact]
    public void MatchStrength_AboveOne_IsNull() => MatchStrength.Percent(1.1).Should().BeNull();

    [Fact]
    public void MatchStrength_Negative_IsNull() => MatchStrength.Percent(-0.1).Should().BeNull();

    [Fact]
    public void Split_OmitsEmptyVideo()
    {
        var cards = new List<TextFindCard> { new("/a.txt", "a.txt", "Search_Kind_Text", "", "Search_Match_Words", false) };
        SearchGroups.Split(cards).Select(g => g.GroupKey).Should().NotContain("Search_Group_Video");
    }

    [Fact]
    public void Split_CodeStaysInText()
    {
        var cards = new List<TextFindCard> { new("/a.cs", "a.cs", "Search_Kind_Code", "", "Search_Match_Words", false) };
        SearchGroups.Split(cards)[0].GroupKey.Should().Be("Search_Group_Text");
    }

    [Fact]
    public void Split_OrderIsTextImageVideoAudio()
    {
        var cards = new List<TextFindCard>
        {
            new("/a.mp3", "a.mp3", "Search_Kind_Audio", "", "", false),
            new("/a.jpg", "a.jpg", "Search_Kind_Image", "", "", false),
            new("/a.txt", "a.txt", "Search_Kind_Text", "", "Search_Match_Words", false),
            new("/a.mp4", "a.mp4", "Search_Kind_Video", "", "", false),
        };
        SearchGroups.Split(cards).Select(g => g.GroupKey).Should().ContainInOrder(
            "Search_Group_Text", "Search_Group_Image", "Search_Group_Video", "Search_Group_Audio");
    }

    [Fact]
    public void Build_ImagePath_KindIsImage()
    {
        var hits = new List<TextSearchHit>
        {
            new("id", "/g/pic.jpg", "pic.jpg", DateTimeOffset.UtcNow, TextMatchReason.FullText, 0.8),
        };
        TextFindSession.Build(hits, _ => true).Cards[0].KindKey.Should().Be("Search_Kind_Image");
    }

    [Fact]
    public void Build_VideoWords_ReasonIsWordsOnMedia()
    {
        var hits = new List<TextSearchHit>
        {
            new("id", "/g/v.mp4", "v.mp4", DateTimeOffset.UtcNow, TextMatchReason.WordsOnMedia, 0.5),
        };
        TextFindSession.Build(hits, _ => true).Cards[0].ReasonKey.Should().Be("Search_Match_WordsOnMedia");
    }

    [Fact]
    public void Build_FullText_IsNotMeaningAndWords()
    {
        var hits = new List<TextSearchHit>
        {
            new("id", "/g/a.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.FullText, 0.5),
        };
        TextFindSession.Build(hits, _ => true).Cards[0].ReasonKey.Should().Be("Search_Match_Words");
    }

    [Fact]
    public void Build_UnixEpoch_TimeIsEmpty()
    {
        var hits = new List<TextSearchHit>
        {
            new("id", "/g/a.txt", "a.txt", DateTimeOffset.UnixEpoch, TextMatchReason.FullText, 0.5),
        };
        TextFindSession.Build(hits, _ => true).Cards[0].LocalTime.Should().BeEmpty();
    }

    [Fact]
    public void ResultAction_Image_IsView() => ResultActionPolicy.Decide("/a.jpg").Should().Be(ResultActionKind.View);

    [Fact]
    public void ResultAction_Video_IsPlay() => ResultActionPolicy.Decide("/a.mp4").Should().Be(ResultActionKind.Play);

    [Fact]
    public void ResultAction_Text_IsOpenOnly() => ResultActionPolicy.Decide("/a.txt").Should().Be(ResultActionKind.OpenOnly);

    [Fact]
    public void HomeRazor_HidesEmptyTitleWhenResultsExist()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Recall.UI", "Pages", "Find", "Home.razor");
        var text = File.ReadAllText(path);
        text.Should().Contain("ShowEmptyPrompt");
        text.Should().NotContain("@if (!TextFindSession.ShouldQuery(query) || !hasSearched)");
    }

    [Fact]
    public void HomeRazor_DoesNotCallPreviewPlay()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Recall.UI", "Pages", "Find", "Home.razor.cs");
        File.ReadAllText(path).Should().NotContain("Preview.PlayAsync");
    }

    [Fact]
    public void HomeRazor_PicturePickStartsFind()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Recall.UI", "Pages", "Find", "Home.razor.cs");
        var text = File.ReadAllText(path);
        text.Should().Contain("RunPictureSearchAsync");
        text.Should().Contain("OnPictureSelectedAsync");
    }

    [Fact]
    public void OpenAtAsync_DoesNotCallPlay()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Recall.Maui", "Platform", "Files", "MauiLocalFileOpener.cs");
        File.ReadAllText(path).Should().NotContain("PlayAsync");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Recall.slnx was not found above the test output directory.");
    }
}
``n
