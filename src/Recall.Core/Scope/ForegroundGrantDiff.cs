using Recall.Core.Checkpoints;

namespace Recall.Core.Scope;

public readonly record struct WatchedFile(string Path, string ContentHash);

public static class ForegroundGrantDiff
{
    public static IReadOnlyList<ScopeChange> Diff(
        IReadOnlyList<WatchedFile> current,
        IReadOnlyList<IngestFileRow> stored)
    {
        var currentByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (current is not null)
        {
            foreach (var file in current)
            {
                if (string.IsNullOrWhiteSpace(file.Path))
                {
                    continue;
                }

                currentByPath[file.Path] = file.ContentHash;
            }
        }

        var storedByPath = new Dictionary<string, IngestFileRow>(StringComparer.OrdinalIgnoreCase);
        if (stored is not null)
        {
            foreach (var row in stored)
            {
                storedByPath[row.Path] = row;
            }
        }

        var created = new List<(string Path, string Hash)>();
        var deleted = new List<(string Path, string Hash)>();
        var changes = new List<ScopeChange>();

        foreach (var pair in currentByPath)
        {
            if (!storedByPath.TryGetValue(pair.Key, out var row))
            {
                created.Add((pair.Key, pair.Value));
                continue;
            }

            if (!string.Equals(row.ContentHash, pair.Value, StringComparison.Ordinal))
            {
                changes.Add(new ScopeChange(ScopeChangeKind.Modified, pair.Key, null));
            }
        }

        foreach (var pair in storedByPath.Where(p => !currentByPath.ContainsKey(p.Key)))
        {
            if (pair.Value.State == IngestCursorState.Missing)
            {
                continue;
            }

            deleted.Add((pair.Key, pair.Value.ContentHash));
        }

        if (created.Count == 1 && deleted.Count == 1
            && string.Equals(created[0].Hash, deleted[0].Hash, StringComparison.Ordinal))
        {
            changes.Add(new ScopeChange(ScopeChangeKind.Renamed, created[0].Path, deleted[0].Path));
            return changes;
        }

        if (created.Count > 0 && deleted.Count > 0)
        {
            var createdByHash = created.GroupBy(c => c.Hash, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            var deletedByHash = deleted.GroupBy(d => d.Hash, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            var pairedCreated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pairedDeleted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var hash in createdByHash.Keys)
            {
                if (deletedByHash.TryGetValue(hash, out var delList)
                    && createdByHash[hash].Count == 1
                    && delList.Count == 1)
                {
                    var c = createdByHash[hash][0];
                    var d = delList[0];
                    changes.Add(new ScopeChange(ScopeChangeKind.Renamed, c.Path, d.Path));
                    pairedCreated.Add(c.Path);
                    pairedDeleted.Add(d.Path);
                }
            }

            foreach (var c in created.Where(c => !pairedCreated.Contains(c.Path)))
            {
                changes.Add(new ScopeChange(ScopeChangeKind.Created, c.Path, null));
            }

            foreach (var d in deleted.Where(d => !pairedDeleted.Contains(d.Path)))
            {
                changes.Add(new ScopeChange(ScopeChangeKind.Deleted, d.Path, null));
            }

            return changes;
        }

        foreach (var c in created)
        {
            changes.Add(new ScopeChange(ScopeChangeKind.Created, c.Path, null));
        }

        foreach (var d in deleted)
        {
            changes.Add(new ScopeChange(ScopeChangeKind.Deleted, d.Path, null));
        }

        return changes;
    }
}
