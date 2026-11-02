namespace Recall.Core.Runtime;

public interface IMemoryBudget
{
    long CapacityBytes { get; }

    long WorkingSetBytes { get; }

    long FloorBytes { get; }

    long EffectiveFloorBytes { get; }

    DeviceVerdict Verdict { get; }

    MemoryPressure Pressure { get; }

    bool MayLoadFile(long fileBytes, int expansion);

    void RecordObserved();
}
