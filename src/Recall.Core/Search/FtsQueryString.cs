namespace Recall.Core.Search;

public static class FtsQueryString
{
    public static string ForZVec(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Length == 0)
        {
            return query;
        }

        // ZVec FTS treats ':' as a field prefix (field:value). User text often contains colons (titles, times).
        return query.Replace(':', ' ');
    }
}
