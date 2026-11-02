using System.Globalization;
using System.Text;

namespace Recall.Core.Ocr;

public readonly record struct OcrCharacterError(double Rate, int Edits, int ReferenceLength);

public static class OcrCharacterErrorRate
{
    public static OcrCharacterError MeasureGolden(string reference, string hypothesis) =>
        Measure(NormalizeGolden(reference), NormalizeGolden(hypothesis));

    public static OcrCharacterError Measure(string reference, string hypothesis)
    {
        ArgumentNullException.ThrowIfNull(reference);
        hypothesis ??= string.Empty;
        if (reference.Length == 0)
        {
            return new OcrCharacterError(hypothesis.Length == 0 ? 0d : 1d, hypothesis.Length, 0);
        }

        var edits = Levenshtein(reference, hypothesis);
        return new OcrCharacterError(edits / (double)reference.Length, edits, reference.Length);
    }

    public static string NormalizeGolden(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        var pendingSpace = false;
        foreach (var ch in normalized)
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            if (ch is >= '\u0660' and <= '\u0669')
            {
                builder.Append((char)('0' + (ch - '\u0660')));
                continue;
            }

            if (ch is >= '\u06F0' and <= '\u06F9')
            {
                builder.Append((char)('0' + (ch - '\u06F0')));
                continue;
            }

            if (ch is '\u066B')
            {
                builder.Append('.');
                continue;
            }

            if (ch is '\u066C' or '\u060C' or '\u061B' or '-' or ':' or '—' or '–' or '(' or ')' or '[' or ']')
            {
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    public static int CeilingPercent(OcrCharacterError error) =>
        error.ReferenceLength == 0 ? 0 : (int)Math.Ceiling(error.Rate * 100d);

    private static int Levenshtein(string left, string right)
    {
        var costs = new int[left.Length + 1];
        for (var i = 0; i <= left.Length; i++)
        {
            costs[i] = i;
        }

        for (var j = 1; j <= right.Length; j++)
        {
            costs[0] = j;
            var corner = j - 1;
            for (var i = 1; i <= left.Length; i++)
            {
                var upper = costs[i];
                var next = left[i - 1] == right[j - 1] ? corner : corner + 1;
                corner = upper;
                costs[i] = Math.Min(Math.Min(costs[i] + 1, costs[i - 1] + 1), next);
            }
        }

        return costs[left.Length];
    }
}
