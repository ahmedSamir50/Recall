namespace Recall.Maui.UnitTests;

internal static class OcrWordErrorRate
{
    /// <summary>
    /// Fraction of reference words absent from OCR output (whole-word match; Arabic allows bounded edit distance).
    /// </summary>
    public static double MissingReferenceWordRate(string reference, string hypothesis)
    {
        var referenceWords = Tokenize(reference);
        if (referenceWords.Count == 0)
        {
            return 1d;
        }

        var normalizedHypothesis = Normalize(hypothesis);
        var missing = 0;
        foreach (var word in referenceWords)
        {
            if (!ReferenceWordPresent(word, normalizedHypothesis))
            {
                missing++;
            }
        }

        return missing / (double)referenceWords.Count;
    }

    public static IReadOnlyList<string> MissingReferenceWords(string reference, string hypothesis)
    {
        var referenceWords = Tokenize(reference);
        var normalizedHypothesis = Normalize(hypothesis);
        var missing = new List<string>();
        foreach (var word in referenceWords)
        {
            if (!ReferenceWordPresent(word, normalizedHypothesis))
            {
                missing.Add(word);
            }
        }

        return missing;
    }

    private static bool ReferenceWordPresent(string referenceWord, string normalizedHypothesis)
    {
        if (normalizedHypothesis.Contains(referenceWord, StringComparison.Ordinal))
        {
            return true;
        }

        if (!ContainsArabic(referenceWord))
        {
            return false;
        }

        var maxDistance = referenceWord.Length <= 4 ? 1 : 2;
        var minLength = Math.Max(2, referenceWord.Length - maxDistance);
        var maxLength = referenceWord.Length + maxDistance;
        for (var length = minLength; length <= maxLength && length <= normalizedHypothesis.Length; length++)
        {
            for (var start = 0; start + length <= normalizedHypothesis.Length; start++)
            {
                var slice = normalizedHypothesis.Substring(start, length);
                if (EditDistance(referenceWord, slice) <= maxDistance)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsArabic(string text)
    {
        foreach (var ch in text)
        {
            if (ch is >= '\u0600' and <= '\u06FF')
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch) || ch is >= '\u0600' and <= '\u06FF')
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.ToString();
    }

    private static List<string> Tokenize(string text)
    {
        var words = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch) || ch is >= '\u0600' and <= '\u06FF')
            {
                current.Append(char.ToLowerInvariant(ch));
            }
            else if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            words.Add(current.ToString());
        }

        return words;
    }

    private static int EditDistance(string left, string right)
    {
        var rows = left.Length + 1;
        var cols = right.Length + 1;
        var costs = new int[rows * cols];
        for (var i = 0; i < rows; i++)
        {
            costs[i * cols] = i;
        }

        for (var j = 0; j < cols; j++)
        {
            costs[j] = j;
        }

        for (var i = 1; i < rows; i++)
        {
            for (var j = 1; j < cols; j++)
            {
                var substitution = costs[(i - 1) * cols + (j - 1)] + (left[i - 1] == right[j - 1] ? 0 : 1);
                var insertion = costs[i * cols + (j - 1)] + 1;
                var deletion = costs[(i - 1) * cols + j] + 1;
                costs[i * cols + j] = Math.Min(substitution, Math.Min(insertion, deletion));
            }
        }

        return costs[(rows - 1) * cols + (cols - 1)];
    }
}
