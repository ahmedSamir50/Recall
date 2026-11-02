using FluentAssertions;
using Recall.Core.Runtime;

namespace Recall.UnitTests;

public sealed class MemoryGateTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    private const long Gib = 1024L * 1024 * 1024;

    private static MemoryGate NewGate() => new(new MemoryBudgetOptions { CooldownSeconds = 30, MaxCooldownSeconds = 300 });

    [Fact]
    public void Open_OnHard_CoolsDownForThirtySeconds()
    {
        var gate = NewGate();

        gate.Observe(MemoryPressure.Hard, 64 * Gib, 40 * Gib, 0, T0);

        gate.State.Should().Be(MemoryGateState.Cooling);
        gate.IsOpen().Should().BeFalse();

        gate.Observe(MemoryPressure.Normal, 64 * Gib, 1 * Gib, 0, T0.AddSeconds(29));
        gate.IsOpen().Should().BeFalse();

        gate.Observe(MemoryPressure.Normal, 64 * Gib, 1 * Gib, 0, T0.AddSeconds(30));
        gate.IsOpen().Should().BeTrue();
    }

    [Fact]
    public void Cooling_DoublesBackoffWhilePressureStaysHigh()
    {
        var gate = NewGate();
        gate.Observe(MemoryPressure.Hard, 64 * Gib, 40 * Gib, 0, T0);

        gate.Observe(MemoryPressure.Hard, 64 * Gib, 40 * Gib, 0, T0.AddSeconds(30));
        gate.NextAttemptAt.Should().Be(T0.AddSeconds(90));

        gate.Observe(MemoryPressure.Hard, 64 * Gib, 40 * Gib, 0, T0.AddSeconds(90));
        gate.NextAttemptAt.Should().Be(T0.AddSeconds(210));
        gate.State.Should().Be(MemoryGateState.Cooling);
    }

    [Fact]
    public void Cooling_DoesNotOpenAtFortyPercent()
    {
        var gate = NewGate();
        gate.Observe(MemoryPressure.Hard, 1000, 500, 0, T0);

        gate.Observe(MemoryPressure.Soft, 1000, 400, 0, T0.AddSeconds(30));

        gate.State.Should().Be(MemoryGateState.Cooling);
        gate.IsOpen().Should().BeFalse();
    }

    [Fact]
    public void FileCompleted_ResetsBackoff()
    {
        var gate = NewGate();
        gate.Observe(MemoryPressure.Hard, 64 * Gib, 40 * Gib, 0, T0);
        gate.Observe(MemoryPressure.Hard, 64 * Gib, 40 * Gib, 0, T0.AddSeconds(30));

        gate.OnFileCompleted();
        gate.Backoff.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Blocked_NeverOpens()
    {
        var gate = NewGate();
        gate.Block();

        gate.Observe(MemoryPressure.Normal, 64 * Gib, 1 * Gib, 0, T0.AddHours(1));

        gate.IsOpen().Should().BeFalse();
        gate.State.Should().Be(MemoryGateState.Blocked);
    }
}
