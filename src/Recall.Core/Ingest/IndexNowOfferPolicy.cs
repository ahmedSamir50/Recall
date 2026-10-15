using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public static class IndexNowOfferPolicy
{
    public static IReadOnlyList<ScopeFolderGrant> ListOffers(
        IReadOnlyList<ScopeFolderGrant> grants,
        IReadOnlySet<string> finishedPaths)
    {
        if (grants is null || grants.Count == 0)
        {
            return [];
        }

        var normalized = grants
            .Where(g => g is not null && !string.IsNullOrWhiteSpace(g.Path))
            .Select(g => g with { Path = ScopeGrantPolicy.Normalize(g.Path) })
            .ToList();
        var offers = new List<ScopeFolderGrant>();
        foreach (var grant in normalized)
        {
            var isChild = false;
            foreach (var other in normalized)
            {
                if (ReferenceEquals(grant, other))
                {
                    continue;
                }

                if (ScopeGrantPolicy.IsChildPath(ScopeGrantPolicy.Normalize(other.Path), grant.Path))
                {
                    isChild = true;
                    break;
                }
            }

            if (isChild)
            {
                continue;
            }

            if (finishedPaths is not null && finishedPaths.Contains(grant.Path))
            {
                continue;
            }

            offers.Add(grant);
        }

        return offers;
    }
}
