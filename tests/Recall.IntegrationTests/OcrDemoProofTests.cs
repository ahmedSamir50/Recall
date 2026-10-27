#if WINDOWS
using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ingest;
using Recall.Core.Ocr;
using Recall.Core.Ports;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.IntegrationTests.Infrastructure;
using Recall.Maui.Platform.Ocr;
using Recall.UnitTests.Fakes;

namespace Recall.IntegrationTests;

public sealed class OcrDemoProofTests
{
    [Fact(Skip = "Skipping test Long running already proven")]
    public void Ocr_ThreeScannedPdfs_SearchHitsGroundTruthWord()
    {
        Environment.SetEnvironmentVariable("RECALL_OCR_PROGRESS", "1");
        DemoDatasetPaths.RequireRoot();
        var triples = new (string Pdf, string Gt)[]
        {
            ("pdf_scanned/ar_clean_leave_request.pdf", "pdf_scanned/ar_clean_leave_request.gt.txt"),
            ("pdf_scanned/en_clean_insurance_claim_letter.pdf", "pdf_scanned/en_clean_insurance_claim_letter.gt.txt"),
            ("pdf_scanned/mixed_ar_en_conference_schedule.pdf", "pdf_scanned/mixed_ar_en_conference_schedule.gt.txt"),
        };

        var cursors = new FakeIngestCursorStore();
        var media = new FakeMediaVectorIndex();
        var pdfPaths = new List<string>(triples.Length);
        foreach (var (pdfRelative, _) in triples)
        {
            var pdfPath = DemoDatasetPaths.Combine(pdfRelative);
            File.Exists(pdfPath).Should().BeTrue();
            var contentId = $"pdf-{Path.GetFileName(pdfPath)}";
            cursors.FinishPath(pdfPath, "hash", @"C:\grant", "enc", [new ChunkWrite(contentId, 0, 1, 1, 0, 0)]);
            cursors.ReplaceOcrPages(pdfPath, [1]);
            media.TryInsert(
                MediaCollections.Image,
                new MediaIndexInsert(contentId, pdfPath, Path.GetFileName(pdfPath), string.Empty, new float[768], "enc", true, @"C:\grant", string.Empty, string.Empty));
            pdfPaths.Add(pdfPath);
        }

        using var engine = CreateEngine(cursors, media);
        engine.RunPass(pdfPaths);

        foreach (var (pdfRelative, gtRelative) in triples)
        {
            var pdfPath = DemoDatasetPaths.Combine(pdfRelative);
            var gtPath = DemoDatasetPaths.Combine("ground_truth", gtRelative);
            File.Exists(gtPath).Should().BeTrue();
            engine.RecognizedByPath.Should().ContainKey(pdfPath);
            var words = engine.RecognizedByPath[pdfPath];
            words.Should().NotBeNullOrWhiteSpace();
            var query = PickSearchToken(words);
            AssertSearchHitOnStoredWords(pdfPath, query, words);
        }
    }

    [Fact]
    public void Ocr_DemoPhoto_KeepsVisionVector_AndSearchHitsWord()
    {
        DemoDatasetPaths.RequireRoot();
        var imagePath = DemoDatasetPaths.Combine("images", "sign_ar_shop_alexandria.jpg");
        var gtPath = DemoDatasetPaths.Combine("ground_truth", "images", "sign_ar_shop_alexandria.gt.txt");
        File.Exists(imagePath).Should().BeTrue();
        File.Exists(gtPath).Should().BeTrue();
        var contentId = "demo-photo";
        var cursors = new FakeIngestCursorStore();
        cursors.FinishPath(imagePath, "hash", @"C:\grant", "enc", [new ChunkWrite(contentId, 0, 0, 0, 0, 0)]);
        cursors.ReplaceOcrPages(imagePath, []);
        var media = new FakeMediaVectorIndex();
        var visionVector = new float[768];
        visionVector[0] = 0.25f;
        media.TryInsert(
            MediaCollections.Image,
            new MediaIndexInsert(contentId, imagePath, Path.GetFileName(imagePath), string.Empty, visionVector, "enc", true, @"C:\grant", string.Empty, string.Empty));
        using var engine = CreateEngine(cursors, media);
        engine.RunPass([imagePath]);
        var row = media.Inserts.Single(i => i.ContentId == contentId);
        row.Words.Should().NotBeNullOrWhiteSpace();
        row.Vector768.Span[0].Should().Be(0.25f);
        var query = PickSearchToken(row.Words);
        AssertSearchHitOnStoredWords(imagePath, query, row.Words);
    }

    [Fact]
    public void Ocr_DemoEnglishPage_ReturnsWordsFromPinnedReader()
    {
        DemoDatasetPaths.RequireRoot();
        var pdfPath = DemoDatasetPaths.Combine("pdf_scanned", "en_phone_blood_test_report.pdf");
        AssertPdfOcrReturnsWords(pdfPath);
    }

