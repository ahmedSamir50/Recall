using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recall.UI.Layout;

public partial class MainLayout
{
    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    private string Lang => Ui.Current.Lang;

    private string Dir => Ui.Current.Dir;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await Js.InvokeVoidAsync("recallSetDocumentCulture", Lang, Dir);
    }
}
