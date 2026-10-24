using Recall.Core.Vocabulary;

namespace Recall.Core.Age;

public interface IAgeCutDecision
{
    bool TryPass(Modality modality);
}
