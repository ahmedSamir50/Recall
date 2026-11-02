namespace Recall.Core.Runtime;

public sealed class ProcessMemoryBudget : IMemoryBudget
{
    private readonly long modelWeightsBytes;
    private readonly MemoryBudgetOptions options;
    private readonly MemoryFloorStore floors;
    private readonly long frameworkBaseBytes;
    private int observed;

    public ProcessMemoryBudget(
        long modelWeightsBytes,
        MemoryBudgetOptions options,
        MemoryFloorStore floors,
        long? frameworkBaseBytes = null)
    {
        this.modelWeightsBytes = Math.Max(0, modelWeightsBytes);
        this.options = options;
        this.floors = floors;
        this.frameworkBaseBytes = frameworkBaseBytes ?? Math.Max(0, Environment.WorkingSet);
    }

    public long CapacityBytes => GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

    public long WorkingSetBytes => Math.Max(0, Environment.WorkingSet);

    public long FloorBytes => MemoryBudgetPolicy.FloorBytes(
        modelWeightsBytes,
        options.ComputeScratchBytes,
        options.ReserveBytes,
        frameworkBaseBytes);

    public long EffectiveFloorBytes => Math.Max(FloorBytes, floors.LoadObserved());

    public DeviceVerdict Verdict => MemoryBudgetPolicy.Verdict(CapacityBytes, EffectiveFloorBytes);

    public MemoryPressure Pressure => MemoryBudgetPolicy.Classify(CapacityBytes, WorkingSetBytes, EffectiveFloorBytes);

    public bool MayLoadFile(long fileBytes, int expansion) =>
        MemoryBudgetPolicy.MayLoadFile(fileBytes, expansion, CapacityBytes, WorkingSetBytes);

    public void RecordObserved()
    {
        if (Interlocked.Exchange(ref observed, 1) != 0)
        {
            return;
        }

        var measured = Math.Max(0, WorkingSetBytes - frameworkBaseBytes);
        if (measured > EffectiveFloorBytes)
        {
            floors.SaveObserved(measured);
        }
    }
}
