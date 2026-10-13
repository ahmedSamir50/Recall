namespace Recall.Core.Scope;

public static class ScopeGrantPolicy
{
    public static bool IsChildPath(string parentPath, string childPath)
    {
        if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(childPath))
        {
            return false;
        }

        var parent = Normalize(parentPath);
        var child = Normalize(childPath);
        if (parent.Equals(child, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!child.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var suffix = child[parent.Length..];
        return suffix.Length == 0 || suffix[0] is '\\' or '/';
    }

    public static string Normalize(string path)
    {
        return path.Trim().Replace('/', '\\').TrimEnd('\\');
    }
}
