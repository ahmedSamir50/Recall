using Microsoft.AspNetCore.Components;
using Recall.Core.Search;

namespace Recall.UI.Components.Find;

public partial class ActionMenu
{
    [Parameter, EditorRequired]
    public TextFindCard Card { get; set; } = null!;

    [Parameter]
    public EventCallback<TextFindCard> OnOpen { get; set; }

    [Parameter]
    public EventCallback<TextFindCard> OnReveal { get; set; }

    [Parameter]
    public EventCallback<TextFindCard> OnViewPlay { get; set; }

    [Parameter]
    public EventCallback<TextFindCard> OnPin { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }
}