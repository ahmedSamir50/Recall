using System.Diagnostics;
using Recall.Core.Ports;
using Recall.Core.Resources;

namespace Recall.Maui.Platform.Resources;

public sealed class ProcessResourceMeter : IResourceMeter
{
    private readonly Dictionary<string, PerformanceCounter> gpuCounters = new(StringComparer.Ordinal);
    private bool cpuSampleReady;
    private DateTime lastWallTime = DateTime.UtcNow;
    private TimeSpan lastProcessorTime;

    public ResourceSample Read()
    {
        var workingSetBytes = ReadWorkingSetBytes();
        var cpuPercent = ReadCpuPercent();
        var gpuPercent = ReadGpuPercent();
        return new ResourceSample(workingSetBytes, cpuPercent, gpuPercent);
    }

    private static long ReadWorkingSetBytes()
    {
        double memMb;
#if ANDROID
        var memInfo = new Android.OS.Debug.MemoryInfo();
        Android.OS.Debug.GetMemoryInfo(memInfo);
        memMb = memInfo.TotalPss / 1024.0;
#elif WINDOWS
        memMb = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
#else
        memMb = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
#endif
        return (long)(memMb * 1048576);
    }

    private double ReadCpuPercent()
    {
        var process = Process.GetCurrentProcess();
        var now = DateTime.UtcNow;
        var processorTime = process.TotalProcessorTime;
        double cpuPercent = 0;
        if (cpuSampleReady)
        {
            var wallSeconds = (now - lastWallTime).TotalSeconds;
            if (wallSeconds > 0)
            {
                cpuPercent = (processorTime - lastProcessorTime).TotalSeconds / wallSeconds / Environment.ProcessorCount * 100;
                cpuPercent = Math.Clamp(cpuPercent, 0, 100);
            }
        }
        else
        {
            cpuSampleReady = true;
        }

        lastWallTime = now;
        lastProcessorTime = processorTime;
        return cpuPercent;
    }

    private double? ReadGpuPercent()
    {
#if WINDOWS
        try
        {
            if (!PerformanceCounterCategory.Exists("GPU Engine"))
            {
                return null;
            }

            var category = new PerformanceCounterCategory("GPU Engine");
            var prefix = $"pid_{Process.GetCurrentProcess().Id}_";
            var names = category.GetInstanceNames().Where(instance => instance.StartsWith(prefix, StringComparison.Ordinal)).ToArray();

            if (names.Length == 0)
            {
                return null;
            }

            var values = new List<float>(names.Length);
            foreach (var name in names)
            {
                if (!gpuCounters.TryGetValue(name, out var counter))
                {
                    counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", name, readOnly: true);
                    gpuCounters[name] = counter;
                }

                values.Add(counter.NextValue());
            }

            return ResourceSampleMath.GpuAverage(values);
        }
        catch
        {
            return null;
        }
#else
        return null;
#endif
    }
}
