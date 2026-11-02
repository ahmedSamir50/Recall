using Recall.Core.Ingest;

namespace Recall.Maui.Platform.Ocr;

public sealed class MauiPdfPageRenderer : IPdfPageRenderer
{
    public byte[] Render(string path, int pageNumber)
    {
#if WINDOWS
        return WindowsPdfPageRenderer.Render(path, pageNumber);
#elif ANDROID
        return AndroidPdfPageRenderer.Render(path, pageNumber);
#elif IOS || MACCATALYST
        return ApplePdfPageRenderer.Render(path, pageNumber);
#else
        throw new PlatformNotSupportedException();
#endif
    }
}
