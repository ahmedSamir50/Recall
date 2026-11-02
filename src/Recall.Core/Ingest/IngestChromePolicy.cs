using Recall.Core.Download;
using Recall.Core.Runtime;

namespace Recall.Core.Ingest;

public enum IngestChromeStatus
{
    Running = 0,
    Stopped = 1,
    Done = 2,
    Blocked = 3,
}

public static class IngestChromePolicy
{
    public static IngestChromeStatus Status(
        bool passRunning,
        TextModelDownloadPhase text,
        TextModelDownloadPhase vision,
        TextModelDownloadPhase audio,
        bool unfinished,
        DeviceVerdict verdict)
    {
        if (verdict == DeviceVerdict.BelowMinimum)
        {
            return IngestChromeStatus.Blocked;
        }

        if (passRunning
            || text is TextModelDownloadPhase.Downloading or TextModelDownloadPhase.Verifying
            || vision is TextModelDownloadPhase.Downloading or TextModelDownloadPhase.Verifying
            || audio is TextModelDownloadPhase.Downloading or TextModelDownloadPhase.Verifying)
        {
            return IngestChromeStatus.Running;
        }

        if (unfinished)
        {
            return IngestChromeStatus.Stopped;
        }

        return IngestChromeStatus.Done;
    }
}
