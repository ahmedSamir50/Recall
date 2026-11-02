namespace Recall.Core.Runtime;

public enum MemoryGateState
{
    Open = 0,
    Cooling = 1,
    Blocked = 2,
}

public sealed class MemoryGate
{
    private readonly object sync = new();
    private readonly MemoryBudgetOptions options;
    private MemoryGateState state = MemoryGateState.Open;
    private DateTimeOffset nextAttemptAt;
    private TimeSpan backoff;

    public MemoryGate(MemoryBudgetOptions? options = null)
    {
        this.options = options ?? new MemoryBudgetOptions();
        backoff = Cooldown;
    }

    public MemoryGateState State
    {
        get
        {
            lock (sync)
            {
                return state;
            }
        }
    }

    public DateTimeOffset NextAttemptAt
    {
        get
        {
            lock (sync)
            {
                return nextAttemptAt;
            }
        }
    }

    public TimeSpan Backoff
    {
        get
        {
            lock (sync)
            {
                return backoff;
            }
        }
    }

    private TimeSpan Cooldown => TimeSpan.FromSeconds(options.CooldownSeconds);

    private TimeSpan MaxCooldown => TimeSpan.FromSeconds(options.MaxCooldownSeconds);

    public bool IsOpen()
    {
        lock (sync)
        {
            return state == MemoryGateState.Open;
        }
    }

    public void Observe(MemoryPressure pressure, long capacityBytes, long workingSetBytes, long floorBytes, DateTimeOffset now)
    {
        lock (sync)
        {
            if (state == MemoryGateState.Blocked)
            {
                return;
            }

            if (state == MemoryGateState.Open)
            {
                if (pressure == MemoryPressure.Hard)
                {
                    EnterCooling(now);
                }

                return;
            }

            if (now < nextAttemptAt)
            {
                return;
            }

            if (MemoryBudgetPolicy.MayResume(capacityBytes, workingSetBytes, floorBytes))
            {
                state = MemoryGateState.Open;
                backoff = Cooldown;
                return;
            }

            var doubled = TimeSpan.FromSeconds(backoff.TotalSeconds * 2);
            backoff = doubled > MaxCooldown ? MaxCooldown : doubled;
            nextAttemptAt = now + backoff;
        }
    }

    public void EnterHard(DateTimeOffset now)
    {
        lock (sync)
        {
            if (state == MemoryGateState.Open)
            {
                EnterCooling(now);
            }
        }
    }

    public void OnFileCompleted()
    {
        lock (sync)
        {
            backoff = Cooldown;
        }
    }

    public void Block()
    {
        lock (sync)
        {
            state = MemoryGateState.Blocked;
        }
    }

    private void EnterCooling(DateTimeOffset now)
    {
        state = MemoryGateState.Cooling;
        backoff = Cooldown;
        nextAttemptAt = now + backoff;
    }
}
