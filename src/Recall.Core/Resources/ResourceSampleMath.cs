namespace Recall.Core.Resources;

public static class ResourceSampleMath
{
    public static double? GpuAverage(IReadOnlyList<float> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            return null;
        }

        var mean = values.Average();
        return Math.Clamp(mean, 0, 100);
    }
}
