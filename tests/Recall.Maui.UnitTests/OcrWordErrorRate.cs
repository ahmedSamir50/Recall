namespace Recall.Maui.UnitTests;

internal enum OcrScript
{
    Latin,
    Arabic,
}

internal readonly record struct OcrWordError(double Rate, int Edits, int ReferenceCount);

/// <summary>
/// Standard word error rate: token Levenshtein edits divided by the reference token count.
/// The same normalizer runs on the reference and the hypothesis.
/// </summary>
internal static class OcrWordErrorRate
{
    public static OcrWordError Measure(string reference, string hypothesis) =>
        MeasureTokens(Tokenize(reference), Tokenize(hypothesis));

    public static OcrWordError MeasureScript(string reference, string hypothesis, OcrScript script)
    {
        var referenceTokens = Tokenize(reference).Where(token => BelongsTo(token, script)).ToList();
        var hypothesisTokens = Tokenize(hypothesis).Where(token => BelongsTo(token, script)).ToList();
        return MeasureTokens(referenceTokens, hypothesisTokens);
    }

    private static OcrWordError MeasureTokens(List<string> reference, List<string> hypothesis)
    {
        if (reference.Count == 0)
        {
            return new OcrWordError(hypothesis.Count == 0 ? 0d : 1d, hypothesis.Count, 0);
        }

        var edits = EditDistance(reference, hypothesis);
        return new OcrWordError(edits / (double)reference.Count, edits, reference.Count);
    }

    private static bool BelongsTo(string token, OcrScript script)
    {
        var arabic = false;
        foreach (var ch in token)
        {
            if (ch is >= '\u0600' and <= '\u06FF')
            {
                arabic = true;
                break;
            }
        }

        return script == OcrScript.Arabic ? arabic : !arabic;
    }

    private static List<string> Tokenize(string text)
    {
        var words = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var ch in text)
        {
            var normalized = NormalizeChar(ch);
            if (normalized is null)
            {
                if (current.Length > 0)
                {
                    words.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(normalized.Value);
        }

        if (current.Length > 0)
        {
            words.Add(current.ToString());
        }

        return words;
    }

    private static char? NormalizeChar(char ch)
    {
        if (ch is '\u0640' or >= '\u064B' and <= '\u065F' or '\u0670')
        {
            return null;
        }

        ch = ch switch
        {
            '\u0622' or '\u0623' or '\u0625' or '\u0671' => '\u0627',
            _ => char.ToLowerInvariant(ch),
        };

        if (char.IsLetterOrDigit(ch) || ch is >= '\u0600' and <= '\u06FF')
        {
            return ch;
        }

        return null;
    }

    private static int EditDistance(List<string> left, List<string> right)
    {
        var rows = left.Count + 1;
        var cols = right.Count + 1;
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
