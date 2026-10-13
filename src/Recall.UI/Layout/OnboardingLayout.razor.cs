using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recall.UI.Layout;

public partial class OnboardingLayout
{
    [Inject]
    private IJSRuntime Js { get; set; } = null!;

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
        await Js.InvokeVoidAsync("recallApplyChrome", snap.EffectiveTheme, snap.Lang, snap.Dir);
    }
}
