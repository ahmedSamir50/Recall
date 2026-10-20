using Recall.Core.Ports;

namespace Recall.Core.Ingest;

public static class IdleIngestGate
{
    public const int DesktopIdleAfterSeconds = 30;

    public static bool ShouldRun(IdleSignal signal) => signal == IdleSignal.Idle;

    public static IdleSignal FromLastInput(TimeSpan idleFor) =>
        idleFor >= TimeSpan.FromSeconds(DesktopIdleAfterSeconds) ? IdleSignal.Idle : IdleSignal.Busy;

    public static IdleSignal ForForeground(bool applicationActive) =>
        applicationActive ? IdleSignal.Idle : IdleSignal.Busy;
}
