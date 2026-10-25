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
        foreach (var line in lines)
        {
            var english = line.English ?? string.Empty;
            var arabic = line.Arabic ?? string.Empty;
            string chosen;
            if (pageReaderIsArabic)
            {
                chosen = arabic;
                if (line.ArabicScore < LowConfidence && englishScoreBeatsArabic(line.EnglishScore, line.ArabicScore))
                {
                    chosen = english;
                }
            }
            else
            {
                chosen = english;
                if (line.EnglishScore < LowConfidence && line.ArabicScore > line.EnglishScore + TieEpsilon)
                {
                    chosen = arabic;
                }
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

    private static bool englishScoreBeatsArabic(double englishScore, double arabicScore) =>
        englishScore > arabicScore + TieEpsilon
        || Math.Abs(englishScore - arabicScore) <= TieEpsilon;

    public static bool ContainsArabicScript(string text) =>
        text.EnumerateRunes().Any(IsArabicLetter);

    private static bool IsArabicLetter(Rune rune) =>
        rune.Value >= UnicodeRanges.Arabic.FirstCodePoint
        && rune.Value < UnicodeRanges.Arabic.FirstCodePoint + UnicodeRanges.Arabic.Length;
}
