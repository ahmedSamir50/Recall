using Recall.Core.Ports;

namespace Recall.Core.Scope;

public static class WindowsCloudAttributes
{
    public const int Offline = 0x1000;
    public const int RecallOnOpen = 0x00040000;
    public const int RecallOnDataAccess = 0x00400000;
    public const int Pinned = 0x00080000;
    public const int Unpinned = 0x00100000;

    public static LocalContentState FromFindData(bool found, int attributes)
    {
        if (!found)
        {
            return LocalContentState.Unknown;
        }

        if ((attributes & Offline) != 0 || (attributes & RecallOnOpen) != 0 || (attributes & RecallOnDataAccess) != 0)
        {
            return LocalContentState.Remote;
        }

        return LocalContentState.Local;
    }
}
