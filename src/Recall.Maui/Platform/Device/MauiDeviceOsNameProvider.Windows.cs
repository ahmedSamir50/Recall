#if WINDOWS
namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceOsNameProvider
{
    private static string GetOsDeviceNamePlatform() => Environment.MachineName;
}
#endif
