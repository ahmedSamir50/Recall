using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recall.UI.Layout;

public partial class LandingLayout
{
    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        Ui.ApplyCulture();
        var snap = Ui.Current;
        await Js.InvokeVoidAsync("recallApplyChrome", snap.EffectiveTheme, snap.Lang, snap.Dir);
    }

    protected override void OnStoreChanged()
    {
        _ = InvokeAsync(async () =>
        {
            var snap = Ui.Current;
            await Js.InvokeVoidAsync("recallApplyChrome", snap.EffectiveTheme, snap.Lang, snap.Dir);
            StateHasChanged();
        });
    }
}
