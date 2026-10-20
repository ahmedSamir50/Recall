using Recall.Core.Ports;

namespace Recall.Core.Ingest;

public sealed class LocalOnlyContentProbe : ILocalContentProbe
{
    public LocalContentState Probe(string path) => LocalContentState.Local;
}
