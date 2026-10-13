using Microsoft.AspNetCore.Components;

namespace Recall.UI.Components;

public partial class ShellTabLink
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    [Parameter, EditorRequired]
    public string Href { get; set; } = "/";

    [Parameter, EditorRequired]
    public string LabelKey { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Icon { get; set; } = "search";

    private bool Active => string.Equals(Ui.Current.RoutePath, Href, StringComparison.Ordinal);

    private string Css => Active
        ? "flex flex-col items-center gap-1 py-2 text-11 cy"
        : "flex flex-col items-center gap-1 py-2 text-11 ink2";

    private void Go() => Nav.NavigateTo(Href);
}
