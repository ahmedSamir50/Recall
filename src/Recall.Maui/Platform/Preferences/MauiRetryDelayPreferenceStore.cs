using Recall.Core.Ports;
using Recall.Core.Preferences;

namespace Recall.Maui.Platform;

public sealed class MauiRetryDelayPreferenceStore : IRetryDelayPreferenceStore
{
    private const string Key = "recall.preferences.retryMinutes";

    public int Get() => Preferences.Default.Get(Key, RetryDelayPolicy.DefaultMinutes);

    public void Set(int minutes) => Preferences.Default.Set(Key, minutes);
}