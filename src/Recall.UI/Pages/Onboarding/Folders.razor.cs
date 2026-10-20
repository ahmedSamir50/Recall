using Microsoft.AspNetCore.Components;

namespace Recall.UI.Pages.Onboarding;

public partial class Folders
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private void HandleBack() => Nav.NavigateTo(AppRoutes.OnboardingWelcome);

    private void HandleContinue() => Nav.NavigateTo(AppRoutes.OnboardingModels);
}
