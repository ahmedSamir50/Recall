namespace Recall.Core.Scope;

public enum WindowsWatchKind
{
    Created = 0,
    Changed = 1,
    Renamed = 2,
    Deleted = 3,
    Error = 4,
}

public static class WindowsWatchEventMapper
{
    public static bool TryMap(WindowsWatchKind kind, string path, string? previousPath, out ScopeChange change)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            change = new ScopeChange(ScopeChangeKind.Created, string.Empty, null);
            return false;
        }

        if (kind == WindowsWatchKind.Error)
        {
            change = new ScopeChange(ScopeChangeKind.Created, string.Empty, null);
            return false;
        }

        if (kind == WindowsWatchKind.Renamed)
        {
            if (!string.IsNullOrWhiteSpace(previousPath))
            {
                change = new ScopeChange(ScopeChangeKind.Renamed, path, previousPath);
                return true;
            }

            change = new ScopeChange(ScopeChangeKind.Created, path, null);
            return true;
        }

        if (kind == WindowsWatchKind.Deleted)
        {
            change = new ScopeChange(ScopeChangeKind.Deleted, path, null);
            return true;
        }

        if (kind == WindowsWatchKind.Changed)
        {
            change = new ScopeChange(ScopeChangeKind.Modified, path, null);
            return true;
        }

        if (kind == WindowsWatchKind.Created)
        {
            change = new ScopeChange(ScopeChangeKind.Created, path, null);
            return true;
        }

        change = new ScopeChange(ScopeChangeKind.Created, string.Empty, null);
        return false;
    }
}
