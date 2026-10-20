using System.Text;

namespace Recall.Core.Text;

public static class NumericSpanPolicy
{
    public static bool IsDense(string raw, bool codeFile)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (codeFile)
        {
            return true;
        }

        var letters = 0;
        var others = 0;
        foreach (var rune in raw.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                continue;
            }

            if (Rune.IsLetter(rune))
            {
                letters++;
            }
            else
            {
                others++;
            }
        }

        return letters >= 1 && letters * 4 >= others;
    }
}
