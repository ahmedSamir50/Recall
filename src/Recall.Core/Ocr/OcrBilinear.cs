namespace Recall.Core.Ocr;

public readonly record struct OcrRgb(float B, float G, float R);

public static class OcrBilinear
{
    public static OcrRgb Sample(byte[] bgra, int srcWidth, int srcHeight, int dstX, int dstY, int dstWidth, int dstHeight)
    {
        ArgumentNullException.ThrowIfNull(bgra);
        if (srcWidth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(srcWidth));
        }

        if (srcHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(srcHeight));
        }

        if (dstWidth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dstWidth));
        }

        if (dstHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dstHeight));
        }

        if (dstX < 0 || dstX >= dstWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(dstX));
        }

        if (dstY < 0 || dstY >= dstHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(dstY));
        }

        if (bgra.Length != srcWidth * srcHeight * 4)
        {
            throw new ArgumentException("BGRA length must equal width times height times 4.");
        }

        var fx = (dstX + 0.5d) * srcWidth / dstWidth - 0.5d;
        var fy = (dstY + 0.5d) * srcHeight / dstHeight - 0.5d;
        var x0 = (int)Math.Floor(fx);
        var x1 = x0 + 1;
        var wx = fx - x0;
        if (fx < 0)
        {
            x0 = 0;
            x1 = 0;
            wx = 0;
        }

        if (x0 >= srcWidth - 1)
        {
            x0 = srcWidth - 1;
            x1 = x0;
            wx = 0;
        }
        else if (x1 >= srcWidth)
        {
            x1 = srcWidth - 1;
        }

        var y0 = (int)Math.Floor(fy);
        var y1 = y0 + 1;
        var wy = fy - y0;
        if (fy < 0)
        {
            y0 = 0;
            y1 = 0;
            wy = 0;
        }

        if (y0 >= srcHeight - 1)
        {
            y0 = srcHeight - 1;
            y1 = y0;
            wy = 0;
        }
        else if (y1 >= srcHeight)
        {
            y1 = srcHeight - 1;
        }

        float b = 0f;
        float g = 0f;
        float r = 0f;
        for (var channel = 0; channel < 3; channel++)
        {
            var p00 = (float)bgra[(y0 * srcWidth + x0) * 4 + channel];
            var p10 = (float)bgra[(y0 * srcWidth + x1) * 4 + channel];
            var p01 = (float)bgra[(y1 * srcWidth + x0) * 4 + channel];
            var p11 = (float)bgra[(y1 * srcWidth + x1) * 4 + channel];
            var top = p00 + (p10 - p00) * wx;
            var bottom = p01 + (p11 - p01) * wx;
            var value = (float)(top + (bottom - top) * wy);
            if (channel == 0)
            {
                b = value;
            }
            else if (channel == 1)
            {
                g = value;
            }
            else
            {
                r = value;
            }
        }

        return new OcrRgb(b, g, r);
    }
}
