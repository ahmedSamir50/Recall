using Recall.Core.Ports;

namespace Recall.Core.Scope;

public enum ScopeWatcherKind
{
    None = 0,
    WindowsFileSystem = 1,
}

public static class ScopeWatcherKindPolicy
{
    public static ScopeWatcherKind Choose(RecallShellKind kind)
    {
        if (kind == RecallShellKind.Windows || kind == RecallShellKind.Android || kind == RecallShellKind.MacCatalyst)
        {
            return ScopeWatcherKind.WindowsFileSystem;
        }

        return ScopeWatcherKind.None;
    }
}
