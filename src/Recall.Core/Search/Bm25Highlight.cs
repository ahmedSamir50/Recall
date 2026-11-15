using Recall.Core.Text;

namespace Recall.Core.Search;

public readonly record struct HighlightMark(int Start, int Length);

public static class Bm25Highlight
{
    public static IReadOnlyList<HighlightMark> Marks(string excerpt, string query)
    {
        ArgumentNullException.ThrowIfNull(excerpt);
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var tokens = KeywordFold.Tokens(KeywordFold.Fold(query));
        if (tokens.Count == 0)
        {
            return [];
        }

        var marks = new List<HighlightMark>();
        foreach (var token in tokens)
        {
            if (string.IsNullOrEmpty(token))
            {
                continue;
            }

            var start = 0;
            while (start <= excerpt.Length - token.Length)
            {
                var index = excerpt.IndexOf(token, start, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    break;
                }

                marks.Add(new HighlightMark(index, token.Length));
                start = index + token.Length;
            }
        }

        return marks;
    }
}