using Microsoft.AspNetCore.Components;

namespace Recall.UI.Pages.Onboarding;

public partial class Folders
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private void HandleBack() => Nav.NavigateTo("/onboarding/welcome");

    private void HandleContinue() => Nav.NavigateTo("/onboarding/models");
}
