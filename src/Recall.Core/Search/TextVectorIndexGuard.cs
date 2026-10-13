using Recall.Core.Ports;
using Recall.Core.Vocabulary;

namespace Recall.Core.Search;

public static class TextVectorIndexGuard
{
    public static void EnsureHybridQueryUsesRrf(ITextVectorIndex index, IIndexPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(policy);
        policy.EnsureStorableWidth(Modality.Text, (int)StoredWidth.W512);
    }
}
