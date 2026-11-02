#if IOS || MACCATALYST
using CoreGraphics;
using Foundation;
using PdfKit;
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

        using var page = document.GetPage(pageNumber - 1)
            ?? throw new InvalidOperationException("PDF render failed.");
        var bounds = page.GetBoundsForBox(PdfDisplayBox.Media);
        var scale = (nfloat)(300d / 72d);
        var width = (int)Math.Ceiling((double)bounds.Width * (300d / 72d));
        var height = (int)Math.Ceiling((double)bounds.Height * (300d / 72d));
        using var renderer = new UIGraphicsImageRenderer(new CGSize(width, height));
        var image = renderer.CreateImage(ctx =>
        {
            var graphics = ctx.CGContext;
            graphics.SetFillColor(UIColor.White.CGColor);
            graphics.FillRect(new CGRect(0, 0, width, height));
            graphics.ScaleCTM(scale, scale);
            page.Draw(PdfDisplayBox.Media, graphics);
        });
        using var data = image.AsPNG();
        return data?.ToArray() ?? throw new InvalidOperationException("PDF render failed.");
    }
}
#endif
