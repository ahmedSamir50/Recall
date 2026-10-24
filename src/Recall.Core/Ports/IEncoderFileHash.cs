namespace Recall.Core.Ports;

public interface IEncoderFileHash
{
    string? TextHash { get; }

    string? VisionHash { get; }

    string? AudioHash { get; }
}
