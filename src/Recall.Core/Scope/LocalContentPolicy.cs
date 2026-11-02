using Recall.Core.Ports;

namespace Recall.Core.Scope;

public static class LocalContentPolicy
{
    public const long MaxTextFileBytes = 33_554_432;

    public static bool MayReadBytes(LocalContentState state) => state == LocalContentState.Local;
}
