using Recall.Core.Ingest;

namespace Recall.UI.Layout;

public static class ShellStatusLabel
{
    public static string? Key(IngestChromeStatus status) => status switch
    {
        IngestChromeStatus.Running => "Ingest_Running",
        IngestChromeStatus.Done => "Status_Ready",
        IngestChromeStatus.Blocked => "Ingest_DeviceTooSmall",
        _ => null,
    };
}
