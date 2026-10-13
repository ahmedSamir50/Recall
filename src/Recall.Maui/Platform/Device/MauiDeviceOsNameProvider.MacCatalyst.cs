#if MACCATALYST
using System.Runtime.InteropServices;
using System.Text;

namespace Recall.Maui.Platform;

public sealed partial class MauiDeviceOsNameProvider
{
    private const uint Utf8Encoding = 0x08000100;

    private static string GetOsDeviceNamePlatform()
    {
        var nameRef = SCDynamicStoreCopyComputerName(IntPtr.Zero, IntPtr.Zero);
        if (nameRef == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            var buffer = new byte[1024];
            var copied = CFStringGetCString(nameRef, buffer, buffer.Length, Utf8Encoding);
            if (copied == 0)
            {
                return string.Empty;
            }

            var end = Array.IndexOf(buffer, (byte)0);
            return Encoding.UTF8.GetString(buffer, 0, end < 0 ? buffer.Length : end);
        }
        finally
        {
            CFRelease(nameRef);
        }
    }

    [DllImport("/System/Library/Frameworks/SystemConfiguration.framework/SystemConfiguration")]
    private static extern IntPtr SCDynamicStoreCopyComputerName(IntPtr store, IntPtr encoded);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr value);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern byte CFStringGetCString(IntPtr value, byte[] buffer, nint bufferSize, uint encoding);
}
#endif
