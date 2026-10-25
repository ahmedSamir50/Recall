#if WINDOWS
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Recall.Maui.Platform.Ocr;

public static class WindowsPdfPageRenderer
{
    private const double Scale = 300d / 96d;

    [SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "PDF render is synchronous and runs off the UI thread.")]
    public static byte[] Render(string path, int pageNumber)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        var file = StorageFile.GetFileFromPathAsync(path).AsTask().GetAwaiter().GetResult();
        var document = PdfDocument.LoadFromFileAsync(file).AsTask().GetAwaiter().GetResult();
        if (pageNumber > document.PageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        using var page = document.GetPage((uint)pageNumber - 1);
        using var stream = new InMemoryRandomAccessStream();
        var options = new PdfPageRenderOptions
        {
            DestinationWidth = (uint)Math.Max(1, page.Size.Width * Scale),
            DestinationHeight = (uint)Math.Max(1, page.Size.Height * Scale),
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
}
#endif

