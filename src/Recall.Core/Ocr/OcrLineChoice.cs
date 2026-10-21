namespace Recall.Core.Ocr;

public static class OcrLineChoice
{
    public static string Choose(string? englishText, double englishScore, string? arabicText, double arabicScore)
    {
        var english = englishText ?? string.Empty;
        if (englishText is null)
        {
            englishScore = 0;
        }

        var arabic = arabicText ?? string.Empty;
        if (arabicText is null)
        {
            arabicScore = 0;
        }

        if (english.Length == 0 && arabic.Length == 0)
        {
            return string.Empty;
        }

        if (arabicScore > englishScore + 1e-6)
        {
            return arabic;
        }

        return english;
    }
}
