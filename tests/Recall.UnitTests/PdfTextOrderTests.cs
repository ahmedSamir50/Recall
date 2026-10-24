using System.Text;
using FluentAssertions;
using Recall.Core.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Recall.UnitTests;

public sealed class PdfTextOrderTests
{
    [Fact]
    public void Read_Null_Throws()
    {
        var act = () => PdfPigPageSource.Read(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Read_NotAPdf_Throws()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("not a pdf"));
        var act = () => PdfPigPageSource.Read(stream);
        act.Should().Throw<InvalidDataException>().WithMessage("PDF text could not be read.");
    }

    [Fact]
    public void Read_LtrEnglish_StaysLeftToRight()
    {
        ReadLines()[0].Should().Be("The price is 12.");
        RawLines()[0].Should().Be("The price is 12.");
    }

    [Fact]
    public void Read_LtrArabicPresentation_RawIsReversed_ReadIsLogical()
    {
        RawLines()[1].Should().Be("ﻢﻟﺎﻌﻟﺎﺑ ﺎﺒﺣﺮﻣ");
        ReadLines()[1].Should().Be("مرحبا بالعالم");
    }

    [Fact]
    public void Read_LtrMixed_KeepsEnglishOrder_AndRestoresArabic()
    {
        RawLines()[2].Should().Be("Hello ﺎﺒﺣﺮﻣ world");
        ReadLines()[2].Should().Be("Hello مرحبا world");
    }

    [Fact]
    public void Read_RtlMixedRightAligned_RestoresReadingOrder()
    {
        RawLines()[3].Should().Be("world ﺎﺒﺣﺮﻣ Hello");
        ReadLines()[3].Should().Be("Hello مرحبا world");
    }

    [Fact]
    public void Read_LogicalArabicPlacedRightToLeft_IsNotReversedAgain()
    {
        RawLines()[4].Should().Be("مرحبا بالعالم");
        ReadLines()[4].Should().Be("مرحبا بالعالم");
    }

    [Fact]
    public void Read_PageWithoutArabic_StaysContentOrder()
    {
        var pages = ReadPages();
        pages[1].PageNumber.Should().Be(2);
        pages[1].Text.Should().Be("Notes stay in order.");
        RawPage(2).Should().Be("Notes stay in order.");
    }

    private static string[] ReadLines() => ReadPages()[0].Text.Split('\n');

    private static IReadOnlyList<PdfPageText> ReadPages()
    {
        using var stream = File.OpenRead(FixturePath());
        return PdfPigPageSource.Read(stream);
    }

    private static string[] RawLines() => RawPage(1).Split('\n').Select(line => line.Trim()).ToArray();

    private static string RawPage(int pageNumber)
    {
        using var document = PdfDocument.Open(FixturePath());
        var page = document.GetPages().Single(item => item.Number == pageNumber);
        return ContentOrderTextExtractor.GetText(page);
    }

    private static string FixturePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "tests", "Recall.UnitTests", "Fixtures", "arabic-visual.pdf");
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("arabic-visual.pdf was not found.");
    }
}
