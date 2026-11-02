using System.Globalization;

namespace Recall.UI.Layout;

public static class ResourceSampleFormat
{
    public static string RamMegabytes(long bytes, string unit) =>
        string.Create(CultureInfo.InvariantCulture, $"{bytes / 1048576} {unit}");

    public static string CpuPercent(double percent) =>
        string.Create(CultureInfo.InvariantCulture, $"{(int)percent}%");

    public static string? GpuValue(double? percent) =>
        percent is null ? null : string.Create(CultureInfo.InvariantCulture, $"{(int)percent.Value}%");
}
