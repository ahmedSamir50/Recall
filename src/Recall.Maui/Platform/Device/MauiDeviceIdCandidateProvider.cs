using Recall.Core.Device;
using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceIdCandidateProvider : IDeviceIdCandidateProvider
{
    public DeviceIdCandidate GetCandidate() => GetCandidatePlatform();
}
