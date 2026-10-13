using Recall.Core.Faults;

namespace Recall.Core.Vectors;

public static class TokenMeanPool
{
    public static float[] Mean(ReadOnlySpan<float> tokenRows, int hiddenWidth)
    {
        if (hiddenWidth != MatryoshkaVectorMath.NativeWidth || tokenRows.Length == 0 || tokenRows.Length % hiddenWidth != 0)
        {
            throw new RecallFaultException(RecallFault.EmbeddingWidthUnexpected);
        }

        var rows = tokenRows.Length / hiddenWidth;
        var pooled = new float[hiddenWidth];
        if (rows == 1)
        {
            tokenRows.CopyTo(pooled);
            return pooled;
        }

        for (var row = 0; row < rows; row++)
        {
            var start = row * hiddenWidth;
            for (var column = 0; column < hiddenWidth; column++)
            {
                pooled[column] += tokenRows[start + column];
            }
        }

        var scale = 1f / rows;
        for (var column = 0; column < hiddenWidth; column++)
        {
            pooled[column] *= scale;
        }

        return pooled;
    }
}
