namespace Recall.Core.Download;

public enum AudioDownloadJoin
{
    UseReady = 0,
    WaitForCurrent = 1,
    StartVision = 2,
}

public static class AudioDownloadJoinPolicy
{
    public static AudioDownloadJoin Decide(TextModelDownloadPhase visionPhase, bool fileReady)
    {
        if (fileReady || visionPhase == TextModelDownloadPhase.Ready)
        {
            return AudioDownloadJoin.UseReady;
        }

        if (visionPhase is TextModelDownloadPhase.Downloading or TextModelDownloadPhase.Verifying)
        {
            return AudioDownloadJoin.WaitForCurrent;
        }

        return AudioDownloadJoin.StartVision;
    }
}
