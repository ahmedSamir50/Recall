using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceOsNameProvider : IDeviceOsNameProvider
{
    public string GetOsDeviceName() => GetOsDeviceNamePlatform();
}
