using System.Globalization;
using Microsoft.AspNetCore.Components;
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;

namespace Recall.UI.Pages;

public partial class Activity
{
    [Inject]
    private IngestStatusService IngestStatus { get; set; } = null!;

    private IReadOnlyList<IngestLogEntry> entries = [];

    protected override void OnInitialized()
    {
        entries = IngestStatus.ActivityLog;
    }

    private static string FormatTime(DateTimeOffset at) =>
        at.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);
}