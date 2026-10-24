using Recall.Core.Ports;

namespace Recall.Core.Scope;

public static class AndroidDocumentPresence
{
    public static LocalContentState FromFlags(
        bool partial,
        bool virtualDocument,
        bool api36,
        bool? availableLocally,
        bool downloadProgress,
        bool downloadError)
    {
        if (partial || virtualDocument)
        {
            return LocalContentState.Remote;
        }

        if (downloadProgress || downloadError)
        {
            return LocalContentState.Remote;
        }

        if (availableLocally == false)
        {
            return LocalContentState.Remote;
        }

        GC.KeepAlive(api36);
        return LocalContentState.Local;
    }
}
