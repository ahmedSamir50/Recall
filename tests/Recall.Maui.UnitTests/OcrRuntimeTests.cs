#if WINDOWS
using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ocr;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.Maui.Platform.Ocr;
using Recall.UnitTests.Fakes;

namespace Recall.Maui.UnitTests;

public sealed class OcrRuntimeTests
{
    [Fact]
    public void Ocr_TextLayer_DoesNotOpen()
    {
        var cursors = new FakeIngestCursorStore();
        var media = new FakeMediaVectorIndex();
        var textIngest = CreateTextIngest(cursors);
        var engine = new OnnxOcrEngine(Path.Combine(Path.GetTempPath(), $"ocr-empty-{Guid.NewGuid():N}"), cursors, media, textIngest);
        engine.RunPass(["text.pdf"]);
        engine.RecognizedByPath.Should().BeEmpty();
    }

    [Fact]
    public void OcrImage_Words_UseChunkOrdinalZero()
    {
        var path = FixturePath("ocr-en-invoice.png");
        var contentId = "image-chunk-0";
        var cursors = new FakeIngestCursorStore();
        cursors.FinishPath(
            path,
            "hash",
            @"C:\grant",
            "enc",
            [new ChunkWrite(contentId, 0, 0, 0, 0, 0)]);
        var media = new FakeMediaVectorIndex();
        media.TryInsert(
            MediaCollections.Image,
            new MediaIndexInsert(contentId, path, Path.GetFileName(path), string.Empty, new float[768], "enc", true, @"C:\grant", string.Empty, string.Empty));
        var engine = CreateEngine(cursors, media);
        engine.RunPass([path]);
        media.Inserts.Should().ContainSingle(i => i.ContentId == contentId && i.Words.Length > 0);
    }

    [Fact]
    public void OcrEn_Invoice_WordErrorWithinCardBudget()
    {
        var (path, words) = RunOcrPass("ocr-en-invoice.png");
        AssertWordErrorWithinCard(OcrFixtureReference.EnInvoice, words);
        AssertSearchHitOnStoredWords(path, "INV-2026-001", words);
    }

    [Fact]
    public void OcrAr_Invoice_WordErrorWithinCardBudget()
    {
        var (path, words) = RunOcrPass("ocr-ar-total.png");
        AssertWordErrorWithinCard(OcrFixtureReference.ArInvoice, words);
        AssertSearchHitOnStoredWords(path, "INV-2026-003", words);
    }

    [Fact]
    public void OcrMixed_Invoice_WordErrorWithinCardBudget()
    {
        var (path, words) = RunOcrPass("ocr-mixed-invoice.png");
        AssertWordErrorWithinCard(OcrFixtureReference.MixedInvoice, words);
        AssertSearchHitOnStoredWords(path, "INV-2026-004", words);
    }

    [Fact]
    public void OcrEn_BookPage_WordErrorWithinCardBudget()
    {
        var (_, words) = RunOcrPass("en_book_page.webp");
        AssertWordErrorWithinCard(OcrFixtureReference.EnBookPage, words);
    }

    [Fact]
    public void OcrAr_BookPage_WordErrorWithinCardBudget()
    {
        var (_, words) = RunOcrPass("ar_book_page.webp");
        AssertWordErrorWithinCard(OcrFixtureReference.ArBookPage, words);
    }

