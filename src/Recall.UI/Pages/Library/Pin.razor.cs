using Microsoft.AspNetCore.Components;

namespace Recall.UI.Pages;

public partial class Pin
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private void GoPreferences() => Nav.NavigateTo("/preferences");
}
