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

    public static IReadOnlyList<TextSearchHit> Fuse(IReadOnlyList<TextSearchHit> dense, IReadOnlyList<TextSearchHit> bm25)
    {
        RequireRankConstant(RrfRankConstant);
        var scores = new Dictionary<string, (TextSearchHit Hit, double Score)>(StringComparer.Ordinal);
        AddRanked(dense, scores);
        AddRanked(bm25, scores);
        return scores.Values
            .OrderByDescending(v => v.Score)
            .Select(v => v.Hit with { Score = v.Score })
            .ToList();
    }

    private static void AddRanked(IReadOnlyList<TextSearchHit> hits, Dictionary<string, (TextSearchHit Hit, double Score)> scores)
    {
        for (var rank = 0; rank < hits.Count; rank++)
        {
            var hit = hits[rank];
            var term = 1.0 / (RrfRankConstant + rank + 1);
            if (scores.TryGetValue(hit.ContentId, out var existing))
            {
                scores[hit.ContentId] = (existing.Hit, existing.Score + term);
            }
            else
            {
                scores[hit.ContentId] = (hit, term);
            }
        }
    }
}
