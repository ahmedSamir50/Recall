namespace Recall.Core.Ocr;

public static class OcrDetectorResize
{
    public const int MinSide = 64;

    public const int MaxSide = 4000;

    public const int Stride = 32;

    public static (int Width, int Height) Size(int width, int height)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var ratio = 1d;
        if (Math.Min(width, height) < MinSide)
        {
            ratio = MinSide / (double)Math.Min(width, height);
        }

        var resizeWidth = (int)(width * ratio);
        var resizeHeight = (int)(height * ratio);
        if (Math.Max(resizeWidth, resizeHeight) > MaxSide)
        {
            var cap = MaxSide / (double)Math.Max(resizeWidth, resizeHeight);
            resizeWidth = (int)(resizeWidth * cap);
            resizeHeight = (int)(resizeHeight * cap);
        }

        resizeWidth = Math.Max(Stride, (int)Math.Round(resizeWidth / (double)Stride, MidpointRounding.ToEven) * Stride);
        resizeHeight = Math.Max(Stride, (int)Math.Round(resizeHeight / (double)Stride, MidpointRounding.ToEven) * Stride);
        return (resizeWidth, resizeHeight);
    }
}
