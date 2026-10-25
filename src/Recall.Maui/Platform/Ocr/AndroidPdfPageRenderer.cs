#if ANDROID
using Android.Graphics;
using Android.Graphics.Pdf;

namespace Recall.Maui.Platform.Ocr;

public static class AndroidPdfPageRenderer
{
    public static byte[] Render(string path, int pageNumber)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        using var descriptor = ParcelFileDescriptor.Open(new Java.IO.File(path), ParcelFileDescriptor.ModeReadOnly);
        using var renderer = new PdfRenderer(descriptor);
        if (pageNumber > renderer.PageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        using var page = renderer.OpenPage(pageNumber - 1);
        var width = (int)(page.Width * 300f / 72f);
        var height = (int)(page.Height * 300f / 72f);
        using var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!);
        page.Render(bitmap, null, null, PdfRenderMode.ForDisplay);
        using var stream = new MemoryStream();
        if (!bitmap.Compress(Bitmap.CompressFormat.Png!, 100, stream))
        {
            throw new InvalidOperationException("PDF render failed.");
        }

        return stream.ToArray();
    }
}
#endif
