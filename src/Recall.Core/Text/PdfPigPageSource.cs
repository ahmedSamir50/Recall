using UglyToad.PdfPig;

namespace Recall.Core.Text;

public static class PdfPigPageSource
{
    public static IReadOnlyList<PdfPageText> Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            using var document = PdfDocument.Open(stream);
            var pages = new List<PdfPageText>();
            foreach (var page in document.GetPages())
            {
                pages.Add(new PdfPageText(page.Number, PdfTextOrder.Logical(page)));
            }

            return pages;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidDataException("PDF text could not be read.", ex);
        }
    }
}
