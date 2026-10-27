using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Ocr;
using Recall.Core.Text;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class Phase5PdfTests
{
    [Fact]
    public void EndsPatch_At7680_IsTrue()
    {
        DocumentPatchPlanner.EndsPatch(7680).Should().BeTrue();
    }

    [Fact]
    public void EndsPatch_At7679_IsFalse()
    {
        DocumentPatchPlanner.EndsPatch(7679).Should().BeFalse();
    }

    [Fact]
    public void OpenPage_Null_IsOne()
    {
        DocumentPatchPlanner.OpenPage(null).Should().Be(1);
    }

    [Fact]
    public void OpenPage_Zero_IsOne()
    {
        DocumentPatchPlanner.OpenPage(0).Should().Be(1);
    }

    [Fact]
    public void OpenPage_Three_StaysThree()
    {
        DocumentPatchPlanner.OpenPage(3).Should().Be(3);
    }

    [Fact]
    public void Patch_Targets2048()
    {
        var document = new string('x', 2038) + " " + new string('y', 2962);
        var ends = Enumerable.Range(0, 3001).ToList();
        var patches = DocumentPatchPlanner.Plan("Notes", document, null, 10, ends);
        patches[0].CharEnd.Should().Be(2038);
        patches[0].Raw.Should().Be(new string('x', 2038));
        patches[0].CharEnd.Should().NotBe(7680);
    }

    [Fact]
    public void FullDocument_ElevenShortPages_IsOnePatchThroughPageEleven()
    {
        var pages = Enumerable.Range(1, 11)
            .Select(page => new PdfPageText(page, new string('x', 101)))
            .ToList();
        var document = string.Concat(pages.Select(page => page.Text).Select((text, index) => index == 0 ? text : "\f" + text));
        var ends = Enumerable.Range(0, document.Length + 1).ToList();
        var patches = DocumentPatchPlanner.Plan("Notes", document, pages, 0, ends);
        patches.Should().ContainSingle();
        patches[0].PageStart.Should().Be(1);
        patches[0].PageEnd.Should().Be(11);
    }

    [Fact]
    public void Cut_PrefersParagraph()
    {
        var document = new string('a', 1884) + "\n\n" + new string('b', 94) + ". " + "C" + new string('y', 417);
        var ends = Enumerable.Range(0, document.Length + 1).ToList();
        var patches = DocumentPatchPlanner.Plan("Notes", document, null, 0, ends);
        patches.Count.Should().BeGreaterThan(1);
        patches[0].CharEnd.Should().Be(1886);
        patches[0].Raw.Should().EndWith("\n");
    }

    [Fact]
    public void Cut_FallsBackToSentence_ArabicQuestionMark()
    {
        var document = new string('x', 1900) + "؟ C" + new string('y', 499);
        var ends = Enumerable.Range(0, document.Length + 1).ToList();
        var patches = DocumentPatchPlanner.Plan("Notes", document, null, 0, ends);
        patches.Count.Should().BeGreaterThan(1);
        patches[0].CharEnd.Should().Be(1901);
        patches[0].Raw.Should().EndWith("؟");
    }

    [Fact]
    public void Cut_NeverInsideWord()
    {
        var document = new string('a', 1700) + " " + new string('b', 1700);
        var ends = Enumerable.Range(0, document.Length + 1).ToList();
        var patches = DocumentPatchPlanner.Plan("Notes", document, null, 0, ends);
        patches.Should().HaveCount(2);
        patches[0].CharEnd.Should().Be(1700);
        patches[0].Raw.Contains(' ').Should().BeFalse();
        patches[0].Raw.Should().EndWith("a");
        patches[1].CharStart.Should().Be(1700);
        document[patches[1].CharStart].Should().Be(' ');
    }

    [Fact]
    public void Overlap_StartsAtSentenceStart()
    {
        var document = BuildSentences(30);
        var ends = Enumerable.Range(0, document.Length + 1).ToList();
        var patches = DocumentPatchPlanner.Plan("Notes", document, null, 0, ends);
        var start = patches[1].CharStart;
        document[start - 2].Should().Be('.');
        document[start - 1].Should().Be(' ');
        document[start].Should().Be('S');
        (patches[0].CharEnd - start).Should().BeGreaterThanOrEqualTo(256);
        (patches[0].CharEnd - start).Should().BeLessThanOrEqualTo(384);
    }

    [Fact]
    public void CharSpan_SecondPatch_IsNotZero()
    {
        var document = BuildSentences(30);
        var ends = Enumerable.Range(0, document.Length + 1).ToList();
        var patches = DocumentPatchPlanner.Plan("Notes", document, null, 0, ends);
        patches[1].CharStart.Should().BeGreaterThan(0);
        patches[1].CharStart.Should().BeLessThan(document.Length);
    }

    [Fact]
    public void Decimal_IsNotSentenceEnd()
    {
        var document = new string('x', 1880) + " 3.5 e.g. now Done. Next" + new string('y', 499);
        var ends = Enumerable.Range(0, document.Length + 1).ToList();
        var patches = DocumentPatchPlanner.Plan("Notes", document, null, 0, ends);
        patches.Count.Should().BeGreaterThan(1);
        patches[0].CharEnd.Should().Be(1899);
        patches[0].Raw.Should().EndWith("Done.");
    }

    [Fact]
    public void Overlap_ShortPatch_DoesNotRepeat()
    {
        var document = new string('x', 100);
        var ends = Enumerable.Range(0, document.Length + 1).ToList();
        var patches = DocumentPatchPlanner.Plan("Notes", document, null, 0, ends);
        patches.Should().ContainSingle();
        patches[0].Raw.Should().Be(document);
        patches[0].CharEnd.Should().Be(100);
    }

    [Fact]
    public void SplitPdfPages_NeedsOcr_OmitsPageFromJoin()
    {
        var pages = new[]
        {
            new PdfPageText(1, "intro"),
            new PdfPageText(2, "مر\uFFFD\uFFFD\uFFFD"),
            new PdfPageText(3, "outro"),
        };
        var split = DocumentPatchPlanner.SplitPdfPages(pages);
        split.OcrPageNumbers.Should().Equal(2);
        split.ImageFastPathPages.Should().BeEmpty();
        split.EmbeddablePages.Should().HaveCount(2);
        split.JoinedDocument.Should().Be("intro\foutro");
        split.JoinedDocument.Should().NotContain("\uFFFD");
    }

    [Fact]
    public void SplitPdfPages_BlankPage_QueuesOcrAndSkipsText()
    {
        var split = DocumentPatchPlanner.SplitPdfPages(
        [
            new PdfPageText(1, "hello"),
            new PdfPageText(2, "   "),
            new PdfPageText(3, string.Empty),
        ]);
        split.EmbeddablePages.Select(page => page.PageNumber).Should().Equal(1);
        split.OcrPageNumbers.Should().Equal(2, 3);
        split.ImageFastPathPages.Should().Equal(2, 3);
        split.JoinedDocument.Should().Be("hello");
    }

    [Fact]
    public void AppendLiveChunk_LeavesFileReading()
    {
        var cursors = new FakeIngestCursorStore();
        cursors.MarkReading("a.pdf", "hash", "grant");
        cursors.AppendLiveChunk("a.pdf", new ChunkWrite("id", 0, 1, 1, 0, 10));
        cursors.GetFile("a.pdf")!.State.Should().Be(IngestCursorState.Reading);
        cursors.ListChunks("a.pdf").Should().ContainSingle(chunk => chunk.PageEnd == 1 && !chunk.Missing && !chunk.Retired);
        cursors.AnyLivePath("id").Should().BeTrue();
    }

    [Fact]
    public void OcrPageWaitlist_EnqueuesScheduler()
    {
        var cursors = new FakeIngestCursorStore();
        var scheduler = new IngestScheduler();
        OcrPageWaitlist.Apply("doc.pdf", [2, 5], cursors, scheduler);
        cursors.ListOcrPages("doc.pdf").Should().Equal(2, 5);
        scheduler.QueueCount(IngestModel.Ocr).Should().Be(1);
    }

    [Fact]
    public void ReplaceOcrPages_Sqlite_RoundTrips()
    {
        var db = Path.Combine(Path.GetTempPath(), $"recall-ocr-{Guid.NewGuid():N}", CheckpointSql.FileName);
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.ReplaceOcrPages("doc.pdf", [3, 1]);
            store.ListOcrPages("doc.pdf").Should().Equal(1, 3);
            store.ReplaceOcrPages("doc.pdf", []);
            store.ListOcrPages("doc.pdf").Should().BeEmpty();
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    private static string BuildSentences(int count)
    {
        var builder = new System.Text.StringBuilder();
        for (var index = 0; index < count; index++)
        {
            builder.Append(new string('S', 99));
            builder.Append('.');
            builder.Append(' ');
        }

        return builder.ToString();
    }
}
