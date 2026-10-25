#if ANDROID
using Recall.Core.Ingest;
using Recall.Maui.Platform;

namespace Recall.Maui.Platform.Bench;

public static class AndroidAudioDecodeSession
{
    public static void RunIfReady(string appFilesDirectory)
    {
        var external = global::Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
        var root = File.Exists(Path.Combine(external ?? string.Empty, "tone.m4a"))
            ? external
            : File.Exists(Path.Combine(appFilesDirectory, "tone.m4a")) ? appFilesDirectory : null;
        if (root is null)
        {
            return;
        }

        var tonePath = Path.Combine(root, "tone.m4a");
        var reportPath = Path.Combine(root, "android-audio-decode.txt");
        try
        {
            var source = new AndroidAudioSource();
            var samples = source.ReadCompressed(File.ReadAllBytes(tonePath));
            File.WriteAllText(reportPath, $"sampleRate={AudioWindows.SampleRate}\nsampleCount={samples.Length}");
        }
        catch
        {
            File.WriteAllText(reportPath, "sampleRate=0\nsampleCount=0");
        }
    }
}
#endif
