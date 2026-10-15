namespace Recall.Core.Scope;

public static class ScopeWatchRoots
{
    public static IReadOnlyList<string> Select(IReadOnlyList<string>? paths)
    {
        if (paths is null || paths.Count == 0)
        {
            return [];
        }

        var normalized = new List<string>();
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var value = ScopeGrantPolicy.Normalize(path);
            if (normalized.Any(existing => existing.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (normalized.Any(other => ScopeGrantPolicy.IsChildPath(other, path)))
            {
                continue;
            }

            normalized.RemoveAll(existing => ScopeGrantPolicy.IsChildPath(value, existing));
            normalized.Add(value);
        }

        return normalized;
    }
}
