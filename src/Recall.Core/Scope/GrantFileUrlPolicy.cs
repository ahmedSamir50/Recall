namespace Recall.Core.Scope;

public static class GrantFileUrlPolicy
{
    public const string Scheme = "recall-file";

    public static bool PathInGrants(string filePath, IReadOnlyList<ScopeFolderGrant> grants)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(grants);
        if (filePath.StartsWith(ContentUri.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return grants.Count > 0;
        }

        string full;
        try
        {
            full = Path.GetFullPath(filePath);
        }
        catch
        {
            return false;
        }

        for (var index = 0; index < grants.Count; index++)
        {
            if (IsUnderGrant(full, grants[index].Path))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUnderGrant(string fullPath, string grantPath)
    {
        if (string.IsNullOrWhiteSpace(grantPath))
        {
            return false;
        }

        string root;
        try
        {
            root = Path.GetFullPath(grantPath);
        }
        catch
        {
            return false;
        }

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(prefix, comparison) || string.Equals(fullPath, root, comparison);
    }
}