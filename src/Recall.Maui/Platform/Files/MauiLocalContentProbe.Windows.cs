#if WINDOWS
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiLocalContentProbe
{
    private static LocalContentState ProbePlatform(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LocalContentState.Unknown;
        }

        try
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return LocalContentState.Unknown;
            }

            var attributes = File.GetAttributes(path);
            var numeric = (int)attributes;
            return WindowsCloudAttributes.FromFindData(true, numeric);
        }
        catch
        {
            return LocalContentState.Unknown;
        }
    }
}
#endif
