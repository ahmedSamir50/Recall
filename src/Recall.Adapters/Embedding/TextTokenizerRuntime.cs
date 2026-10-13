using Recall.Core.Ports;

namespace Recall.Adapters.Embedding;

public static class TextTokenizerRuntime
{
    public static bool IsSupported(RecallShellKind kind) => kind is RecallShellKind.Windows or RecallShellKind.MacCatalyst or RecallShellKind.Unknown;
}
