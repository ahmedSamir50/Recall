using Recall.Core.Ports;

namespace Recall.Core.Download;

public sealed class DefaultEncoderFileHash : IEncoderFileHash
{
    public string? TextHash { get; set; }

    public string? VisionHash { get; set; }

    public string? AudioHash { get; set; }
}
