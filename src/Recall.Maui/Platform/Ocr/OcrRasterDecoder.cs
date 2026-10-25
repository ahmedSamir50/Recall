using System.Diagnostics.CodeAnalysis;

namespace Recall.Maui.Platform.Ocr;

internal static class OcrRasterDecoder
{
    public static OcrRaster Decode(byte[] imageBytes)
    {
#if WINDOWS
        return DecodeWindows(imageBytes);
#elif ANDROID
        return DecodeAndroid(imageBytes);
#elif IOS || MACCATALYST
        return DecodeApple(imageBytes);
#else
        throw new PlatformNotSupportedException();
#endif
    }

#if WINDOWS
    [SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "OCR decode is synchronous and runs off the UI thread.")]
    private static OcrRaster DecodeWindows(byte[] imageBytes)
    {
        using var stream = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
        stream.WriteAsync(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(imageBytes)).AsTask().GetAwaiter().GetResult();
        stream.Seek(0);
        var decoder = global::Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream).AsTask().GetAwaiter().GetResult();
        var software = decoder.GetSoftwareBitmapAsync(
            global::Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            global::Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied).AsTask().GetAwaiter().GetResult();
        var width = software.PixelWidth;
        var height = software.PixelHeight;
        var bytes = new byte[width * height * 4];
        software.CopyToBuffer(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(bytes));
        return new OcrRaster(width, height, bytes);
    }
#endif

#if ANDROID
    private static OcrRaster DecodeAndroid(byte[] imageBytes)
    {
        using var bitmap = Android.Graphics.BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length)
            ?? throw new InvalidOperationException("Image decode failed.");
        var width = bitmap.Width;
        var height = bitmap.Height;
        var pixels = new int[width * height];
        bitmap.GetPixels(pixels, 0, width, 0, 0, width, height);
        var bgra = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++)
        {
            var color = pixels[i];
            var offset = i * 4;
            bgra[offset] = (byte)color;
            bgra[offset + 1] = (byte)(color >> 8);
            bgra[offset + 2] = (byte)(color >> 16);
            bgra[offset + 3] = (byte)(color >> 24);
        }

        return new OcrRaster(width, height, bgra);
    }
#endif

#if IOS || MACCATALYST
    private static OcrRaster DecodeApple(byte[] imageBytes)
    {
        using var data = Foundation.NSData.FromArray(imageBytes);
        using var image = UIKit.UIImage.LoadFromData(data) ?? throw new InvalidOperationException("Image decode failed.");
        using var cg = image.CGImage ?? throw new InvalidOperationException("Image decode failed.");
        var width = cg.Width;
        var height = cg.Height;
        var bytes = new byte[width * height * 4];
        using var context = new CoreGraphics.CGBitmapContext(
            bytes,
            width,
            height,
            8,
            width * 4,
            CoreGraphics.CGColorSpace.CreateDeviceRGB(),
            CoreGraphics.CGBitmapFlags.PremultipliedFirst | CoreGraphics.CGBitmapFlags.ByteOrder32Little);
        context.DrawImage(new CoreGraphics.CGRect(0, 0, width, height), cg);
        return new OcrRaster(width, height, bytes);
    }
#endif
}
