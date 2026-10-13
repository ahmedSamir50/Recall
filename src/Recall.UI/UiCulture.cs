using System.Globalization;

namespace Recall.UI;

public static class UiCulture
{
    public static string Lang(CultureInfo culture)
    {
        if (culture.TwoLetterISOLanguageName == Recall.Core.Locale.UiLanguage.Arabic)
        {
            return Recall.Core.Locale.UiLanguage.Arabic;
        }

        return Recall.Core.Locale.UiLanguage.English;
    }

    public static string Dir(CultureInfo culture)
    {
        if (Lang(culture) == Recall.Core.Locale.UiLanguage.Arabic)
        {
            return Recall.Core.Locale.UiLanguage.RightToLeft;
        }

        return Recall.Core.Locale.UiLanguage.LeftToRight;
    }
}
