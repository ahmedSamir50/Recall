namespace Recall.Core.Scope;

public static class GrantMembership
{
    public static string? GrantPathFor(
        string documentPath,
        IReadOnlyList<ScopeFolderGrant> grants,
        IReadOnlySet<string> enumeratedDocumentPaths)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(grants);
        ArgumentNullException.ThrowIfNull(enumeratedDocumentPaths);
        if (documentPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            return grants
                .Select(grant => grant.Path)
                .FirstOrDefault(tree => MatchesContentTree(documentPath, tree) && enumeratedDocumentPaths.Contains(documentPath));
        }

        return grants.FirstOrDefault(grant => ScopeGrantPolicy.IsInsideGrant(grant.Path, documentPath))?.Path;
    }

    private static bool MatchesContentTree(string documentPath, string grantPath)
    {
        var tree = ScopeGrantPolicy.Normalize(grantPath);
        if (!documentPath.StartsWith(tree, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (documentPath.Length <= tree.Length)
        {
            return true;
        }

        return documentPath[tree.Length] == '/';
    }
}
