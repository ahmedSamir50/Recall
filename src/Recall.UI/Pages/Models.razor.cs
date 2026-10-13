using Recall.Core.Vocabulary;

namespace Recall.UI.Pages;

public partial class Models
{
    private string ModelLabel(Modality modality) => modality switch
    {
        Modality.Text => T("Models_Entry_Text"),
        Modality.Image => T("Models_Entry_Vision"),
        Modality.Audio => T("Models_Entry_Audio"),
        _ => modality.ToString(),
    };
}
