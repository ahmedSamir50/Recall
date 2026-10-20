using Microsoft.AspNetCore.Components;
using Recall.Core.Ports;

namespace Recall.UI.Components;

public sealed partial class BootstrapRedirect
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    [Inject]
    private IOnboardingCompletionStore Onboarding { get; set; } = null!;

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender || Onboarding.IsComplete())
        {
            return;
        }

        var path = new Uri(Nav.Uri).AbsolutePath;
        if (path.StartsWith("/onboarding", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/landing", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Nav.NavigateTo(AppRoutes.OnboardingWelcome, replace: true);
    }
}
