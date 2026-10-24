#if ANDROID
using Recall.Core.Ports;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiLocalContentProbe
{
    private static LocalContentState ProbePlatform(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LocalContentState.Unknown;
        }

        if (path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            return LocalContentState.Unknown;
        }

        return LocalContentState.Local;
    }
}
#endif
