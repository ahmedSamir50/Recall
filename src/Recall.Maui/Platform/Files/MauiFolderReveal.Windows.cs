#if WINDOWS
using System.Diagnostics;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiFolderReveal
{
    private partial bool TryReveal(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
#pragma warning disable S4036
                FileName = "explorer.exe",
#pragma warning restore S4036
                Arguments = $"/select,\"{sourcePath}\"",
                UseShellExecute = true,
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
#endif