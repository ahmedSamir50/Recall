using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed class MauiRecallShellKind : IRecallShellKind
{
    public RecallShellKind Kind =>
#if WINDOWS
        RecallShellKind.Windows;
#elif ANDROID
        RecallShellKind.Android;
#elif IOS
        RecallShellKind.Ios;
#elif MACCATALYST
        RecallShellKind.MacCatalyst;
#else
        RecallShellKind.Unknown;
#endif
}
