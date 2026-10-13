using Recall.Core.Ports;
using Recall.Core.Preferences;

namespace Recall.Maui.Platform;

public sealed class MauiUiLocaleStore : IUiLocaleStore
{
    private const string Key = "recall.locale";

    public UiLocale Get()
    {
        var raw = Preferences.Default.Get(Key, UiLocale.En.ToString());
        return Enum.TryParse<UiLocale>(raw, ignoreCase: true, out var parsed) ? parsed : UiLocale.En;
    }

    public void Set(UiLocale locale)
    {
        Preferences.Default.Set(Key, locale.ToString());
    }
}
