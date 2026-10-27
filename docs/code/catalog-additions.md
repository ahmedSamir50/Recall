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
if (ShouldSkipPath(path))
if (pending.Count == 0)
if (path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
if (pageTexts.Count == 0)
if (string.IsNullOrEmpty(text))
if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
if (pageIndex > 0)
if (!File.Exists(path))
if (!string.IsNullOrEmpty(pageText))
if (row.Retired || row.Missing || row.Ordinal != 0)
if (matches == 1)
if (pageTexts.Count == 0 || row is null)
if (boxes.Count == 0)
if (results.Count != 1)
if (x < canvas.ContentWidth)
if (loaded)
if (int.TryParse(threadText, out var threads) && threads > 0)
```

## src/Recall.Maui/Platform/Ocr/WindowsPdfPageRenderer.cs

```csharp
WindowsPdfPageRenderer
if (pageNumber < 1)
if (pageNumber > document.PageCount)
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
if (!File.Exists(pdfPath) || !File.Exists(gtPath))
if (!File.Exists(imagePath) || !File.Exists(gtPath))
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

