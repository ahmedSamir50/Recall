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
    public void OcrEn_InvoiceSentence_IsFound()
    {
        // SalorWorks 01-simple-english: invoice number INV-2026-001 on the page.
        AssertSearchHit("ocr-en-invoice.png", "INV-2026-001");
    }

    [Fact]
    public void OcrAr_TotalSentence_IsFound()
    {
        // SalorWorks 03-arabic: Latin invoice id plus Arabic body text on the same scan.
        var (_, words) = RunOcrPass("ocr-ar-total.png");
        words.Should().Contain("INV-2026-003");
        ContainsArabicLetters(words).Should().BeTrue();
        AssertSearchHitOnStoredWords("ocr-ar-total.png", "INV-2026-003", words);
    }

    [Fact]
    public void OcrMixed_BothScripts_AreFound()
    {
        var (path, words) = RunOcrPass("ocr-mixed-invoice.png");
        words.Should().Contain("INV-2026-004");
        words.ToUpperInvariant().Should().Contain("SUP-WAT-330");
        ContainsArabicLetters(words).Should().BeTrue();
        AssertSearchHitOnStoredWords(path, "INV-2026-004", words);
        AssertSearchHitOnStoredWords(path, "WAT-330", words);
    }

    private static void AssertSearchHit(string fixtureName, string query)
    {
        var (path, words) = RunOcrPass(fixtureName);
        WordsShouldBeStored(words);
        AssertSearchHitOnStoredWords(path, query, words);
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
        return (path, words);
    }

    private static void WordsShouldBeStored(string words) =>
        words.Length.Should().BeGreaterThan(0, "OCR should store words on the image row");

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

    private static bool ContainsArabicLetters(string text)
    {
        foreach (var ch in text)
        {
            if (ch is >= '\u0600' and <= '\u06FF')
            {
                return true;
            }
        }

        return false;
    }
}
#endif
