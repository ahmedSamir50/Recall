using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.IntegrationTests.Fixtures;
using Recall.UnitTests.Fakes;
using UglyToad.PdfPig;

namespace Recall.IntegrationTests;

public sealed class RealPdfPatchTests
{
    [Fact]
    public void RealPdf_FirstPatch_IsBetween1664And2048()
    {
        var path = Path.Combine(Path.GetTempPath(), "recall-real-" + Guid.NewGuid().ToString("N") + ".pdf");
        RealPdfFixture.Write(path);
        using var document = PdfDocument.Open(path);
        var text = string.Concat(document.GetPages().Select(page => page.Text));
        var ends = Enumerable.Range(1, text.Length).ToList();
        var patches = DocumentPatchPlanner.Plan("Notes", text, null, 0, ends).Patches;
        patches.Should().NotBeEmpty();
        var firstPatchTokens = ends[patches[0].CharEnd - 1];
        firstPatchTokens.Should().BeInRange(1664, 2048);
        File.Delete(path);
    }

    [Fact]
    public async Task RealPdf_Ingest_WritesChunkSpans()
    {
        var path = Path.Combine(Path.GetTempPath(), "recall-spans-" + Guid.NewGuid().ToString("N") + ".pdf");
        RealPdfFixture.Write(path);
        try
        {
            var cursors = new FakeIngestCursorStore();
            var engine = new TextIngestEngine(
                new FakeTextEmbeddingEncoder(),
                new FakeTextVectorIndex(),
                new FakeIndexPolicy(),
                new FakeTextTokenCounter(),
                new LocalOpLogWriter(Path.Combine(Path.GetTempPath(), $"recall-oplog-{Guid.NewGuid():N}.jsonl")),
                cursors,
                TestIngest.Memory(),
                TestIngest.Gate());

            var result = await engine.IngestFileAsync(
                path,
                string.Empty,
                DateTimeOffset.UtcNow,
                @"C:\grant",
                "enc",
                codeFile: false,
                TestContext.Current.CancellationToken);

            result.Chunks.Should().HaveCountGreaterThan(1);
            var rows = cursors.ListChunks(path).OrderBy(row => row.Ordinal).ToList();
            rows.Should().HaveCount(result.Chunks.Count, "every planned chunk is written as an ingest_chunk row");
            rows.Select(row => row.Ordinal).Should().BeInAscendingOrder();

            foreach (var row in rows)
            {
                row.PageStart.Should().BeGreaterThan(0, "a pdf_text row carries its page span");
                row.PageEnd.Should().BeGreaterThanOrEqualTo(row.PageStart);
                row.CharEnd.Should().BeGreaterThan(row.CharStart, "a pdf_text row carries its char span");
            }

            (rows[0].CharEnd - rows[0].CharStart).Should().BeInRange(
                1664,
                2048,
                "the first cut lands inside the planner target window at one token per character");

            using var document = PdfDocument.Open(path);
            var text = string.Concat(document.GetPages().Select(page => page.Text));
            var overlaps = rows
                .Zip(rows.Skip(1))
                .Where(pair => pair.Second.CharStart < pair.First.CharEnd)
                .ToList();
            overlaps.Should().NotBeEmpty("a chunk cut repeats the sentence it starts on");
            foreach (var (previous, next) in overlaps)
            {
                next.CharStart.Should().BeLessThan(previous.CharEnd);
                var start = next.CharStart;
                char.IsLetter(text[start]).Should().BeTrue($"chunk {next.Ordinal} starts on a word, not mid-sentence");
                var back = start - 1;
                while (back >= 0 && char.IsWhiteSpace(text[back]))
                {
                    back--;
                }

                back.Should().BeGreaterThanOrEqualTo(0);
                text[back].Should().BeOneOf('.', '!', '?', '\u061F', '\u2026');
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
}
