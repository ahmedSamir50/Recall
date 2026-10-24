using Recall.Core.Ports;

namespace Recall.IntegrationTests.Infrastructure;

internal sealed class TestEncoderFileHash : IEncoderFileHash
{
    public string? TextHash { get; set; } = "integration-test-hash";

    public string? VisionHash { get; set; }

    public string? AudioHash { get; set; }
}
