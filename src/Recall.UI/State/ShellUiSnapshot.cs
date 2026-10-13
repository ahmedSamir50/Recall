using System.Globalization;
using Recall.Core.Locale;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Theme;

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
        ? CultureInfo.GetCultureInfo("ar")
        : CultureInfo.GetCultureInfo("en");

    public static ShellUiSnapshot CreateInitial(
        ThemePreference themePreference,
        string effectiveTheme,
        UiLocale locale,
        string routePath,
        IReadOnlyList<ScopeFolderGrant> grants,
        bool canContinueFolders)
    {
        var lang = locale == UiLocale.Ar ? "ar" : "en";
        var dir = locale == UiLocale.Ar ? "rtl" : "ltr";
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
