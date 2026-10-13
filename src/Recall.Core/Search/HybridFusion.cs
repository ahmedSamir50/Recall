using Recall.Core.Faults;

namespace Recall.Core.Search;

public static class HybridFusion
{
    public const int RrfRankConstant = 60;

    public static void RequireRankConstant(int rankConstant)
    {
        if (rankConstant != RrfRankConstant)
        {
            throw new RecallFaultException(RecallFault.RrfRankConstant);
        }
    }
}
