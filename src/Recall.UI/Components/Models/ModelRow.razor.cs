using System.Globalization;
using Microsoft.AspNetCore.Components;
using Recall.Core.Catalog;
using Recall.Core.Download;
using Recall.Core.Ports;
using Recall.Core.Vocabulary;

namespace Recall.UI.Components;

public partial class ModelRow
{
    [Inject]
    private IModelDownloadService Downloads { get; set; } = null!;

    [Parameter, EditorRequired]
    public ModelCatalogEntry Entry { get; set; } = null!;

    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public bool AutoStart { get; set; }

    [Parameter]
    public EventCallback<TextModelDownloadPhase> PhaseChanged { get; set; }

    private TextModelDownloadPhase phase = TextModelDownloadPhase.NotStarted;
    private long bytesReceived;
    private long? currentFileTotal;
    private int completedFiles;
    private int fileCount = 1;
    private TextModelDownloadFault? fault;
    private bool downloadKickoffScheduled;
    private readonly Progress<ModelDownloadProgress> progress;

    private bool TextRow => Entry.Modality == Modality.Text;

    private string HashFailureBorderStyle => phase == TextModelDownloadPhase.HashFailed
        ? "border-color:color-mix(in srgb,var(--err) 50%,transparent)"
        : string.Empty;

    public ModelRow()
    {
        progress = new Progress<ModelDownloadProgress>(_ => SyncAndNotify());
    }

    protected override void OnInitialized() => SyncFromService();

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && AutoStart && TextRow && !downloadKickoffScheduled)
        {
            downloadKickoffScheduled = true;
            _ = StartDownloadIfNeededAsync();
        }

        return Task.CompletedTask;
    }

    private string StatusToneClass => phase switch
    {
        TextModelDownloadPhase.Ready => "em",
        TextModelDownloadPhase.HashFailed => "er",
        TextModelDownloadPhase.Downloading or TextModelDownloadPhase.Verifying => "cy",
        _ => "ink3",
    };

    private string StatusLabel => phase switch
    {
        TextModelDownloadPhase.NotStarted => T("Models_Status_Queued"),
        TextModelDownloadPhase.Downloading => FormatDownloadingLabel(),
        TextModelDownloadPhase.Verifying => T("Models_Status_Verifying"),
        TextModelDownloadPhase.Ready => T("Models_Status_Verified"),
        TextModelDownloadPhase.HashFailed or TextModelDownloadPhase.Interrupted or TextModelDownloadPhase.Cancelled => string.Empty,
        _ => string.Empty,
    };

    private string FaultMessage => fault switch
    {
        TextModelDownloadFault.HashMismatch => T("Fault_DownloadHashMismatch"),
        TextModelDownloadFault.Incomplete => T("Fault_DownloadIncomplete"),
        TextModelDownloadFault.Transport => T("Fault_DownloadTransport"),
        _ => T("Models_DownloadInterrupted"),
    };

    private int OverallPercent =>
        TextModelDownloadUiPolicy.ComputeOverallPercent(completedFiles, fileCount, bytesReceived, currentFileTotal);

    private string ProgressBarStyle
    {
        get
        {
            if (phase == TextModelDownloadPhase.Ready)
            {
                return "width:100%;background:var(--emerald)";
            }

            var percent = OverallPercent;
            if (phase == TextModelDownloadPhase.HashFailed)
            {
                return $"width:{percent}%;background:var(--err)";
            }

            if (phase is TextModelDownloadPhase.Downloading or TextModelDownloadPhase.Verifying)
            {
                return $"width:{percent}%;background:linear-gradient(90deg,var(--cyan-ink),var(--violet))";
            }

            return "width:0%;background:var(--c3)";
        }
    }

    private string FormatDownloadingLabel()
    {
        var digits = OverallPercent.ToString(CultureInfo.InvariantCulture);
        return string.Format(CultureInfo.InvariantCulture, T("Models_Status_Downloading"), digits);
    }

    private void SyncAndNotify()
    {
        SyncFromService();
        _ = InvokeAsync(StateHasChanged);
    }

    private void SyncFromService()
    {
        var snap = Downloads.GetTextDownloadSnapshot();
        phase = snap.Phase;
        bytesReceived = snap.BytesReceived;
        currentFileTotal = snap.CurrentFileTotal;
        completedFiles = snap.CompletedFiles;
        fileCount = snap.FileCount;
        fault = snap.Fault;
        if (TextRow && PhaseChanged.HasDelegate)
        {
            _ = PhaseChanged.InvokeAsync(phase);
        }
    }

    private async Task StartDownloadIfNeededAsync()
    {
        SyncFromService();
        if (!TextModelDownloadUiPolicy.ShouldAutoStartDownload(phase))
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        try
        {
            await Downloads.DownloadTextModelAsync(progress, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // The download service records Cancelled when its own token is cancelled.
        }
        finally
        {
            SyncFromService();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RetryAsync()
    {
        SyncFromService();
        if (!TextModelDownloadUiPolicy.ShouldOfferRetry(phase))
        {
            return;
        }

        try
        {
            await Downloads.RetryTextDownloadAsync(progress, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // The download service records Cancelled when its own token is cancelled.
        }
        finally
        {
            SyncFromService();
            await InvokeAsync(StateHasChanged);
        }
    }
}
