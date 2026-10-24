using Recall.Core.Ports;
using Recall.Core.Preferences;
using System.Globalization;

namespace Recall.Core.Locale;

public sealed class LocaleService
{
    private readonly IUiLocaleStore store;

    public LocaleService(IUiLocaleStore store)
    {
        this.store = store;
    }

    public UiLocale GetLocale()
    {
        return store.Get();
    }

    public void SetLocale(UiLocale locale)
    {
        store.Set(locale);
        ApplyCulture(locale);
    }

    public UiLocale ToggleLocale()
    {
        var next = GetLocale() == UiLocale.Ar ? UiLocale.En : UiLocale.Ar;
        SetLocale(next);
        return next;
    }

    public CultureInfo GetCulture()
    {
        return GetLocale() == UiLocale.Ar
            ? CultureInfo.GetCultureInfo(UiLanguage.Arabic)
            : CultureInfo.GetCultureInfo(UiLanguage.English);
    }

    public string LangCode()
    {
        return GetLocale() == UiLocale.Ar ? UiLanguage.Arabic : UiLanguage.English;
    }

    public string Dir()
    {
        return GetLocale() == UiLocale.Ar ? UiLanguage.RightToLeft : UiLanguage.LeftToRight;
    }

    public static string ShapeDigits(string? text) => text ?? string.Empty;

    public void ApplyCulture(UiLocale? locale = null)
    {
        var culture = locale ?? GetLocale();
        var cultureInfo = culture == UiLocale.Ar
            ? CultureInfo.GetCultureInfo(UiLanguage.Arabic)
            : CultureInfo.GetCultureInfo(UiLanguage.English);
        CultureInfo.CurrentUICulture = cultureInfo;
        CultureInfo.CurrentCulture = cultureInfo;
    }
}
