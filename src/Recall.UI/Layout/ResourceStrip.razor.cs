using System.Globalization;
using Microsoft.AspNetCore.Components;
using Recall.Core.Download;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.UI.Layout;

public partial class ResourceStrip
{
    [Inject]
    private IResourceMeter Meter { get; set; } = null!;

    [Inject]
    private IngestStatusService IngestStatus { get; set; } = null!;

    [Inject]
    private IGrantedTextIngestService Ingest { get; set; } = null!;

    [Inject]
    private IModelDownloadService Downloads { get; set; } = null!;

    [Parameter]
    public bool Compact { get; set; }

    private string PathListClass => Compact
        ? "max-h-[8rem] overflow-y-auto overflow-x-hidden w-full min-w-0"
        : "max-h-[16rem] overflow-y-auto overflow-x-hidden w-full min-w-0";

    private Timer? timer;

    private string ramDisplay = "0 MB";

    private string cpuDisplay = "0%";

    private string? gpuDisplay;

    private IReadOnlyList<IngestKindCount> ingestRows = [];

    private IReadOnlyList<string> unfinishedPaths = [];

    private IngestChromeStatus chromeStatus = IngestChromeStatus.Done;

    protected override void OnInitialized()
    {
        RefreshSnapshot();
        timer = new Timer(static state => ((ResourceStrip)state!).OnTimer(), this, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    private void OnTimer()
    {
#pragma warning disable VSTHRD110
        InvokeAsync(RefreshAsync);
#pragma warning restore VSTHRD110
    }

    private Task RefreshAsync()
    {
        RefreshSnapshot();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void RefreshSnapshot()
    {
        var sample = Meter.Read();
        ramDisplay = ResourceSampleFormat.RamMegabytes(sample.WorkingSetBytes, T("Resources_Mb"));
        cpuDisplay = ResourceSampleFormat.CpuPercent(sample.CpuPercent);
        gpuDisplay = ResourceSampleFormat.GpuValue(sample.GpuPercent);

        var enabled = GrantFileKinds.None;
        foreach (var grant in Ui.Current.Grants)
        {
            enabled |= grant.EnabledKinds;
        }

        ingestRows = IngestStatus.List(enabled);
        unfinishedPaths = IngestStatus.ListUnfinished(Ui.Current.Grants);
        chromeStatus = IngestChromePolicy.Status(
            Ingest.PassRunning,
            Downloads.GetTextDownloadSnapshot().Phase,
            Downloads.GetVisionDownloadSnapshot().Phase,
            Downloads.GetAudioDownloadSnapshot().Phase,
            unfinishedPaths.Count > 0,
            IngestStatus.Verdict);
    }

    private static string CountText(IngestKindCount row) =>
        string.Create(CultureInfo.InvariantCulture, $"{row.Finished}/{row.Seen}");

    private static string PercentText(IngestKindCount row) =>
        string.Create(CultureInfo.InvariantCulture, $"{row.Percent}%");

    private Task RetryIngestAsync()
    {
        _ = Ingest.IngestGrantedTextAsync(CancellationToken.None);
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer?.Dispose();
        }

        base.Dispose(disposing);
    }
}
