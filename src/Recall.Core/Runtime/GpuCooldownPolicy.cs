namespace Recall.Core.Runtime;

public static class GpuCooldownPolicy
{
    public const int HotCelsius = 80;

    public static bool Blocks(int? celsius) => celsius is not null && celsius > HotCelsius;
}