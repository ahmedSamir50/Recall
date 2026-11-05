#if WINDOWS
using System.Diagnostics;
using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ocr;
using Recall.Core.Ports;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.IntegrationTests.Infrastructure;
using Recall.Maui.Platform.Ocr;
using Recall.UnitTests.Fakes;

namespace Recall.IntegrationTests;

public sealed class OcrGoldenReportTests
{
    public static readonly TimeSpan GoldenTimeCeiling = TimeSpan.FromMinutes(5);

    private const double CleanCeiling = 0.15;
    private const double PhoneCeiling = 0.15;
    private const int GoldenScanPages = 80;
    private const int GoldenPhonePages = 4;

    [Fact]
    public async Task OcrGolden_Page28_ArabicSlice_CerWithinCleanCeiling()
    {
        var pages = OcrGoldenCatalog.Build();
        pages[28].Language.Should().Be("ar", "page 28 is the Arabic slice the manifest orders first");
        await AssertGoldenPageCerAsync(28, CleanCeiling);
    }

    [Fact]
    public async Task OcrGolden_EightyPages_CerWithinCeiling()
    {
        Environment.SetEnvironmentVariable("RECALL_OCR_PROGRESS", "1");
        OcrRunProgress.Message("golden catalog build start");
        var pages = OcrGoldenCatalog.Build();
        OcrRunProgress.Message($"golden catalog build done pages={pages.Count}");
        pages.Count(page => !page.IsPhone).Should().Be(GoldenScanPages, "the golden gate covers eighty real scan pages");
        pages.Count(page => page.IsPhone).Should().Be(GoldenPhonePages, "the four demo phone captures keep the phone ceiling on real photos");

        var tempDir = Path.Combine(Path.GetTempPath(), $"recall-golden-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var paths = new List<string>(pages.Count);
        try
        {
            for (var index = 0; index < pages.Count; index++)
            {
                var page = pages[index];
                var path = Path.Combine(tempDir, $"page-{index:D3}.png");
                await File.WriteAllBytesAsync(path, page.Png, TestContext.Current.CancellationToken);
                paths.Add(path);
            }

            var cursors = new FakeIngestCursorStore();
            var media = new FakeMediaVectorIndex();
            OcrRunProgress.Message("ocr run start");
            using var engine = CreateEngine(cursors, media);
            var watch = Stopwatch.StartNew();
            await engine.RunPassAsync(paths, TestContext.Current.CancellationToken);
            watch.Stop();
            OcrRunProgress.Message($"ocr run end elapsed={watch.ElapsedMilliseconds}ms; cer check start");

            var failures = new List<string>();
            for (var index = 0; index < pages.Count; index++)
            {
                var page = pages[index];
                var path = paths[index];
                engine.RecognizedByPath.Should().ContainKey(path);
                var hypothesis = engine.RecognizedByPath[path];
                var cer = Recall.Core.Ocr.OcrCharacterErrorRate.MeasureGolden(page.GroundTruth, hypothesis);
                var ceiling = page.IsPhone ? PhoneCeiling : CleanCeiling;
                OcrRunProgress.Message($"cer page {index} lang={page.Language} phone={page.IsPhone} rate={cer.Rate:F4} ceiling={ceiling:F2}");
                if (cer.Rate > ceiling)
                {
                    failures.Add($"page {index} ({page.Language}, phone={page.IsPhone}) CER {cer.Rate:P1} > {ceiling:P0}");
                }
            }

            OcrRunProgress.Message("golden cer check done");
            failures.Should().BeEmpty("every golden page stays inside its CER ceiling: " + string.Join("; ", failures));
            watch.Elapsed.Should().BeLessThan(GoldenTimeCeiling, $"the eighty-page gate must finish inside {GoldenTimeCeiling}");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private static async Task AssertGoldenPageCerAsync(int pageIndex, double ceiling)
    {
        var pages = OcrGoldenCatalog.Build();
        var page = pages[pageIndex];
        var tempDir = Path.Combine(Path.GetTempPath(), $"recall-golden-p{pageIndex}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, $"page-{pageIndex:D3}.png");
        await File.WriteAllBytesAsync(path, page.Png);
        try
        {
            var cursors = new FakeIngestCursorStore();
            var media = new FakeMediaVectorIndex();
            using var engine = CreateEngine(cursors, media);
            await engine.RunPassAsync([path], TestContext.Current.CancellationToken);
            engine.RecognizedByPath.Should().ContainKey(path);
            var hypothesis = engine.RecognizedByPath[path];
            var cer = Recall.Core.Ocr.OcrCharacterErrorRate.MeasureGolden(page.GroundTruth, hypothesis);
            if (cer.Rate > ceiling)
            {
                var dump = Path.Combine(Path.GetTempPath(), $"recall-ocr-golden-p{pageIndex}.txt");
                await File.WriteAllTextAsync(dump, $"REF:\n{page.GroundTruth}\n\nHYP:\n{hypothesis}\n\nNREF:\n{Recall.Core.Ocr.OcrCharacterErrorRate.NormalizeGolden(page.GroundTruth)}\n\nNHYP:\n{Recall.Core.Ocr.OcrCharacterErrorRate.NormalizeGolden(hypothesis)}");
            }

            cer.Rate.Should().BeLessThanOrEqualTo(ceiling, $"page {pageIndex} CER {cer.Rate:P1}");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (IOException ex) when (ex.Message.Contains("being used", StringComparison.Ordinal))
                {
                    // Windows may keep the PNG open briefly after ONNX disposes.
                }
            }
        }
    }

    private static OnnxOcrEngine CreateEngine(FakeIngestCursorStore cursors, FakeMediaVectorIndex media)
    {
        var modelRoot = CopyModelTree();
        var textIngest = new TextIngestEngine(
            new FakeTextEmbeddingEncoder(),
            new FakeTextVectorIndex(),
            new FakeIndexPolicy(),
            new FakeTextTokenCounter(),
            new LocalOpLogWriter(Path.Combine(Path.GetTempPath(), $"recall-oplog-{Guid.NewGuid():N}.jsonl")),
            cursors,
            TestIngest.Memory(),
            TestIngest.Gate());
        return new OnnxOcrEngine(modelRoot, cursors, media, textIngest, TestIngest.Memory());
    }

    private static string CopyModelTree()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "TestAssets", "models", "ocr");
        var targetRoot = Path.Combine(Path.GetTempPath(), $"recall-ocr-models-{Guid.NewGuid():N}");
        var target = Path.Combine(targetRoot, OcrModelFiles.Directory);
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }

        return targetRoot;
    }
}
#endif
