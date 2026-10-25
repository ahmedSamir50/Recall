using FluentAssertions;
using Recall.Core.Text;
using Recall.IntegrationTests.Fixtures;
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
        var patches = DocumentPatchPlanner.Plan("Notes", text, null, 0, ends);
        patches.Should().NotBeEmpty();
        var firstPatchTokens = ends[patches[0].CharEnd - 1];
        firstPatchTokens.Should().BeInRange(1664, 2048);
        File.Delete(path);
    }
}
