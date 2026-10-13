namespace Recall.Core.Locale;

public static class ArabicDigitShaping
{
    private static readonly char[] ArabicIndicDigits = ['٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩'];

    public static string ShapeForArabicUi(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '0' and <= '9')
            {
                chars[i] = ArabicIndicDigits[chars[i] - '0'];
            }
        }

        return new string(chars);
    }
}
