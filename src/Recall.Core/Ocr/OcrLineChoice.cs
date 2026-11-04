using System.Text;
using System.Text.Unicode;

namespace Recall.Core.Ocr;

public static class OcrLineChoice
{
    private const double TieEpsilon = 1e-6;

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

        if (arabicScore > englishScore + TieEpsilon)
        {
            return arabic;
        }

        return english;
    }

    /// <summary>
    /// Script union for one line. Arabic-script tokens are kept from the Arabic read, Latin-script
    /// tokens from the English read, and script-neutral tokens (digits, punctuation, currency) from
    /// the higher-confidence read. The kept blocks are joined with the script of the line's first
    /// script-bearing character first.
    /// </summary>
    public static string Union(string? englishText, double englishScore, string? arabicText, double arabicScore)
    {
        var englishTokens = Tokenize(englishText);
        var arabicTokens = Tokenize(arabicText);
        var englishIsPrimary = arabicScore <= englishScore + TieEpsilon;

        var latinKept = new List<string>();
        foreach (var token in englishTokens)
        {
            if (IsArabicScript(token))
            {
                continue;
            }

            if (englishIsPrimary || IsLatinScript(token))
            {
                latinKept.Add(token);
            }
        }

        var arabicKept = new List<string>();
        foreach (var token in arabicTokens)
        {
            if (IsLatinScript(token))
            {
                continue;
            }

            if (!englishIsPrimary || IsArabicScript(token))
            {
                arabicKept.Add(token);
            }
        }

        if (latinKept.Count == 0 && arabicKept.Count == 0)
        {
            return Choose(englishText, englishScore, arabicText, arabicScore);
        }

        if (arabicKept.Count == 0)
        {
            return string.Join(' ', latinKept);
        }

        if (latinKept.Count == 0)
        {
            return string.Join(' ', arabicKept);
        }

        var primaryTokens = englishIsPrimary ? englishTokens : arabicTokens;
        var otherTokens = englishIsPrimary ? arabicTokens : englishTokens;
        var arabicFirst = FirstScriptIsArabic(primaryTokens) ?? FirstScriptIsArabic(otherTokens) ?? false;

        return arabicFirst
            ? string.Join(' ', arabicKept.Concat(latinKept))
            : string.Join(' ', latinKept.Concat(arabicKept));
    }

    private static List<string> Tokenize(string? text)
    {
        var tokens = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            return tokens;
        }

        var start = -1;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                if (start >= 0)
                {
                    tokens.Add(text[start..i]);
                    start = -1;
                }

                continue;
            }

            if (start < 0)
            {
                start = i;
            }
        }

        if (start >= 0)
        {
            tokens.Add(text[start..]);
        }

        return tokens;
    }

    private static bool? FirstScriptIsArabic(IReadOnlyList<string> tokens)
    {
        foreach (var token in tokens)
        {
            foreach (var rune in token.EnumerateRunes())
            {
                if (IsArabicLetter(rune))
                {
                    return true;
                }

                if (IsLatinLetter(rune))
                {
                    return false;
                }
            }
        }

        return null;
    }

    private static bool IsArabicScript(string token) =>
        token.Any(ch => ch is >= '\u0600' and <= '\u06FF');

    private static bool IsLatinScript(string token) =>
        token.EnumerateRunes().Any(IsLatinLetter);

    private static bool IsArabicLetter(Rune rune) =>
        rune.Value >= UnicodeRanges.Arabic.FirstCodePoint
        && rune.Value < UnicodeRanges.Arabic.FirstCodePoint + UnicodeRanges.Arabic.Length;

    private static bool IsLatinLetter(Rune rune) =>
        (rune.Value >= 'A' && rune.Value <= 'Z')
        || (rune.Value >= 'a' && rune.Value <= 'z')
        || (rune.Value >= 0x00C0 && rune.Value <= 0x024F);
}
