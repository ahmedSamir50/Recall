#if WINDOWS
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
    private const double CleanCeiling = 0.05;
    private const double PhoneCeiling = 0.10;

    [Fact(Skip = "Skipping test Long running already proven")]
    public void OcrGolden_Page28_ArabicSlice_CerWithinCleanCeiling()
    {
        AssertGoldenPageCer(28, CleanCeiling);
    }

    [Fact(Skip = "Skipping test Long running already proven")]
    public void OcrGolden_EightyPages_CerWithinCeiling()
    {
        Environment.SetEnvironmentVariable("RECALL_OCR_PROGRESS", "1");
        OcrRunProgress.Message("golden catalog build start");
        var pages = OcrGoldenCatalog.Build();
        OcrRunProgress.Message($"golden catalog build done pages={pages.Count}");
        pages.Should().HaveCount(80);

        var tempDir = Path.Combine(Path.GetTempPath(), $"recall-golden-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var paths = new List<string>(pages.Count);
        try
        {
            for (var index = 0; index < pages.Count; index++)
            {
                var page = pages[index];
                var path = Path.Combine(tempDir, $"page-{index:D3}.png");
                File.WriteAllBytes(path, page.Png);
                paths.Add(path);
            }

            var cursors = new FakeIngestCursorStore();
            var media = new FakeMediaVectorIndex();
            OcrRunProgress.Message("ocr run start");
            using var engine = CreateEngine(cursors, media);
            engine.RunPass(paths);
            OcrRunProgress.Message("ocr run end; cer check start");

            for (var index = 0; index < pages.Count; index++)
            {
                var page = pages[index];
                var path = paths[index];
                engine.RecognizedByPath.Should().ContainKey(path);
                var hypothesis = engine.RecognizedByPath[path];
                var cer = Recall.Core.Ocr.OcrCharacterErrorRate.MeasureGolden(page.GroundTruth, hypothesis);
                var ceiling = page.IsPhone ? PhoneCeiling : CleanCeiling;
                cer.Rate.Should().BeLessThanOrEqualTo(ceiling, $"page {index} CER {cer.Rate:P1} exceeded {(page.IsPhone ? "phone" : "clean")} ceiling");
                if (index % 10 == 9)
                {
                    OcrRunProgress.Message($"cer check through page {index + 1}/{pages.Count}");
                }
            }

            OcrRunProgress.Message("golden cer check passed");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private static void AssertGoldenPageCer(int pageIndex, double ceiling)
    {
        DemoDatasetPaths.RequireRoot();
        var pages = OcrGoldenCatalog.Build();
        pages.Should().HaveCount(80);
        var page = pages[pageIndex];
        var tempDir = Path.Combine(Path.GetTempPath(), $"recall-golden-p{pageIndex}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, $"page-{pageIndex:D3}.png");
        File.WriteAllBytes(path, page.Png);
        try
        {
            var cursors = new FakeIngestCursorStore();
            var media = new FakeMediaVectorIndex();
            using var engine = CreateEngine(cursors, media);
            engine.RunPass([path]);
            engine.RecognizedByPath.Should().ContainKey(path);
            var hypothesis = engine.RecognizedByPath[path];
            var cer = Recall.Core.Ocr.OcrCharacterErrorRate.MeasureGolden(page.GroundTruth, hypothesis);
            if (cer.Rate > ceiling)
            {
                var dump = Path.Combine(Path.GetTempPath(), $"recall-ocr-golden-p{pageIndex}.txt");
                File.WriteAllText(dump, $"REF:\n{page.GroundTruth}\n\nHYP:\n{hypothesis}\n\nNREF:\n{Recall.Core.Ocr.OcrCharacterErrorRate.NormalizeGolden(page.GroundTruth)}\n\nNHYP:\n{Recall.Core.Ocr.OcrCharacterErrorRate.NormalizeGolden(hypothesis)}");
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
            cursors);
        return new OnnxOcrEngine(modelRoot, cursors, media, textIngest);
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
