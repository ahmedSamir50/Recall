namespace Recall.Core.Scope;

public sealed record ScopeFolderGrant(
    string Path,
    string DisplayName,
    GrantFileKinds EnabledKinds = GrantFileKinds.All,
    string? ReopenToken = null);
