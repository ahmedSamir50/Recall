using Recall.Core.Ports;

namespace Recall.Core.Ingest;

public sealed class AudioOffReport : IAudioOffReport
{
    public bool Shown { get; private set; }

    public void Report() => Shown = true;

    public void Clear() => Shown = false;
}
