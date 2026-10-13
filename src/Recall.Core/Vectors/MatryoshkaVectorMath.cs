using Recall.Core.Faults;

namespace Recall.Core.Vectors;

public static class MatryoshkaVectorMath
{
    public const int NativeWidth = 768;

    public static void L2Normalize(Span<float> vector)
    {
        var sum = 0d;
        foreach (var value in vector)
        {
            sum += value * (double)value;
        }

        if (sum <= 0d)
        {
            return;
        }

        var inv = (float)(1d / Math.Sqrt(sum));
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] *= inv;
        }
    }

    public static float[] CutAndNormalize(ReadOnlySpan<float> full768, int storedWidth)
    {
        if (full768.Length != NativeWidth)
        {
            throw new RecallFaultException(RecallFault.EmbeddingWidthUnexpected);
        }

        if (storedWidth <= 0 || storedWidth > NativeWidth)
        {
            throw new RecallFaultException(RecallFault.TextWidthNot512);
        }

        var slice = new float[storedWidth];
        full768[..storedWidth].CopyTo(slice);
        L2Normalize(slice);
        return slice;
    }

    public static double L2Length(ReadOnlySpan<float> vector)
    {
        var sum = 0d;
        foreach (var value in vector)
        {
            sum += value * value;
        }

        return Math.Sqrt(sum);
    }
}
