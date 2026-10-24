using Recall.Core.Locale;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using System.Globalization;

namespace Recall.UI.State;

public sealed record ShellUiSnapshot(
    ThemePreference ThemePreference,
    string EffectiveTheme,
    UiLocale Locale,
    string Lang,
    string Dir,
    string RoutePath,
    IReadOnlyList<ScopeFolderGrant> Grants,
    bool CanContinueFolders)
{
    public CultureInfo Culture => Locale == UiLocale.Ar
        ? CultureInfo.GetCultureInfo(UiLanguage.Arabic)
        : CultureInfo.GetCultureInfo(UiLanguage.English);

    public static ShellUiSnapshot CreateInitial(
        ThemePreference themePreference,
        string effectiveTheme,
        UiLocale locale,
        string routePath,
        IReadOnlyList<ScopeFolderGrant> grants,
        bool canContinueFolders)
    {
        var lang = locale == UiLocale.Ar ? UiLanguage.Arabic : UiLanguage.English;
        var dir = locale == UiLocale.Ar ? UiLanguage.RightToLeft : UiLanguage.LeftToRight;
        return new ShellUiSnapshot(
            themePreference,
            effectiveTheme,
            locale,
            lang,
            dir,
            routePath,
            grants,
            canContinueFolders);
    }
}
