using System.Numerics;

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

        return SampleUnchecked(bgra, srcWidth, srcHeight, dstX, dstY, dstWidth, dstHeight);
    }

    /// <summary>
    /// The same interpolation math as <see cref="Sample"/> without re-validating every argument per
    /// pixel. The resample loops in the input builders validate the geometry once per pass and then
    /// call this per pixel, so the per-pixel cost carries only the taps and the interpolation.
    /// </summary>
    internal static OcrRgb SampleUnchecked(byte[] bgra, int srcWidth, int srcHeight, int dstX, int dstY, int dstWidth, int dstHeight)
    {
        var (x0, x1, y0, y1, wx, wy) = ComputeTaps(srcWidth, srcHeight, dstX, dstY, dstWidth, dstHeight);
        return SampleVector(bgra, srcWidth, x0, x1, y0, y1, (float)wx, (float)wy);
    }

    /// <summary>
    /// The pre-vectorization interpolation on the full <see cref="Sample"/> geometry, kept as the
    /// equivalence reference the packed path is measured against.
    /// </summary>
    internal static OcrRgb SampleScalarReference(byte[] bgra, int srcWidth, int srcHeight, int dstX, int dstY, int dstWidth, int dstHeight)
    {
        var (x0, x1, y0, y1, wx, wy) = ComputeTaps(srcWidth, srcHeight, dstX, dstY, dstWidth, dstHeight);
        return SampleScalar(bgra, srcWidth, x0, x1, y0, y1, wx, wy);
    }

    private static (int X0, int X1, int Y0, int Y1, double Wx, double Wy) ComputeTaps(int srcWidth, int srcHeight, int dstX, int dstY, int dstWidth, int dstHeight)
    {
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

        return (x0, x1, y0, y1, wx, wy);
    }

    /// <summary>
    /// The pre-vectorization interpolation, kept verbatim (same op order, double-precision weights)
    /// as the equivalence reference the packed path is measured against.
    /// </summary>
    internal static OcrRgb SampleScalar(byte[] bgra, int srcWidth, int x0, int x1, int y0, int y1, double wx, double wy)
    {
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

    /// <summary>
    /// The hot path: the three color channels are interpolated as packed Vector3 arithmetic
    /// (System.Numerics) in the same op order as <see cref="SampleScalar"/>, one component per
    /// channel instead of a channel loop with twelve ranged reads.
    /// </summary>
    private static OcrRgb SampleVector(byte[] bgra, int srcWidth, int x0, int x1, int y0, int y1, float wx, float wy)
    {
        var stride = srcWidth * 4;
        var i00 = y0 * stride + x0 * 4;
        var i10 = y0 * stride + x1 * 4;
        var i01 = y1 * stride + x0 * 4;
        var i11 = y1 * stride + x1 * 4;
        var p00 = new Vector3(bgra[i00], bgra[i00 + 1], bgra[i00 + 2]);
        var p10 = new Vector3(bgra[i10], bgra[i10 + 1], bgra[i10 + 2]);
        var p01 = new Vector3(bgra[i01], bgra[i01 + 1], bgra[i01 + 2]);
        var p11 = new Vector3(bgra[i11], bgra[i11 + 1], bgra[i11 + 2]);
        var top = p00 + (p10 - p00) * wx;
        var bottom = p01 + (p11 - p01) * wx;
        var value = top + (bottom - top) * wy;
        return new OcrRgb(value.X, value.Y, value.Z);
    }
}
