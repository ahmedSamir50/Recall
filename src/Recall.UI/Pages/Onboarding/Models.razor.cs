using Microsoft.AspNetCore.Components;
using Recall.Core.Download;
using Recall.Core.Ports;
using Recall.Core.Vocabulary;

namespace Recall.UI.Pages.Onboarding;

public partial class Models
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    [Inject]
    private IModelDownloadService Downloads { get; set; } = null!;

    private TextModelDownloadPhase textPhase = TextModelDownloadPhase.NotStarted;

    private bool CanContinue => textPhase == TextModelDownloadPhase.Ready;

    protected override void OnInitialized()
    {
        textPhase = Downloads.GetTextDownloadSnapshot().Phase;
    }

    private void OnTextPhase(TextModelDownloadPhase phase)
    {
        textPhase = phase;
    }

    private void GoBack() => Nav.NavigateTo(AppRoutes.OnboardingFolders);

    private void GoNext()
    {
        if (!CanContinue)
        {
            return;
        }

        Nav.NavigateTo(AppRoutes.OnboardingReady);
    }

    private string ModelLabel(Modality modality) => modality switch
    {
        Modality.Text => T("Models_Entry_Text"),
        Modality.Image => T("Models_Entry_Vision"),
        Modality.Audio => T("Models_Entry_Audio"),
        _ => modality.ToString(),
    };
}
