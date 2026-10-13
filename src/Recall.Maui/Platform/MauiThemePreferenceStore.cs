using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Theme;

namespace Recall.Maui.Platform;

public sealed class MauiThemePreferenceStore : IThemePreferenceStore
{
    private const string Key = "recall.theme";

    public ThemePreference Get()
    {
        return ThemeResolution.ParseStored(Preferences.Default.Get(Key, ThemePreference.System.ToString()));
    }

    public void Set(ThemePreference preference)
    {
        Preferences.Default.Set(Key, preference.ToString());
    }
}
