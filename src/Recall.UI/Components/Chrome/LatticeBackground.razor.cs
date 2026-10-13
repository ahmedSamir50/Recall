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
            await Js.InvokeVoidAsync(RecallScripts.MountLandingLattice, theme);
        }
        else
        {
            await Js.InvokeVoidAsync(RecallScripts.MountAppLattice, theme);
        }
    }

    protected override void OnStoreChanged()
    {
        _ = InvokeAsync(async () =>
        {
            await Js.InvokeVoidAsync(RecallScripts.SetLatticeTheme, Ui.Current.EffectiveTheme);
            StateHasChanged();
        });
    }
}
