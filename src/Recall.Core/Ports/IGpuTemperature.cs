namespace Recall.Core.Ports;

public interface IGpuTemperature
{
    int? ReadCelsius();
}