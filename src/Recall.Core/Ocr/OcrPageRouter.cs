using System.Text;
using System.Text.Unicode;

namespace Recall.Core.Ocr;

public readonly record struct OcrLinePair(string English, double EnglishScore, string Arabic, double ArabicScore, bool ArabicScript);

public static class OcrPageRouter
{
    private const double LowConfidence = 0.6;

    private const double TieEpsilon = 1e-6;

    public static string Route(IReadOnlyList<OcrLinePair> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var sampleCount = Math.Min(3, lines.Count);
        var englishMean = 0d;
        var arabicMean = 0d;
        for (var i = 0; i < sampleCount; i++)
        {
            englishMean += lines[i].EnglishScore;
            arabicMean += lines[i].ArabicScore;
        }

        englishMean /= sampleCount;
        arabicMean /= sampleCount;
        var pageReaderIsArabic = arabicMean > englishMean + TieEpsilon;

        var builder = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var english = line.English ?? string.Empty;
            var arabic = line.Arabic ?? string.Empty;
            var pageReaderScore = pageReaderIsArabic ? line.ArabicScore : line.EnglishScore;
            string chosen;
            if (i < sampleCount || pageReaderScore < LowConfidence)
            {
                chosen = OcrLineChoice.Choose(english, line.EnglishScore, arabic, line.ArabicScore);
            }
            else
            {
                chosen = pageReaderIsArabic ? arabic : english;
            }

            if (string.IsNullOrEmpty(chosen))
            {
                continue;
            }

            if (line.ArabicScript)
            {
                builder.Append(chosen);
            }
            else
            {
                if (builder.Length > 0 && !char.IsWhiteSpace(builder[^1]))
                {
                    builder.Append(' ');
                }

                builder.Append(chosen);
            }
        }

        return builder.ToString();
    }

    public static bool ContainsArabicScript(string text) =>
        text.EnumerateRunes().Any(IsArabicLetter);

    private static bool IsArabicLetter(Rune rune) =>
        rune.Value >= UnicodeRanges.Arabic.FirstCodePoint
        && rune.Value < UnicodeRanges.Arabic.FirstCodePoint + UnicodeRanges.Arabic.Length;

}
