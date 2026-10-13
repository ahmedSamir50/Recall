using Recall.Core.Preferences;

namespace Recall.Core.Theme;

public static class ThemeResolution
{
    public static string ResolveEffectiveTheme(ThemePreference preference, bool osIsDark)
    {
        return preference switch
        {
            ThemePreference.Dark => "dark",
            ThemePreference.Light => "light",
            ThemePreference.System => osIsDark ? "dark" : "light",
            _ => osIsDark ? "dark" : "light",
        };
    }

    public static ThemePreference ParseStored(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ThemePreference.System;
        }

        return Enum.TryParse<ThemePreference>(value, ignoreCase: true, out var parsed)
            ? parsed
            : ThemePreference.System;
    }
}
