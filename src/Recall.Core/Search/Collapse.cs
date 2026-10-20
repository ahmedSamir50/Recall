namespace Recall.Core.Search;

public static class Collapse
{
    public static TextSearchHit MaxRrf(IReadOnlyList<TextSearchHit> hits)
    {
        if (hits.Count == 0)
        {
            throw new ArgumentException("At least one hit is required.", nameof(hits));
        }

        TextSearchHit best = hits[0];
        for (var i = 1; i < hits.Count; i++)
        {
            var candidate = hits[i];
            if (candidate.Score > best.Score)
            {
                best = candidate;
                continue;
            }

            if (Math.Abs(candidate.Score - best.Score) < 1e-12 && candidate.Ordinal < best.Ordinal)
            {
                best = candidate;
            }
        }

        return best;
    }
}
