#if !WINDOWS
using System.Diagnostics;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiFolderReveal
{
#if MACCATALYST
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
                FileName = "open",
                Arguments = $"-R \"{sourcePath}\"",
                UseShellExecute = false,
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
#else
    private partial bool TryReveal(string sourcePath) => false;
#endif
}
#endif