using Recall.Core.Preferences;
using Recall.Core.Vocabulary;

namespace Recall.Core.Ports;

public interface IThemePreferenceStore
{
    ThemePreference Get();

    void Set(ThemePreference preference);
}

public interface IUiLocaleStore
{
    UiLocale Get();

    void Set(UiLocale locale);
}

public interface ISystemThemeProvider
{
    bool IsDark();
}

public interface IAgeCutoffPreferenceStore
{
    AgeCutoffYears Get();

    void Set(AgeCutoffYears years);
}

public interface IUserSkipStore
{
    UserSkipAdditions Load();

    void Save(UserSkipAdditions additions);
}

public enum RecallShellKind
{
    Unknown = 0,
    Windows = 1,
    Android = 2,
    Ios = 3,
    MacCatalyst = 4,
}

public interface IRecallShellKind
{
    RecallShellKind Kind { get; }
}
