namespace Recall.Core.Ports;

public interface IEgressProbe
{
    Task<bool> HasNonLocalByteAsync(CancellationToken cancellationToken);
}
