using Recall.Core.Ports;

namespace Recall.Core.Scope;

public sealed class DisabledScopeWatcher : IFileScopeWatcherService
{
    public int StartCount { get; private set; }

    public bool Watching { get; private set; }

    public void Start(IReadOnlyList<ScopeFolderGrant> grants, IScopeChangeSink sink)
    {
        StartCount++;
        Watching = false;
    }

    public void Stop()
    {
        Watching = false;
    }
}
