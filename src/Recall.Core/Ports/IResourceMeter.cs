namespace Recall.Core.Ports;

public readonly record struct ResourceSample
{
    public ResourceSample(long workingSetBytes, double cpuPercent, double? gpuPercent)
    {
        if (workingSetBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workingSetBytes));
        }

        if (cpuPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(cpuPercent));
        }

        if (gpuPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(gpuPercent));
        }

        WorkingSetBytes = workingSetBytes;
        CpuPercent = cpuPercent;
        GpuPercent = gpuPercent;
    }

    public long WorkingSetBytes { get; }

    public double CpuPercent { get; }

    public double? GpuPercent { get; }
}

public interface IResourceMeter
{
    ResourceSample Read();
}
