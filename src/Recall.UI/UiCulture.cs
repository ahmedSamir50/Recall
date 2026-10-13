using System.Globalization;

namespace Recall.UI;

public static class UiCulture
{
    public static string Lang(CultureInfo culture)
    {
        if (culture.TwoLetterISOLanguageName == "ar")
        {
            return "ar";
        }

        return "en";
    }

    public static string Dir(CultureInfo culture)
    {
        if (Lang(culture) == "ar")
        {
            return "rtl";
        }

        return "ltr";
    }
}
