namespace Recall.Maui.Platform.Ocr;

internal sealed class OcrRaster(int width, int height, byte[] bgra)
{
    public int Width { get; } = width;

    public int Height { get; } = height;

    public byte[] Bgra { get; } = bgra;

    public float GetB(int x, int y) => Bgra[(y * Width + x) * 4];

    public float GetG(int x, int y) => Bgra[(y * Width + x) * 4 + 1];

    public float GetR(int x, int y) => Bgra[(y * Width + x) * 4 + 2];
}
