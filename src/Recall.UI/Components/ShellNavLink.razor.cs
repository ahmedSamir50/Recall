using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Recall.UI.State;

namespace Recall.UI.Components;

public partial class ShellNavLink
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
        ? "flex items-center gap-3 h-10 px-3 rounded-xl text-14 w-full text-start nav-active cy font-medium"
        : "flex items-center gap-3 h-10 px-3 rounded-xl text-14 w-full text-start ink2";

    private void Go() => Nav.NavigateTo(Href);
}
