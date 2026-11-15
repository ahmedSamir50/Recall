using Recall.Core.Ports;

namespace Recall.Core.Preferences;

public sealed class MemoryRetryDelayPreferenceStore : IRetryDelayPreferenceStore
{
    private int minutes = RetryDelayPolicy.DefaultMinutes;

    public int Get() => minutes;

    public void Set(int minutes) => this.minutes = minutes;
}