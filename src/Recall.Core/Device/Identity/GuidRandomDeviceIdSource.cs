using Recall.Core.Ports;

namespace Recall.Core.Device;

public sealed class GuidRandomDeviceIdSource : IRandomDeviceIdSource
{
    public string NewDeviceId() => Guid.NewGuid().ToString("N");
}
