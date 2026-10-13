using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Vocabulary;

namespace Recall.Core.Search;

public static class TextVectorIndexGuard
{
    public static void EnsureHybridQueryUsesRrf(int rankConstant, IIndexPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        HybridFusion.RequireRankConstant(rankConstant);
        policy.EnsureStorableWidth(Modality.Text, (int)StoredWidth.W512);
    }
}
