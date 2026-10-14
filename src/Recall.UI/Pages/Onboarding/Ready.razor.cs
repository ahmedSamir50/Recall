using Microsoft.AspNetCore.Components;
using Recall.Core.Ports;

namespace Recall.UI.Pages.Onboarding;

public partial class Ready
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    [Inject]
    private IOnboardingCompletionStore Onboarding { get; set; } = null!;

    private void GoHome()
    {
        Onboarding.MarkComplete();
        Nav.NavigateTo("/");
    }
}
