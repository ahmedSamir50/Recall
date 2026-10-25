using System.Globalization;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Recall.IntegrationTests.Fixtures;

public static class RealPdfFixture
{
    private const string Sentence = "The quick brown fox jumps over the lazy dog. ";

    public static void Write(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var text = new StringBuilder();
        while (true)
        {
            if (text.Length > 0)
            {
                text.Append(Sentence);
            }
            else
            {
                text.Append(Sentence.TrimEnd());
                text.Append(' ');
            }

            var content = BuildContentStream(text.ToString());
            var bytes = BuildPdf(content);
            var temp = path + ".tmp";
            File.WriteAllBytes(temp, bytes);
            try
            {
                using var document = PdfDocument.Open(temp);
                var page = document.GetPage(1);
                if (ContentOrderTextExtractor.GetText(page).Length >= 12000)
                {
                    File.Move(temp, path, overwrite: true);
                    return;
                }
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
        }
    }

    private static string BuildContentStream(string visibleText)
    {
        var escaped = visibleText.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);
        return $"BT /F1 12 Tf 72 720 Td ({escaped}) Tj ET";
    }

    private static byte[] BuildPdf(string contentStream)
    {
        var contentBytes = Encoding.ASCII.GetBytes(contentStream);
        var length = contentBytes.Length.ToString(CultureInfo.InvariantCulture);
        var builder = new StringBuilder();
        builder.AppendLine("%PDF-1.4");
        var offsets = new List<int> { 0 };
        void AddObject(int number, string body)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.AppendLine($"{number} 0 obj");
            builder.AppendLine(body);
            builder.AppendLine("endobj");
        }

        AddObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
        AddObject(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        AddObject(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>");
        AddObject(4, $"<< /Length {length} >>\nstream\n{contentStream}\nendstream");
        AddObject(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.AppendLine("xref");
        builder.AppendLine($"0 {offsets.Count}");
        builder.AppendLine("0000000000 65535 f ");
        for (var i = 1; i < offsets.Count; i++)
        {
            builder.AppendLine($"{offsets[i]:D10} 00000 n ");
        }

        builder.AppendLine("trailer");
        builder.AppendLine($"<< /Size {offsets.Count} /Root 1 0 R >>");
        builder.AppendLine("startxref");
        builder.AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("%%EOF");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }
}
