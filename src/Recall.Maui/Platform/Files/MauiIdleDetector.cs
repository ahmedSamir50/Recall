using Recall.Core.Ingest;
using Recall.Core.Ports;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiIdleDetector : IIdleDetectorService
{
    public IdleSignal Current => ReadCurrent();
}
