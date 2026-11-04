#if WINDOWS
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Recall.Maui.Platform.Ocr;

public static class WindowsPdfPageRenderer
{
    private const int DefaultDpi = 200;

    public static double RenderScale => RenderDpi / 96d;

    private static int RenderDpi
    {
        get
        {
            var text = Environment.GetEnvironmentVariable("RECALL_OCR_RENDER_DPI");
            return int.TryParse(text, out var dpi) && dpi > 0 ? dpi : DefaultDpi;
        }
    }

    [SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "PDF render is synchronous and runs off the UI thread.")]
    public static byte[] Render(string path, int pageNumber) => Render(path, pageNumber, RenderScale);

    [SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "PDF render is synchronous and runs off the UI thread.")]
    public static byte[] Render(string path, int pageNumber, double renderScale)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        using var document = WindowsPdfDocument.Open(path);
        return document.Render(pageNumber, renderScale);
    }
}

public sealed class WindowsPdfDocument : IDisposable
{
    private PdfDocument? document;

    private WindowsPdfDocument(PdfDocument document)
    {
        this.document = document;
    }

    public int PageCount => (int)Document.PageCount;

    private PdfDocument Document => document ?? throw new ObjectDisposedException(nameof(WindowsPdfDocument));

    [SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "PDF render is synchronous and runs off the UI thread.")]
    public static WindowsPdfDocument Open(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var fullPath = Path.GetFullPath(path);
        var file = StorageFile.GetFileFromPathAsync(fullPath).AsTask().GetAwaiter().GetResult();
        var document = PdfDocument.LoadFromFileAsync(file).AsTask().GetAwaiter().GetResult();
        return new WindowsPdfDocument(document);
    }

    [SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "PDF render is synchronous and runs off the UI thread.")]
    public byte[] Render(int pageNumber) => Render(pageNumber, WindowsPdfPageRenderer.RenderScale);

    [SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "PDF render is synchronous and runs off the UI thread.")]
    public byte[] Render(int pageNumber, double renderScale)
    {
        if (pageNumber < 1 || pageNumber > PageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        using var page = Document.GetPage((uint)pageNumber - 1);
        using var stream = new InMemoryRandomAccessStream();
        var options = new PdfPageRenderOptions
        {
            DestinationWidth = (uint)Math.Max(1, page.Size.Width * renderScale),
            DestinationHeight = (uint)Math.Max(1, page.Size.Height * renderScale),
        };
        page.RenderToStreamAsync(stream, options).AsTask().GetAwaiter().GetResult();
        stream.Seek(0);
        using var reader = new global::Windows.Storage.Streams.DataReader(stream);
        var size = (uint)stream.Size;
        reader.LoadAsync(size).AsTask().GetAwaiter().GetResult();
        var buffer = new byte[size];
        reader.ReadBytes(buffer);
        return buffer;
    }

    public void Dispose() => document = null;
}
#endif
