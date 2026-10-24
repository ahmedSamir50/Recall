using System.Globalization;

namespace Recall.Core.Ingest;

public readonly record struct AudioWindow(int StartMilliseconds, int EndMilliseconds);

public static class AudioWindows
{
    public const int NativeLengthMilliseconds = 11200;

    public const int SampleRate = 16000;

    public static int HopMilliseconds(int nativeLengthMilliseconds)
    {
        if (nativeLengthMilliseconds < 2)
        {
            return 1;
        }

        return nativeLengthMilliseconds / 2;
    }

    public static IReadOnlyList<AudioWindow> Plan(int durationMilliseconds)
    {
        if (durationMilliseconds <= 0)
        {
            return [];
        }

        if (durationMilliseconds <= NativeLengthMilliseconds)
        {
            return [new AudioWindow(0, durationMilliseconds)];
        }

        var windows = new List<AudioWindow>();
        var hop = HopMilliseconds(NativeLengthMilliseconds);
        var start = 0;
        while (start < durationMilliseconds)
        {
            if (durationMilliseconds - start >= NativeLengthMilliseconds)
            {
                windows.Add(new AudioWindow(start, start + NativeLengthMilliseconds));
                start += hop;
                continue;
            }

            if (windows.Count > 0)
            {
                var last = windows[^1];
                windows[^1] = new AudioWindow(last.StartMilliseconds, durationMilliseconds);
            }
            else
            {
                windows.Add(new AudioWindow(0, durationMilliseconds));
            }

            break;
        }

        return windows;
    }

    public static string Seconds(int milliseconds) =>
        (milliseconds / 1000d).ToString("0.######", CultureInfo.InvariantCulture);
}
