using Recall.Core.Ports;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiIdleDetector
{
    private static IdleSignal ReadCurrent() => IdleSignal.Idle;
}
