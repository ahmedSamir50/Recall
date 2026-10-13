using System.Globalization;
using Recall.Core.Ports;
using Recall.Core.Preferences;

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
            ? CultureInfo.GetCultureInfo("ar")
            : CultureInfo.GetCultureInfo("en");
    }

    public string LangCode()
    {
        return GetLocale() == UiLocale.Ar ? "ar" : "en";
    }

    public string Dir()
    {
        return GetLocale() == UiLocale.Ar ? "rtl" : "ltr";
    }

    public string ShapeDigits(string? text)
    {
        if (GetLocale() != UiLocale.Ar)
        {
            return text ?? string.Empty;
        }

        return ArabicDigitShaping.ShapeForArabicUi(text);
    }

    public void ApplyCulture(UiLocale? locale = null)
    {
        var culture = locale ?? GetLocale();
        var cultureInfo = culture == UiLocale.Ar
            ? CultureInfo.GetCultureInfo("ar")
            : CultureInfo.GetCultureInfo("en");
        CultureInfo.CurrentUICulture = cultureInfo;
        CultureInfo.CurrentCulture = cultureInfo;
    }
}
