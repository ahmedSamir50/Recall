using Recall.Core.Ports;
using Recall.Core.Preferences;

namespace Recall.Core.Theme;

public sealed class ThemeService
{
    private readonly IThemePreferenceStore store;
    private readonly ISystemThemeProvider systemTheme;

    public ThemeService(IThemePreferenceStore store, ISystemThemeProvider systemTheme)
    {
        this.store = store;
        this.systemTheme = systemTheme;
    }

    public ThemePreference GetPreference()
    {
        return store.Get();
    }

    public void SetPreference(ThemePreference preference)
    {
        store.Set(preference);
    }

    public string GetEffectiveTheme()
    {
        return ThemeResolution.ResolveEffectiveTheme(GetPreference(), systemTheme.IsDark());
    }

    public ThemePreference CyclePreference()
    {
        var next = GetPreference() switch
        {
            ThemePreference.System => ThemePreference.Dark,
            ThemePreference.Dark => ThemePreference.Light,
            ThemePreference.Light => ThemePreference.System,
            _ => ThemePreference.System,
        };
        SetPreference(next);
        return next;
    }
}
