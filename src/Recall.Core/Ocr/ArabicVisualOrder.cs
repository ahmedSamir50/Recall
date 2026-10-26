using System.Text;

namespace Recall.Core.Ocr;

public static class ArabicVisualOrder
{
    public static string ToLogical(string visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        if (visual.Length == 0)
        {
            return string.Empty;
        }

        var groups = new List<string>();
        var run = new StringBuilder();
        foreach (char ch in visual)
        {
            if (ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or ' ' or ':' or '*' or '.' or '/' or '%' or '+' or '-')
            {
                run.Append(ch);
                continue;
            }

            if (run.Length > 0)
            {
                groups.Add(run.ToString());
                run.Clear();
            }

            groups.Add(ch.ToString());
        }

        if (run.Length > 0)
        {
            groups.Add(run.ToString());
        }

        groups.Reverse();
        return string.Concat(groups);
    }
}
