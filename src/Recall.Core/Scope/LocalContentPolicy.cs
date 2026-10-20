using Recall.Core.Ports;

namespace Recall.Core.Scope;

public static class LocalContentPolicy
{
    public static bool MayReadBytes(LocalContentState state) => state == LocalContentState.Local;
}
