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
    public void OcrEn_Invoice_WordErrorWithinAcceptedCeiling()
    {
        var (path, words) = RunOcrPass("ocr-en-invoice.png");
        AssertWordErrorWithinAccepted("ocr-en-invoice.png", OcrFixtureReference.EnInvoice, words, OcrFixtureReference.EnInvoiceAccepted);
        AssertSearchHitOnStoredWords(path, "INV-2026-001", words);
    }

    [Fact]
    public void OcrAr_Invoice_WordErrorWithinAcceptedCeiling()
    {
        var (path, words) = RunOcrPass("ocr-ar-total.png");
        AssertWordErrorWithinAccepted("ocr-ar-total.png", OcrFixtureReference.ArInvoice, words, OcrFixtureReference.ArInvoiceAccepted);
        AssertSearchHitOnStoredWords(path, "INV-2026-003", words);
    }

    [Fact]
    public void OcrMixed_Invoice_WordErrorWithinAcceptedCeiling()
    {
        var (path, words) = RunOcrPass("ocr-mixed-invoice.png");
        AssertWordErrorWithinAccepted("ocr-mixed-invoice.png", OcrFixtureReference.MixedInvoice, words, OcrFixtureReference.MixedInvoiceAccepted);
        AssertSearchHitOnStoredWords(path, "INV-2026-004", words);
    }

    [Fact]
    public void OcrEn_BookPage_WordErrorWithinAcceptedCeiling()
    {
        var (_, words) = RunOcrPass("en_book_page.webp");
        AssertWordErrorWithinAccepted("en_book_page.webp", OcrFixtureReference.EnBookPage, words, OcrFixtureReference.EnBookAccepted);
    }

    [Fact]
    public void OcrAr_BookPage_WordErrorWithinAcceptedCeiling()
    {
        var (_, words) = RunOcrPass("ar_book_page.webp");
        AssertWordErrorWithinAccepted("ar_book_page.webp", OcrFixtureReference.ArBookPage, words, OcrFixtureReference.ArBookAccepted);
    }

    private static readonly object LogGate = new();

    private static bool logCleared;

    private static void AssertWordErrorWithinAccepted(string name, string reference, string hypothesis, OcrAcceptedWordError accepted)
    {
        WriteHypothesisLog(name, reference, hypothesis, accepted);
        var latin = OcrWordErrorRate.MeasureScript(reference, hypothesis, OcrScript.Latin);
        var arabic = OcrWordErrorRate.MeasureScript(reference, hypothesis, OcrScript.Arabic);
        var page = OcrWordErrorRate.Measure(reference, hypothesis);

        var failures = new List<string>();
        if (page.ReferenceCount != accepted.PageTokens || page.Edits > accepted.PageEdits)
        {
            failures.Add($"page WER {page.Rate:P1} ({page.Edits}/{page.ReferenceCount} edits) exceeds accepted {accepted.PageEdits}/{accepted.PageTokens}");
        }

        if (accepted.LatinTokens > 0 && (latin.ReferenceCount != accepted.LatinTokens || latin.Edits > accepted.LatinEdits))
        {
            failures.Add($"Latin WER {latin.Rate:P1} ({latin.Edits}/{latin.ReferenceCount} edits) exceeds accepted {accepted.LatinEdits}/{accepted.LatinTokens}");
        }

        if (accepted.ArabicTokens > 0 && (arabic.ReferenceCount != accepted.ArabicTokens || arabic.Edits > accepted.ArabicEdits))
        {
            failures.Add($"Arabic WER {arabic.Rate:P1} ({arabic.Edits}/{arabic.ReferenceCount} edits) exceeds accepted {accepted.ArabicEdits}/{accepted.ArabicTokens}");
        }

        var hitRate = OcrWordErrorRate.ReferenceTokenHitRate(reference, hypothesis);
        var preview = hypothesis.Length <= 500 ? hypothesis : hypothesis[..500];
        failures.Should().BeEmpty(
            $"ordered WER over the accepted fixture ceiling. Order-free token hit rate {hitRate:P0}. {string.Join(" ", failures)} OCR: {preview}");
    }

    private static void WriteHypothesisLog(string name, string reference, string hypothesis, OcrAcceptedWordError accepted)
    {
        var page = OcrWordErrorRate.Measure(reference, hypothesis);
        var latin = OcrWordErrorRate.MeasureScript(reference, hypothesis, OcrScript.Latin);
        var arabic = OcrWordErrorRate.MeasureScript(reference, hypothesis, OcrScript.Arabic);
        var hitRate = OcrWordErrorRate.ReferenceTokenHitRate(reference, hypothesis);
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ocr-hypothesis.log"));
        var block =
            $"=== {name} ===" + Environment.NewLine +
            "Reference:" + Environment.NewLine +
            reference + Environment.NewLine + Environment.NewLine +
            "Model returned:" + Environment.NewLine +
            hypothesis + Environment.NewLine + Environment.NewLine +
            $"WER: {page.Rate:P1} ({page.Edits}/{page.ReferenceCount} edits) accepted {accepted.PageEdits}/{accepted.PageTokens}" + Environment.NewLine +
            (latin.ReferenceCount > 0 ? $"Latin WER: {latin.Rate:P1} ({latin.Edits}/{latin.ReferenceCount}) accepted {accepted.LatinEdits}/{accepted.LatinTokens}" + Environment.NewLine : string.Empty) +
            (arabic.ReferenceCount > 0 ? $"Arabic WER: {arabic.Rate:P1} ({arabic.Edits}/{arabic.ReferenceCount}) accepted {accepted.ArabicEdits}/{accepted.ArabicTokens}" + Environment.NewLine : string.Empty) +
            $"Token hit rate: {hitRate:P0}" + Environment.NewLine +
            Environment.NewLine;

        lock (LogGate)
        {
            if (!logCleared)
            {
                File.WriteAllText(path, string.Empty);
                logCleared = true;
            }

            File.AppendAllText(path, block);
        }
    }

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
