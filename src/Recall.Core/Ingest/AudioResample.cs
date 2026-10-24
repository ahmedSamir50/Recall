namespace Recall.Core.Ingest;

public static class AudioResample
{
    public static float[] To16k(float[] source, int sourceRate)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Length == 0)
        {
            return [];
        }

        if (sourceRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRate));
        }

        if (sourceRate == AudioWindows.SampleRate)
        {
            return source;
        }

        var targetRate = AudioWindows.SampleRate;
        var outputLength = (int)((long)source.Length * targetRate / sourceRate);
        var output = new float[outputLength];
        for (var i = 0; i < outputLength; i++)
        {
            var position = (long)i * sourceRate / (double)targetRate;
            var floorIndex = (int)Math.Floor(position);
            var nextIndex = Math.Min(floorIndex + 1, source.Length - 1);
            var fraction = position - floorIndex;
            output[i] = (float)((source[floorIndex] * (1d - fraction)) + (source[nextIndex] * fraction));
        }

        return output;
    }
}
