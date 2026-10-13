using Microsoft.AspNetCore.Components;
using Recall.Core.Vocabulary;
using Recall.UI;

namespace Recall.UI.Pages.Onboarding;

public partial class Models
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private void GoBack() => Nav.NavigateTo(AppRoutes.OnboardingFolders);

    private string ModelLabel(Modality modality) => modality switch
    {
        Modality.Text => T("Models_Entry_Text"),
        Modality.Image => T("Models_Entry_Vision"),
        Modality.Audio => T("Models_Entry_Audio"),
        _ => modality.ToString(),
    };
}
