#if IOS
using UIKit;

namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceOsNameProvider
{
    private static string GetOsDeviceNamePlatform() => UIDevice.CurrentDevice.Name ?? string.Empty;
}
#endif
