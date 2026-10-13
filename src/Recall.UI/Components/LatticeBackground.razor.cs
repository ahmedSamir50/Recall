using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recall.UI.Components;

public partial class LatticeBackground
{
    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Parameter]
    public bool UseClassicShader { get; set; }

    private bool mounted;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || mounted)
        {
            return;
        }

        mounted = true;
        var theme = Ui.Current.EffectiveTheme;
        if (UseClassicShader)
        {
            await Js.InvokeVoidAsync("recallMountLandingLattice", theme);
        }
        else
        {
            await Js.InvokeVoidAsync("recallMountAppLattice", theme);
        }
    }

    protected override void OnStoreChanged()
    {
        _ = InvokeAsync(async () =>
        {
            await Js.InvokeVoidAsync("recallSetLatticeTheme", Ui.Current.EffectiveTheme);
            StateHasChanged();
        });
    }
}
