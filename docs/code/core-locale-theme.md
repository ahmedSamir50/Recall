## src/Recall.Core/Locale/LocaleService.cs

```csharp
namespace Recall.Core.Locale;
public sealed class LocaleService
    public LocaleService(IUiLocaleStore store)
    public UiLocale GetLocale()
    public void SetLocale(UiLocale locale)
    public UiLocale ToggleLocale()
    public CultureInfo GetCulture()
    public string LangCode()
    public string Dir()
    public static string ShapeDigits(string? text) => text ?? string.Empty;
    public void ApplyCulture(UiLocale? locale = null)
```

No branch.


## src/Recall.Core/Locale/UiLanguage.cs

```csharp
namespace Recall.Core.Locale;
public static class UiLanguage
```

No branch.


## src/Recall.Core/Preferences/UiLocale.cs

```csharp
namespace Recall.Core.Preferences;
public enum UiLocale
```

No branch.


## src/Recall.Core/Theme/ThemeService.cs

```csharp
namespace Recall.Core.Theme;
public sealed class ThemeService
    public ThemeService(IThemePreferenceStore store, ISystemThemeProvider systemTheme)
    public ThemePreference GetPreference()
    public void SetPreference(ThemePreference preference)
    public string GetEffectiveTheme()
    public ThemePreference CyclePreference()
```

No branch.


## src/Recall.Core/Theme/ThemeResolution.cs

```csharp
namespace Recall.Core.Theme;
public static class ThemeResolution
    public static string ResolveEffectiveTheme(ThemePreference preference, bool osIsDark)
    public static ThemePreference ParseStored(string? value)
        if (string.IsNullOrWhiteSpace(value))
```


## src/Recall.Core/Preferences/ThemePreference.cs

```csharp
namespace Recall.Core.Preferences;
public enum ThemePreference
```

No branch.


## src/Recall.Core/Preferences/AgeCutoffPreferenceService.cs

```csharp
namespace Recall.Core.Preferences;
public sealed class AgeCutoffPreferenceService
    public AgeCutoffPreferenceService(IAgeCutoffPreferenceStore store)
    public AgeCutoffYears Get() => store.Get();
    public void Set(AgeCutoffYears years)
        if (years is not (AgeCutoffYears.Two or AgeCutoffYears.Five or AgeCutoffYears.Seven))
            throw new RecallFaultException(RecallFault.AgeCutoffNotAllowed);
```


## src/Recall.Core/Ports/PreferencePorts.cs

```csharp
namespace Recall.Core.Ports;
public interface IThemePreferenceStore
public interface IUiLocaleStore
public interface ISystemThemeProvider
public interface IAgeCutoffPreferenceStore
public enum RecallShellKind
public interface IRecallShellKind
    ThemePreference Get();
    void Set(ThemePreference preference);
    UiLocale Get();
    void Set(UiLocale locale);
    bool IsDark();
    AgeCutoffYears Get();
    void Set(AgeCutoffYears years);
```

No branch.