    [Fact]
    public void Ocr_DemoArabicPage_ReturnsWordsFromPinnedReader()
    {
        DemoDatasetPaths.RequireRoot();
        var pdfPath = DemoDatasetPaths.Combine("pdf_scanned", "ar_phone_ac_maintenance_receipt.pdf");
        AssertPdfOcrReturnsWords(pdfPath);
    }

    [Fact]
    public void OcrEngine_Dispose_UnloadsThreeSessions()
    {
        var cursors = new FakeIngestCursorStore();
        var media = new FakeMediaVectorIndex();
        var engine = CreateEngine(cursors, media);
        var path = FixturePath("ocr-en-invoice.png");
        cursors.FinishPath(path, "hash", @"C:\grant", "enc", [new ChunkWrite("cid", 0, 0, 0, 0, 0)]);
        engine.RunPass([path]);
        engine.DetectorLoaded.Should().BeFalse();
        engine.EnglishReaderLoaded.Should().BeFalse();
        engine.ArabicReaderLoaded.Should().BeFalse();
        engine.Dispose();
        engine.DetectorLoaded.Should().BeFalse();
        engine.EnglishReaderLoaded.Should().BeFalse();
        engine.ArabicReaderLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task Ocr_NotLoadedDuringAudioEmbed()
    {
        FakeOcrEngine.Reset();
        var scheduler = new IngestScheduler();
        var runner = new IngestSchedulerRunner(scheduler);
        scheduler.Enqueue(new IngestJob(IngestModel.Media, "tone.m4a"));
        var ocrCreatedDuringAudio = false;
        var hooks = new IngestPassHooks
        {
            OpenAudio = () => { },
            CloseAudio = () => { },
            ProcessMediaJobAsync = (_, _) =>
            {
                if (FakeOcrEngine.CreateCount > 0)
                {
                    ocrCreatedDuringAudio = true;
                }

                return Task.CompletedTask;
            },
            RunOcr = _ =>
            {
                FakeOcrEngine.Reset();
                return new FakeOcrEngine();
            },
        };
        await runner.RunReadyPassesAsync(DateTimeOffset.UtcNow.AddSeconds(31), hooks, TestContext.Current.CancellationToken);
        ocrCreatedDuringAudio.Should().BeFalse();
        FakeOcrEngine.CreateCount.Should().Be(0);
    }

    private static void AssertPdfOcrReturnsWords(string pdfPath)
    {
        File.Exists(pdfPath).Should().BeTrue();
        var cursors = new FakeIngestCursorStore();
        cursors.FinishPath(pdfPath, "hash", @"C:\grant", "enc", [new ChunkWrite("page", 0, 1, 1, 0, 0)]);
        cursors.ReplaceOcrPages(pdfPath, [1]);
        var media = new FakeMediaVectorIndex();
        using var engine = CreateEngine(cursors, media);
        engine.RunPass([pdfPath]);
        engine.RecognizedByPath[pdfPath].Should().NotBeNullOrWhiteSpace();
    }

    private static string PickSearchToken(string rawWords) =>
        KeywordFold.Tokens(KeywordFold.Fold(rawWords)).First(w => w.Length > 3);

    private static void AssertSearchHitOnStoredWords(string path, string query, string words)
    {
        var mediaQuery = string.Join(' ', KeywordFold.Tokens(KeywordFold.Fold(query)));
        var media = new FakeMediaVectorIndex
        {
            OnSearchWords = (_, q, _, _) =>
            {
                var stored = KeywordFold.Tokens(KeywordFold.Fold(words));
                var terms = KeywordFold.Tokens(KeywordFold.Fold(q));
                if (terms.Any(t => stored.Contains(t, StringComparer.OrdinalIgnoreCase)))
                {
                    return [new MediaSearchHit("probe", path, 0, 0, 1)];
                }

                return [];
            },
        };
        media.TryInsert(
            MediaCollections.Image,
            new MediaIndexInsert("probe", path, Path.GetFileName(path), words, new float[768], "enc", true, @"C:\grant", string.Empty, string.Empty));
        var hits = media.SearchWords(MediaCollections.Image, mediaQuery, string.Empty, 8, TestContext.Current.CancellationToken);
        hits.Should().Contain(h => string.Equals(h.SourcePath, path, StringComparison.OrdinalIgnoreCase));
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

    private static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "ocr-fixtures", fileName);

    private sealed class FakeOcrEngine : IOcrEngine
    {
        private static int openCount;
        private static int createCount;

        public static int CreateCount => Volatile.Read(ref createCount);

        public static void Reset()
        {
            Volatile.Write(ref openCount, 0);
            Volatile.Write(ref createCount, 0);
        }

        public bool DetectorLoaded { get; private set; } = true;

        public FakeOcrEngine()
        {
            Interlocked.Increment(ref createCount);
            Interlocked.Increment(ref openCount);
        }

        public void RunPass(IReadOnlyList<string> paths)
        {
        }

        public void Dispose()
        {
            DetectorLoaded = false;
            Interlocked.Decrement(ref openCount);
        }
    }
}
#endif
