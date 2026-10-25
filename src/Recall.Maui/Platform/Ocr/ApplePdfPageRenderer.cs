#if IOS || MACCATALYST
using CoreGraphics;
using Foundation;
using PDFKit;
using UIKit;

namespace Recall.Maui.Platform.Ocr;

public static class ApplePdfPageRenderer
{
    public static byte[] Render(string path, int pageNumber)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        using var document = new PdfDocument(NSUrl.FromFilename(path))
            ?? throw new InvalidOperationException("PDF open failed.");
        if (pageNumber > document.PageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        using var page = document.GetPage(pageNumber - 1);
        var bounds = page.GetBounds(PdfDisplayBox.MediaBox);
        var scale = 300f / 72f;
        var width = (int)Math.Ceiling(bounds.Width * scale);
        var height = (int)Math.Ceiling(bounds.Height * scale);
        using var renderer = new UIGraphicsImageRenderer(new CGSize(width, height));
        var image = renderer.CreateImage(ctx =>
        {
            ctx.SetFillColor(UIColor.White.CGColor);
            ctx.FillRect(new CGRect(0, 0, width, height));
            ctx.ScaleCTM(scale, scale);
            page.Draw(ctx.CGContext, bounds);
        });
        using var data = image.AsPNG();
        return data?.ToArray() ?? throw new InvalidOperationException("PDF render failed.");
    }
}
#endif
