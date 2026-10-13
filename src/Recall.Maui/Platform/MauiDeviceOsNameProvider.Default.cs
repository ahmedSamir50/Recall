#if !WINDOWS && !ANDROID && !IOS && !MACCATALYST
namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceOsNameProvider
{
    private static string GetOsDeviceNamePlatform() => string.Empty;
}
#endif
