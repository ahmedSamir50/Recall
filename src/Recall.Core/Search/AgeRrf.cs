namespace Recall.Core.Search;

public static class AgeRrf
{
    private sealed class Entry
    {
        public required MediaSearchHit Hit { get; set; }

        public double Score { get; set; }

        public int BestRank { get; set; }

        public int ListIndex { get; set; }
    }

    public static IReadOnlyList<MediaSearchHit> Fuse(IReadOnlyList<IReadOnlyList<MediaSearchHit>> lists)
    {
        ArgumentNullException.ThrowIfNull(lists);
        var scores = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        for (var listIndex = 0; listIndex < lists.Count; listIndex++)
        {
            var list = lists[listIndex] ?? throw new ArgumentNullException(nameof(lists));
            if (list.Count == 0)
            {
                continue;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var rank = 0; rank < list.Count; rank++)
            {
                var hit = list[rank];
                if (!seen.Add(hit.SourcePath))
                {
                    continue;
                }

                var term = 1d / (HybridFusion.RrfRankConstant + rank + 1);
                if (scores.TryGetValue(hit.SourcePath, out var existing))
                {
                    existing.Score += term;
                    if (IsBetterRepresentative(hit, rank, listIndex, existing.Hit, existing.BestRank, existing.ListIndex))
                    {
                        existing.Hit = hit;
                        existing.BestRank = rank;
                        existing.ListIndex = listIndex;
                    }
                }
                else
                {
                    scores[hit.SourcePath] = new Entry
                    {
                        Hit = hit,
                        Score = term,
                        BestRank = rank,
                        ListIndex = listIndex,
                    };
                }
            }
        }

        if (scores.Count == 0)
        {
            return [];
        }

        return scores.Values
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Hit.StartSeconds)
            .ThenBy(entry => entry.Hit.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(entry => entry.Hit with { Score = entry.Score })
            .ToList();
    }

    private static bool IsBetterRepresentative(
        MediaSearchHit candidate,
        int candidateRank,
        int candidateListIndex,
        MediaSearchHit current,
        int currentRank,
        int currentListIndex)
    {
        if (candidateRank < currentRank)
        {
            return true;
        }

        if (candidateRank > currentRank)
        {
            return false;
        }

        if (candidate.StartSeconds < current.StartSeconds)
        {
            return true;
        }

        if (candidate.StartSeconds > current.StartSeconds)
        {
            return false;
        }

        return candidateListIndex < currentListIndex;
    }
}
