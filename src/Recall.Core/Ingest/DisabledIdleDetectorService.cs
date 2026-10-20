using Recall.Core.Ports;

namespace Recall.Core.Ingest;

public sealed class DisabledIdleDetectorService : IIdleDetectorService
{
    public IdleSignal Current => IdleSignal.Idle;
}
