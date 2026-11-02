namespace Recall.Core.Ingest;

public static class VideoFrameFailureLabels
{
    public const string NeedWindows11 = "Frames_NeedWindows11";

    public const string Unavailable = "Frames_Unavailable";

    public static string? StatusKey(Exception? error, bool emptyFrames)
    {
        if (error is NotSupportedException notSupported
            && string.Equals(notSupported.Message, FrameCadence.WindowsMinimumBuildMessage, StringComparison.Ordinal))
        {
            return NeedWindows11;
        }

        if (emptyFrames)
        {
            return Unavailable;
        }

        return null;
    }
}
