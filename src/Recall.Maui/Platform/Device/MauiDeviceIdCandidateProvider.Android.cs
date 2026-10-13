#if ANDROID
using Android.Provider;
using Recall.Core.Device;

namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceIdCandidateProvider
{
    private static DeviceIdCandidate GetCandidatePlatform()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity is null)
        {
            return new DeviceIdCandidate(null, false);
        }

        var id = Settings.Secure.GetString(activity.ContentResolver, Settings.Secure.AndroidId);
        return new DeviceIdCandidate(id, true);
    }
}
#endif
