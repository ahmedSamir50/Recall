#if !WINDOWS
using Recall.Core.Ports;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiLocalContentProbe
{
    private static LocalContentState ProbePlatform(string path) =>
        string.IsNullOrWhiteSpace(path) ? LocalContentState.Unknown : LocalContentState.Local;
}
#endif
