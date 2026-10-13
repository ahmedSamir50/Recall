#if IOS || MACCATALYST
using Recall.Core.Device;
using UIKit;

namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceIdCandidateProvider
{
    private static DeviceIdCandidate GetCandidatePlatform()
    {
        var vendor = UIDevice.CurrentDevice.IdentifierForVendor;
        if (vendor is null)
        {
            return new DeviceIdCandidate(null, false);
        }

        return new DeviceIdCandidate(vendor.AsString(), true);
    }
}
#endif
