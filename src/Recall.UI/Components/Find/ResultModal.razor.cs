using Microsoft.AspNetCore.Components;
using Recall.Core.Search;

namespace Recall.UI.Components.Find;

public partial class ResultModal
{
    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public TextFindCard? Card { get; set; }

    [Parameter]
    public byte[]? PickedImage { get; set; }

    [Parameter]
    public string? MediaUrl { get; set; }

    [Parameter]
    public EventCallback OnClosed { get; set; }

    private string? LensDataUri =>
        PickedImage is null ? null : "data:image/jpeg;base64," + Convert.ToBase64String(PickedImage);
}