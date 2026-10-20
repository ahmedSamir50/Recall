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

        if (!api36)
        {
            return LocalContentState.Unknown;
        }

        if (downloadProgress || downloadError)
        {
            return LocalContentState.Remote;
        }

        if (availableLocally == true)
        {
            return LocalContentState.Local;
        }

        return LocalContentState.Remote;
    }
}
