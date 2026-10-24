using Recall.Core.Faults;
using Recall.Core.Vocabulary;

namespace Recall.Core.Age;

public static class AgeCutCorpus
{
    public static bool TryLoad(Modality modality, out IReadOnlyList<AgeCutItem> items)
    {
        if (modality == Modality.Text)
        {
            throw new RecallFaultException(RecallFault.AgeCutTextForbidden);
        }

        items = Array.Empty<AgeCutItem>();
        return false;
    }
}
