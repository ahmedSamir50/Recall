namespace Recall.Core.Scope;

public static class SkipListFilter
{
    public static bool Matches(string row, string? query)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return row.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
