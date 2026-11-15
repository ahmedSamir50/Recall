using Recall.Core.Scope;

namespace Recall.Core.Ports;

public interface IGrantFileUrl
{
    string? Build(string sourcePath, IReadOnlyList<ScopeFolderGrant> grants);

    bool TryResolveUri(Uri uri, IReadOnlyList<ScopeFolderGrant> grants, out string? sourcePath);
}

public interface IFolderReveal
{
    bool Reveal(string sourcePath);
}