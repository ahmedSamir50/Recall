using System.Text;
using System.Text.Unicode;

namespace Recall.Core.Ocr;

public static class BrokenArabic
{
    private const int Replacement = 0xFFFD;

    private const int ReplacementRun = 3;

    public static bool NeedsOcr(string? word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return false;
        }

        var hasArabicLetter = false;
        var hasReplacementRun = false;
        var run = 0;
        foreach (var rune in word.EnumerateRunes())
        {
            if (rune.Value == Replacement)
            {
                run++;
                if (run >= ReplacementRun)
                {
                    hasReplacementRun = true;
                }
            }
            else
            {
                run = 0;
                if (IsArabicLetter(rune))
                {
                    hasArabicLetter = true;
                }
            }

            if (hasArabicLetter && hasReplacementRun)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsArabicLetter(Rune rune) =>
        rune.Value >= UnicodeRanges.Arabic.FirstCodePoint
        && rune.Value < UnicodeRanges.Arabic.FirstCodePoint + UnicodeRanges.Arabic.Length;
}
