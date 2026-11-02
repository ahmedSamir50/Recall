#if WINDOWS
using System.Drawing.Imaging;
using SD = System.Drawing;

namespace Recall.Maui.Platform.Ocr;

public static class WindowsTextPageRenderer
{
    public static byte[] Render(string text) => Render(text, 2480, 3508);

    public static byte[] Render(string text, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (width < 640 || height < 640)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Golden and ingest renders need at least 640 px.");
        }

        using var bitmap = new SD.Bitmap(width, height);
        using var graphics = SD.Graphics.FromImage(bitmap);
        graphics.Clear(SD.Color.White);
        using var font = new SD.Font("Tahoma", 16f, SD.FontStyle.Regular, SD.GraphicsUnit.Point);
        using var brush = new SD.SolidBrush(SD.Color.Black);
        graphics.DrawString(text, font, brush, new SD.RectangleF(40, 40, width - 80, height - 80));
        using var stream = new MemoryStream();
        bitmap.Save(stream, SD.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }
}
#endif
