using Microsoft.AspNetCore.Components;
using Recall.UI;

namespace Recall.UI.Pages.Onboarding;

public partial class Welcome
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private void GoToFolders() => Nav.NavigateTo(AppRoutes.OnboardingFolders);
}
