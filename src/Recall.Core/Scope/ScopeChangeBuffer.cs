namespace Recall.Core.Scope;

public enum ScopeChangeBufferAddResult
{
    Ignored = 0,
    Added = 1,
    Overflow = 2,
}

public sealed class ScopeChangeBuffer
{
    private readonly object gate = new();
    private readonly Dictionary<string, ScopeChange> items = new(StringComparer.OrdinalIgnoreCase);
    private bool needsRescan;

    public bool NeedsRescan
    {
        get
        {
            lock (gate)
            {
                return needsRescan;
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return items.Count;
            }
        }
    }

    public ScopeChangeBufferAddResult TryAdd(ScopeChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        lock (gate)
        {
            if (string.IsNullOrWhiteSpace(change.Path))
            {
                return ScopeChangeBufferAddResult.Ignored;
            }

            if (items.ContainsKey(change.Path))
            {
                items[change.Path] = change;
                return ScopeChangeBufferAddResult.Added;
            }

            if (items.Count >= ScopeWatchLimits.ChangeCapacity)
            {
                needsRescan = true;
                return ScopeChangeBufferAddResult.Overflow;
            }

            items[change.Path] = change;
            return ScopeChangeBufferAddResult.Added;
        }
    }

    public IReadOnlyList<ScopeChange> Drain()
    {
        lock (gate)
        {
            var list = items.Values.ToList();
            items.Clear();
            return list;
        }
    }

    public void AcknowledgeRescan()
    {
        lock (gate)
        {
            needsRescan = false;
        }
    }

    public void RequestRescan()
    {
        lock (gate)
        {
            needsRescan = true;
        }
    }
}
