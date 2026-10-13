#if MACCATALYST
using System.Runtime.InteropServices;

namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceOsNameProvider
{
    private static string GetOsDeviceNamePlatform()
    {
        var ptr = SCDynamicStoreCopyComputerName(IntPtr.Zero, IntPtr.Zero);
        if (ptr == IntPtr.Zero)
        {
            return string.Empty;
        }

        var name = Marshal.PtrToStringUTF8(ptr);
        Marshal.FreeCoTaskMem(ptr);
        return name ?? string.Empty;
    }

    [DllImport("/System/Library/Frameworks/SystemConfiguration.framework/SystemConfiguration")]
    private static extern IntPtr SCDynamicStoreCopyComputerName(IntPtr store, IntPtr encoded);
}
#endif
