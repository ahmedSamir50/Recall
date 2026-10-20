using Recall.Core.Ports;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiLocalContentProbe : ILocalContentProbe
{
    public LocalContentState Probe(string path) => ProbePlatform(path);
}
