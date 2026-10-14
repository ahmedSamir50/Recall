using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Recall.Core.Device;
using Recall.Core.Ports;
using Recall.UI.State;

namespace Recall.UI.Layout;

public partial class AppShell
{
    [Inject]
    private IDeviceRecordService Device { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    private ShellUiSnapshot Snapshot => Ui.Current;

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

    protected override void OnStoreChanged()
    {
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
}