    private static void AssertWordErrorWithinCard(string reference, string hypothesis)
    {
        var latin = OcrWordErrorRate.MeasureScript(reference, hypothesis, OcrScript.Latin);
        var arabic = OcrWordErrorRate.MeasureScript(reference, hypothesis, OcrScript.Arabic);
        var pageCap = arabic.ReferenceCount > 0
            ? OcrFixtureReference.ArabicMaxWordErrorRate
            : OcrFixtureReference.EnglishMaxWordErrorRate;
        var page = OcrWordErrorRate.Measure(reference, hypothesis);

        var failures = new List<string>();
        if (latin.ReferenceCount > 0 && latin.Rate > OcrFixtureReference.EnglishMaxWordErrorRate)
        {
            failures.Add(FormatBudget("Latin", latin, OcrFixtureReference.EnglishPrintedRecognitionAccuracy, OcrFixtureReference.EnglishMaxWordErrorRate));
        }

        if (arabic.ReferenceCount > 0 && arabic.Rate > OcrFixtureReference.ArabicMaxWordErrorRate)
        {
            failures.Add(FormatBudget("Arabic", arabic, OcrFixtureReference.ArabicLineRecognitionAccuracy, OcrFixtureReference.ArabicMaxWordErrorRate));
        }

        if (page.Rate > pageCap)
        {
            failures.Add($"page WER {page.Rate:P1} ({page.Edits}/{page.ReferenceCount} edits) exceeds {pageCap:P1}");
        }

        var hitRate = OcrWordErrorRate.ReferenceTokenHitRate(reference, hypothesis);
        var preview = hypothesis.Length <= 500 ? hypothesis : hypothesis[..500];
        failures.Should().BeEmpty(
            $"ordered WER over the card budget. Order-free token hit rate {hitRate:P0}. {string.Join(" ", failures)} OCR: {preview}");
    }

    private static string FormatBudget(string script, OcrWordError error, double cardAccuracy, double maxRate) =>
        $"{script} WER {error.Rate:P1} ({error.Edits}/{error.ReferenceCount}). " +
        $"Card accuracy {cardAccuracy:P2}, complement {(1d - cardAccuracy):P2}, margin {OcrFixtureReference.EmbeddingMargin:P0}, cap {maxRate:P1}.";

    private static (string Path, string Words) RunOcrPass(string fixtureName)
    {
        var path = FixturePath(fixtureName);
        File.Exists(path).Should().BeTrue($"fixture missing: {path}");
        var contentId = $"cid-{fixtureName}";
        var cursors = new FakeIngestCursorStore();
        cursors.FinishPath(path, "hash", @"C:\grant", "enc", [new ChunkWrite(contentId, 0, 0, 0, 0, 0)]);
        var media = new FakeMediaVectorIndex();
        media.TryInsert(
            MediaCollections.Image,
            new MediaIndexInsert(contentId, path, Path.GetFileName(path), string.Empty, new float[768], "enc", true, @"C:\grant", string.Empty, string.Empty));
        var engine = CreateEngine(cursors, media);
        engine.RunPass([path]);
        var words = media.Inserts.Single(i => i.ContentId == contentId).Words;
        words.Length.Should().BeGreaterThan(0, "OCR should store words on the image row");
        return (path, words);
    }

    private static void AssertSearchHitOnStoredWords(string path, string query, string words)
    {
        var media = new FakeMediaVectorIndex();
        media.TryInsert(
            MediaCollections.Image,
            new MediaIndexInsert("probe", path, Path.GetFileName(path), words, new float[768], "enc", true, @"C:\grant", string.Empty, string.Empty));
        var hits = media.SearchWords(MediaCollections.Image, query, string.Empty, 8, TestContext.Current.CancellationToken);
        hits.Should().Contain(
            h => string.Equals(h.SourcePath, path, StringComparison.OrdinalIgnoreCase),
            $"query '{query}' missed stored OCR text (length {words.Length})");
    }

    private static OnnxOcrEngine CreateEngine(FakeIngestCursorStore cursors, FakeMediaVectorIndex media)
    {
        var modelRoot = CopyModelTree();
        var textIngest = CreateTextIngest(cursors);
        return new OnnxOcrEngine(modelRoot, cursors, media, textIngest);
    }

    private static TextIngestEngine CreateTextIngest(FakeIngestCursorStore cursors)
    {
        return new TextIngestEngine(
            new FakeTextEmbeddingEncoder(),
            new FakeTextVectorIndex(),
            new FakeIndexPolicy(),
            new FakeTextTokenCounter(),
            new LocalOpLogWriter(Path.Combine(Path.GetTempPath(), $"recall-oplog-{Guid.NewGuid():N}.jsonl")),
            cursors);
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
}
#endif
