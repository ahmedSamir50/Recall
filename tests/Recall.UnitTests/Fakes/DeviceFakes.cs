using Recall.Core.Device;
using Recall.Core.Ports;

namespace Recall.UnitTests.Fakes;

public sealed class FakeDeviceIdCandidateProvider : IDeviceIdCandidateProvider
{
    public DeviceIdCandidate Candidate { get; set; } = new("test-device-id", true);

    public DeviceIdCandidate GetCandidate() => Candidate;
}
