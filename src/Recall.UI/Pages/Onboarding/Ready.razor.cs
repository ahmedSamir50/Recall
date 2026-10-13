using Microsoft.AspNetCore.Components;

namespace Recall.UI.Pages.Onboarding;

public partial class Ready
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private void GoHome() => Nav.NavigateTo("/");
}
