using Microsoft.AspNetCore.Components;
using Recall.Core.Ports;

namespace Recall.UI.Layout;

public partial class ResourceStrip
{
    [Inject]
    private IResourceMeter Meter { get; set; } = null!;

    private Timer? timer;

    private string ramMb = "0";

    private string cpuPercent = "0";

    private string? gpuPercent;

    protected override void OnInitialized()
    {
        ApplySample(Meter.Read());
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
        ApplySample(Meter.Read());
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void ApplySample(ResourceSample sample)
    {
        ramMb = (sample.WorkingSetBytes / 1048576).ToString(System.Globalization.CultureInfo.InvariantCulture);
        cpuPercent = ((int)sample.CpuPercent).ToString(System.Globalization.CultureInfo.InvariantCulture);
        gpuPercent = sample.GpuPercent is null ? null : ((int)sample.GpuPercent.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
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
