using Recall.Core.Ports;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiLocalContentProbe
{
    private static LocalContentState ProbePlatform(string path) => LocalContentState.Local;
}
