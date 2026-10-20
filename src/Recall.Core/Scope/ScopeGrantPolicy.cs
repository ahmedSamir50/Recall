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

    public static bool IsInsideGrant(string grantPath, string filePath)
    {
        if (string.IsNullOrWhiteSpace(grantPath) || string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var grant = Normalize(grantPath);
        var file = Normalize(filePath);
        if (file.Equals(grant, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsChildPath(grant, file);
    }

    public static string Normalize(string path)
    {
        var trimmed = path.Trim();
        if (trimmed.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.TrimEnd('/');
        }

        return trimmed.Replace('/', '\\').TrimEnd('\\');
    }
}
