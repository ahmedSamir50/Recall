using Recall.Core.Checkpoints;
using Recall.Core.Scope;

namespace Recall.Core.Search;

public static class SearchHitMarks
{
    public static TextSearchHit? Apply(
        TextSearchHit hit,
        IngestChunkRow? chunk,
        IReadOnlyList<ScopeFolderGrant> grants)
    {
        ArgumentNullException.ThrowIfNull(hit);
        if (chunk is null)
        {
            return hit with { SourceMissing = false };
        }

        if (chunk.Retired)
        {
            return null;
        }

        var effectivePath = chunk.Path;
        var inside = false;
        if (grants is not null)
        {
            foreach (var grant in grants)
            {
                if (grant is null || string.IsNullOrWhiteSpace(grant.Path))
                {
                    continue;
                }

                if (ScopeGrantPolicy.IsInsideGrant(grant.Path, effectivePath))
                {
                    inside = true;
                    break;
                }
            }
        }

        if (!inside)
        {
            return null;
        }

        return hit with { SourcePath = effectivePath, SourceMissing = chunk.Missing };
    }
}
