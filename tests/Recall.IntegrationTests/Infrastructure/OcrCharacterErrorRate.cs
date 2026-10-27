namespace Recall.IntegrationTests.Infrastructure;

internal static class OcrCharacterErrorRate
{
    public static double Measure(string reference, string hypothesis)
    {
        reference = Normalize(reference);
        hypothesis = Normalize(hypothesis);
        if (reference.Length == 0)
        {
            return hypothesis.Length == 0 ? 0d : 1d;
        }

        var edits = Levenshtein(reference, hypothesis);
        return edits / (double)reference.Length;
    }

    private static string Normalize(string value) =>
        string.Concat(value.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();

    private static int Levenshtein(string left, string right)
    {
        var costs = new int[right.Length + 1];
        for (var j = 0; j <= right.Length; j++)
        {
            costs[j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            var previous = costs[0];
            costs[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var saved = costs[j];
                var substitution = previous + (left[i - 1] == right[j - 1] ? 0 : 1);
                var insertion = costs[j] + 1;
                var deletion = costs[j - 1] + 1;
                costs[j] = Math.Min(substitution, Math.Min(insertion, deletion));
                previous = saved;
            }
        }

        return costs[right.Length];
    }
}
