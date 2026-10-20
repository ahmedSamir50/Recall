using Recall.Core.Checkpoints;

namespace Recall.Core.Search;

public static class SearchHitMarks
{
    public static TextSearchHit? Apply(
        TextSearchHit hit,
        IngestChunkRow? chunk,
        IReadOnlySet<string> grantPaths)
    {
        ArgumentNullException.ThrowIfNull(hit);
        if (chunk is null)
        {
            return null;
        }

        if (chunk.Retired)
        {
            return null;
        }

        if (!grantPaths.Contains(hit.GrantPath))
        {
            return null;
        }

        return hit with { SourcePath = chunk.Path, SourceMissing = chunk.Missing };
    }

    public static IReadOnlyList<TextSearchHit> Expand(TextSearchHit hit, IReadOnlyList<IngestAliasRow> aliases)
    {
        ArgumentNullException.ThrowIfNull(hit);
        ArgumentNullException.ThrowIfNull(aliases);
        var expanded = new List<TextSearchHit>();
        foreach (var alias in aliases)
        {
            if (alias.Missing || alias.Retired)
            {
                continue;
            }

            expanded.Add(hit with { SourcePath = alias.Path });
        }

        return expanded;
    }
}
