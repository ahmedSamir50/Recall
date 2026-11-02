namespace Recall.Core.Runtime;

public static class MemoryBudgetPolicy
{
    public const int SoftPercent = 40;

    public const int HardPercent = 50;

    public const int ResumePercent = 35;

    public const int TextBytesPerChar = 2;

    public const int AudioExpansion = 2;

    public const int VideoExpansion = 1;

    public const int ImageExpansion = 1;

    public static long LimitBytes(long capacityBytes) =>
        capacityBytes <= 0 ? 0 : capacityBytes * HardPercent / 100;

    public static long HeadroomBytes(long capacityBytes, long workingSetBytes) =>
        Math.Max(0, LimitBytes(capacityBytes) - Math.Max(0, workingSetBytes));

    public static long FloorBytes(long modelWeightsBytes, long computeScratchBytes, long reserveBytes, long frameworkBaseBytes) =>
        Math.Max(0, modelWeightsBytes)
        + Math.Max(0, computeScratchBytes)
        + Math.Max(0, reserveBytes)
        + Math.Max(0, frameworkBaseBytes);

    public static DeviceVerdict Verdict(long capacityBytes, long floorBytes)
    {
        if (capacityBytes <= 0)
        {
            return DeviceVerdict.Ok;
        }

        var floor = Math.Max(0, floorBytes);
        if (floor * 100 > capacityBytes * HardPercent)
        {
            return DeviceVerdict.BelowMinimum;
        }

        return floor * 100 > capacityBytes * SoftPercent ? DeviceVerdict.Tight : DeviceVerdict.Ok;
    }

    public static MemoryPressure Classify(long capacityBytes, long workingSetBytes, long floorBytes)
    {
        if (capacityBytes <= 0)
        {
            return MemoryPressure.Normal;
        }

        var effective = Math.Max(Math.Max(0, workingSetBytes), Math.Max(0, floorBytes));
        if (effective * 100 >= capacityBytes * HardPercent)
        {
            return MemoryPressure.Hard;
        }

        return effective * 100 >= capacityBytes * SoftPercent ? MemoryPressure.Soft : MemoryPressure.Normal;
    }

    public static bool MayResume(long capacityBytes, long workingSetBytes, long floorBytes) =>
        capacityBytes > 0
        && Math.Max(Math.Max(0, workingSetBytes), Math.Max(0, floorBytes)) * 100 <= capacityBytes * ResumePercent;

    public static bool MayLoadFile(long fileBytes, int expansion, long capacityBytes, long workingSetBytes) =>
        fileBytes > 0
        && expansion > 0
        && fileBytes * expansion <= HeadroomBytes(capacityBytes, workingSetBytes);

    public static long MinimumSupportedBytes(long floorBytes) =>
        Math.Max(0, floorBytes) * 100 / HardPercent;

    public static long RecommendedBytes(long floorBytes) =>
        Math.Max(0, floorBytes) * 100 / 30;
}
