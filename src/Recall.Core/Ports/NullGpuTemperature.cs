namespace Recall.Core.Ports;

public sealed class NullGpuTemperature : IGpuTemperature
{
    public int? ReadCelsius() => null;
}