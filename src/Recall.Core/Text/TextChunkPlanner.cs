namespace Recall.Core.Text;

public static class TextChunkPlanner
{
    public const int MaxTokenBudget = 8192;

    public static IReadOnlyList<TextChunkPiece> PlanChunks(
        string title,
        string content,
        Func<string, int> countTokens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(countTokens);

        var whole = EmbeddingTaskPrefixes.WrapTitledDocument(title, content);
        if (countTokens(whole) <= MaxTokenBudget)
        {
            return [new TextChunkPiece(whole, content, 0)];
        }

        var pieces = new List<TextChunkPiece>();
        var start = 0;
        var ordinal = 0;
        while (start < content.Length)
        {
            var end = FindChunkEnd(title, content, start, countTokens);
            end = SnapToRuneBoundary(content, end);
            if (end <= start)
            {
                end = NextRuneEnd(content, start);
            }

            var raw = content[start..end];
            pieces.Add(new TextChunkPiece(EmbeddingTaskPrefixes.WrapTitledDocument(title, raw), raw, ordinal));
            ordinal++;
            start = end;
        }

        return pieces;
    }

    private static int FindChunkEnd(string title, string content, int start, Func<string, int> countTokens)
    {
        var low = start + 1;
        var high = content.Length;
        var best = start;
        while (low <= high)
        {
            var mid = low + ((high - low) / 2);
            var wrapped = EmbeddingTaskPrefixes.WrapTitledDocument(title, content[start..mid]);
            if (countTokens(wrapped) <= MaxTokenBudget)
            {
                best = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return best;
    }

    private static int SnapToRuneBoundary(string text, int index)
    {
        if (index > 0 && index < text.Length && char.IsLowSurrogate(text[index]))
        {
            return index - 1;
        }

        return index;
    }

    private static int NextRuneEnd(string text, int start)
    {
        if (start >= text.Length)
        {
            return text.Length;
        }

        var end = start + 1;
        if (end < text.Length && char.IsHighSurrogate(text[start]) && char.IsLowSurrogate(text[end]))
        {
            return end + 1;
        }

        return Math.Min(end, text.Length);
    }
}

public readonly record struct TextChunkPiece(string PrefixedText, string RawText, int Ordinal);
