#if ANDROID
using Android.OS;
using Android.Provider;

namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceOsNameProvider
{
    private static string GetOsDeviceNamePlatform()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity is not null && (int)Build.VERSION.SdkInt >= 25)
        {
            var deviceName = Settings.Global.GetString(activity.ContentResolver, "device_name");
            if (!string.IsNullOrWhiteSpace(deviceName))
            {
                return deviceName;
            }
        }

        return Build.Model ?? string.Empty;
    }
}
#endif
