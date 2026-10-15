namespace Recall.Core.Scope;

public static class ContentUri
{
    public const string Scheme = "content://";

    public static bool IsContent(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
