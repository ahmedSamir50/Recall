using Recall.Core.Device;

namespace Recall.Core.Ports;

public interface IDeviceIdCandidateProvider
{
    DeviceIdCandidate GetCandidate();
}
