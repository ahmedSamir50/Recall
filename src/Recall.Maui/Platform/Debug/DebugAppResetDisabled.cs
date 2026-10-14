using Recall.Core.Ports;

namespace Recall.Maui.Platform.Debug;

public sealed class DebugAppResetDisabled : IDebugAppReset
{
    public bool IsEnabled => false;

    public Task ResetAllLocalDataAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
