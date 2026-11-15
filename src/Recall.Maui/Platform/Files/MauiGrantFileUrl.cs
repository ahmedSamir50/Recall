using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Maui.Platform.Files;

public sealed class MauiGrantFileUrl : IGrantFileUrl
{
    public string? Build(string sourcePath, IReadOnlyList<ScopeFolderGrant> grants)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || grants is null || grants.Count == 0)
        {
            return null;
        }

        if (!GrantFileUrlPolicy.PathInGrants(sourcePath, grants))
        {
            return null;
        }

        if (!ContentUri.IsContent(sourcePath) && !File.Exists(sourcePath))
        {
            return null;
        }

        var encoded = Uri.EscapeDataString(sourcePath);
        return $"{GrantFileUrlPolicy.Scheme}://local/{encoded}";
    }

    public bool TryResolveUri(Uri uri, IReadOnlyList<ScopeFolderGrant> grants, out string? sourcePath)
    {
        sourcePath = null;
        if (grants is null || !string.Equals(uri.Scheme, GrantFileUrlPolicy.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var payload = uri.Host.Equals("local", StringComparison.OrdinalIgnoreCase)
            ? uri.AbsolutePath.TrimStart('/')
            : $"{uri.Host}{uri.AbsolutePath}".TrimStart('/');

        if (payload.Length == 0)
        {
            return false;
        }

        string path;
        try
        {
            path = Uri.UnescapeDataString(payload);
        }
        catch
        {
            return false;
        }

        if (!GrantFileUrlPolicy.PathInGrants(path, grants))
        {
            return false;
        }

        if (!ContentUri.IsContent(path) && !File.Exists(path))
        {
            return false;
        }

        sourcePath = path;
        return true;
    }
}