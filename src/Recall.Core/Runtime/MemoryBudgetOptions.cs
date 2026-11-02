namespace Recall.Core.Runtime;

public sealed class MemoryBudgetOptions
{
    public const string SectionName = "MemoryBudget";

    public long ComputeScratchBytes { get; set; } = 1_342_177_280;

    public long ReserveBytes { get; set; } = 268_435_456;

    public int CooldownSeconds { get; set; } = 30;

    public int MaxCooldownSeconds { get; set; } = 300;

    public long LargeBufferBytes { get; set; } = 8_388_608;
}
