namespace Recall.Core.Ports;

public interface IDebugAppReset
{
    bool IsEnabled { get; }

    Task ResetAllLocalDataAsync(CancellationToken cancellationToken = default);
}
