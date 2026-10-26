namespace Recall.Core.Ocr;

public readonly record struct OcrReaderCanvas(int CanvasWidth, int ContentWidth);

public static class OcrReaderResize
{
    public const int Height = 48;

    public const int MinCanvasWidth = 320;

    public const int MaxCanvasWidth = 3200;

    public static OcrReaderCanvas Canvas(int cropWidth, int cropHeight)
    {
        if (cropWidth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cropWidth));
        }

        if (cropHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cropHeight));
        }

        var ratio = cropWidth / (double)cropHeight;
        var maxWh = Math.Max(MinCanvasWidth / (double)Height, ratio);
        var canvas = (int)(Height * maxWh);
        if (canvas > MaxCanvasWidth)
        {
            canvas = MaxCanvasWidth;
        }

        var content = (int)Math.Ceiling(Height * ratio);
        if (content > canvas)
        {
            content = canvas;
        }

        if (content < 1)
        {
            content = 1;
        }

        return new OcrReaderCanvas(canvas, content);
    }
}
