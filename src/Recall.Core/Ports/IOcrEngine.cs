namespace Recall.Core.Ports;

public interface IOcrEngine : IDisposable
{
    Task RunPassAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken);
}
