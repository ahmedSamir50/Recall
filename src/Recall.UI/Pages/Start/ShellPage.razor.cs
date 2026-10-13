using Microsoft.AspNetCore.Components;
using Recall.UI;

namespace Recall.UI.Pages;

public partial class ShellPage
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    protected override void OnInitialized() => Nav.NavigateTo(AppRoutes.OnboardingWelcome, replace: true);
}
