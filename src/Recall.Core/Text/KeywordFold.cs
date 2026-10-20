using System.Text;

namespace Recall.Core.Text;

public static class KeywordFold
{
    public static string Fold(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var normalized = raw.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var rune in normalized.EnumerateRunes())
        {
            if (rune.Value is 0x0640 or (>= 0x064B and <= 0x065F) or 0x0670)
            {
                continue;
            }

            if (rune.Value is 0x0622 or 0x0623 or 0x0625 or 0x0671)
            {
                builder.Append('\u0627');
                continue;
            }

            builder.Append(rune.ToString());
        }

        return builder.ToString();
    }

    public static IReadOnlyList<string> Tokens(string folded)
    {
        ArgumentNullException.ThrowIfNull(folded);
        var tokens = new List<string>();
        var buffer = new StringBuilder();
        foreach (var rune in folded.EnumerateRunes())
        {
            if (IsHan(rune))
            {
                Flush(buffer, tokens);
                tokens.Add(rune.ToString());
                continue;
            }

            if (Rune.IsLetter(rune))
            {
                buffer.Append(rune.ToString());
                continue;
            }

            Flush(buffer, tokens);
        }

        Flush(buffer, tokens);
        return tokens;
    }

    private static bool IsHan(Rune rune) => rune.Value is >= 0x4E00 and <= 0x9FFF;

    private static void Flush(StringBuilder buffer, List<string> tokens)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        tokens.Add(buffer.ToString().ToLowerInvariant());
        buffer.Clear();
    }
}
