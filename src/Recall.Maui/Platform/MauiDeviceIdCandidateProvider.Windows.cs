#if WINDOWS
using Microsoft.Win32;
using Recall.Core.Device;

namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceIdCandidateProvider
{
    private static DeviceIdCandidate GetCandidatePlatform()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography",
                "MachineGuid",
                null) as string;
            return new DeviceIdCandidate(value, true);
        }
        catch
        {
            return new DeviceIdCandidate(null, true);
        }
    }
}
#endif
