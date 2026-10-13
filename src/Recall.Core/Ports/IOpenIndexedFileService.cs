namespace Recall.Core.Ports;

public interface IOpenIndexedFileService
{
    Task<bool> TryOpenAsync(string sourcePath, CancellationToken cancellationToken = default);
}
