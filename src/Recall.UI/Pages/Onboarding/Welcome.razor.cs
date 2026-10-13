using Microsoft.AspNetCore.Components;

namespace Recall.UI.Pages.Onboarding;

public partial class Welcome
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private void GoToFolders() => Nav.NavigateTo("/onboarding/folders");
}
