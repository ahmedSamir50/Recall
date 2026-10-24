using Recall.Core.Faults;
using Recall.Core.Vocabulary;

namespace Recall.Core.Age;

public sealed class FailClosedAgeCutDecision : IAgeCutDecision
{
    public bool TryPass(Modality modality)
    {
        if (modality == Modality.Text)
        {
            throw new RecallFaultException(RecallFault.AgeCutTextForbidden);
        }

        if (!AgeCutCorpus.TryLoad(modality, out var items))
        {
            return false;
        }

        return AgeCutGate.Passes(modality, items);
    }
}
