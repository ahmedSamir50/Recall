using Microsoft.AspNetCore.Components;

namespace Recall.UI.Pages;

public partial class ShellPage
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    protected override void OnInitialized() => Nav.NavigateTo("/onboarding/welcome", replace: true);
}
