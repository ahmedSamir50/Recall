using FluentAssertions;
using Recall.Core.Runtime;

namespace Recall.UnitTests;

public sealed class MemoryBudgetTests
{
    private const long Gib = 1024L * 1024 * 1024;

    [Fact]
    public void Verdict_BelowMinimumWhenFloorAboveHalf()
    {
        MemoryBudgetPolicy.Verdict(4 * Gib, (2 * Gib) + 1).Should().Be(DeviceVerdict.BelowMinimum);
    }

    [Fact]
    public void Verdict_TightWhenFloorAboveForty()
    {
        MemoryBudgetPolicy.Verdict(8 * Gib, 3_500_000_000).Should().Be(DeviceVerdict.Tight);
    }

    [Fact]
    public void Verdict_OkWhenFloorBelowForty()
    {
        MemoryBudgetPolicy.Verdict(32 * Gib, 3_500_000_000).Should().Be(DeviceVerdict.Ok);
    }

    [Fact]
    public void Classify_UsesFloorWhenHigherThanWorkingSet()
    {
        MemoryBudgetPolicy.Classify(8 * Gib, 0, 4 * Gib).Should().Be(MemoryPressure.Hard);
    }

    [Fact]
    public void Classify_At40Percent_IsSoft()
    {
        MemoryBudgetPolicy.Classify(1000, 400, 0).Should().Be(MemoryPressure.Soft);
    }

    [Fact]
    public void Classify_At50Percent_IsHard()
    {
        MemoryBudgetPolicy.Classify(1000, 500, 0).Should().Be(MemoryPressure.Hard);
    }

    [Fact]
    public void Classify_At39Percent_IsNormal()
    {
        MemoryBudgetPolicy.Classify(1000, 399, 0).Should().Be(MemoryPressure.Normal);
    }

    [Fact]
    public void Classify_ZeroCapacity_IsNormal()
    {
        MemoryBudgetPolicy.Classify(0, 1, 0).Should().Be(MemoryPressure.Normal);
    }

    [Fact]
    public void MayLoadFile_RejectsAboveHeadroom()
    {
        MemoryBudgetPolicy.MayLoadFile(101, 1, 1000, 400).Should().BeFalse();
        MemoryBudgetPolicy.MayLoadFile(100, 1, 1000, 400).Should().BeTrue();
    }

    [Fact]
    public void LimitBytes_IsHalfOfCapacity()
    {
        MemoryBudgetPolicy.LimitBytes(68_719_476_736).Should().Be(34_359_738_368);
    }

    [Fact]
    public void MinimumAndRecommended_FollowFloor()
    {
        MemoryBudgetPolicy.MinimumSupportedBytes(3 * Gib).Should().Be(6 * Gib);
        MemoryBudgetPolicy.RecommendedBytes(3 * Gib).Should().Be(10 * Gib);
    }

    [Fact]
    public void ReferenceProfiles_MatchPublishedTable()
    {
        var floor = 1_900_000_000;

        MemoryBudgetPolicy.Verdict(3 * Gib, floor).Should().Be(DeviceVerdict.BelowMinimum);
        MemoryBudgetPolicy.Verdict(6 * Gib, floor).Should().Be(DeviceVerdict.Ok);
        MemoryBudgetPolicy.Verdict(16 * Gib, floor).Should().Be(DeviceVerdict.Ok);
    }

    [Fact]
    public void ProcessBudget_UsesLargerOfDeclaredAndObserved()
    {
        var directory = Path.Combine(Path.GetTempPath(), "recall-floor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var floors = new MemoryFloorStore(directory);
        floors.SaveObserved(3 * Gib);
        var budget = new ProcessMemoryBudget(0, new MemoryBudgetOptions { ComputeScratchBytes = 0, ReserveBytes = 0 }, floors, 0);

        budget.FloorBytes.Should().Be(0);
        budget.EffectiveFloorBytes.Should().Be(3 * Gib);
    }

    [Fact]
    public void ProcessBudget_ObservedBelowDeclared_StaysDeclared()
    {
        var directory = Path.Combine(Path.GetTempPath(), "recall-floor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var floors = new MemoryFloorStore(directory);
        floors.SaveObserved(Gib);
        var budget = new ProcessMemoryBudget(0, new MemoryBudgetOptions { ComputeScratchBytes = 2 * Gib, ReserveBytes = 0 }, floors, 0);

        budget.EffectiveFloorBytes.Should().Be(2 * Gib);
    }

    [Fact]
    public void MemoryFloorStore_SaveThenLoad_RoundTrips()
    {
        var directory = Path.Combine(Path.GetTempPath(), "recall-floor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var store = new MemoryFloorStore(directory);

        store.SaveObserved(3 * Gib);

        store.LoadObserved().Should().Be(3 * Gib);
    }

    [Fact]
    public void MemoryFloorStore_MissingOrCorruptFile_ReturnsZero()
    {
        var directory = Path.Combine(Path.GetTempPath(), "recall-floor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var store = new MemoryFloorStore(directory);
        store.LoadObserved().Should().Be(0);

        File.WriteAllText(Path.Combine(directory, MemoryFloorStore.FileName), "{");
        store.LoadObserved().Should().Be(0);
    }
}
