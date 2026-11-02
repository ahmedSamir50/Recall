using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Recall.Core.Device;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.UI.State;

namespace Recall.UI.Layout;

public partial class AppShell
{
    [Inject]
    private IDeviceRecordService Device { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private IGrantedTextIngestService Ingest { get; set; } = null!;

    [Inject]
    private IModelDownloadService Downloads { get; set; } = null!;

    [Inject]
    private IngestStatusService IngestStatus { get; set; } = null!;

    private Timer? statusTimer;

    private IngestChromeStatus chromeStatus = IngestChromeStatus.Done;

    private ShellUiSnapshot Snapshot => Ui.Current;

    private string? ShellStatusKey => ShellStatusLabel.Key(chromeStatus);

    private string Initials
    {
        get
        {
            try
            {
                return Device.GetInitials();
            }
            catch (DeviceIdNotReadyException)
            {
                return string.Empty;
            }
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RefreshChromeStatus();
        statusTimer = new Timer(static state => ((AppShell)state!).OnStatusTimer(), this, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    protected override void OnStoreChanged()
    {
        RefreshChromeStatus();
        _ = InvokeAsync(async () =>
        {
            await ApplyChromeAsync();
            StateHasChanged();
        });
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        Ui.ApplyCulture();
        await ApplyChromeAsync();
    }

    private async Task ApplyChromeAsync()
    {
        var snap = Ui.Current;
        await Js.InvokeVoidAsync(RecallScripts.ApplyChrome, snap.EffectiveTheme, snap.Lang, snap.Dir);
    }

    private void OnStatusTimer()
    {
#pragma warning disable VSTHRD110
        InvokeAsync(RefreshStatusAsync);
#pragma warning restore VSTHRD110
    }

    private Task RefreshStatusAsync()
    {
        RefreshChromeStatus();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void RefreshChromeStatus()
    {
        chromeStatus = IngestChromePolicy.Status(
            Ingest.PassRunning,
            Downloads.GetTextDownloadSnapshot().Phase,
            Downloads.GetVisionDownloadSnapshot().Phase,
            Downloads.GetAudioDownloadSnapshot().Phase,
            IngestStatus.ListUnfinished(Ui.Current.Grants).Count > 0,
            IngestStatus.Verdict);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            statusTimer?.Dispose();
        }

        base.Dispose(disposing);
    }
}
