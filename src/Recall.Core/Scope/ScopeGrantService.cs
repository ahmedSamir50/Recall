using Recall.Core.Faults;
using Recall.Core.Ports;

namespace Recall.Core.Scope;

public sealed class ScopeGrantService : IScopeManagerService
{
    private readonly IScopeGrantStore store;

    public ScopeGrantService(IScopeGrantStore store)
    {
        this.store = store;
    }

    public IReadOnlyList<ScopeFolderGrant> ListGrants()
    {
        return store.List();
    }

    public ScopeGrantAddResult TryAddGrant(ScopeFolderGrant grant)
    {
        var path = ScopeGrantPolicy.Normalize(grant.Path);
        var grants = store.List().ToList();
        if (grants.Any(g => ScopeGrantPolicy.Normalize(g.Path).Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
            return new ScopeGrantAddResult(ScopeGrantAddStatus.Duplicate);
        }

        foreach (var existing in grants)
        {
            var existingPath = ScopeGrantPolicy.Normalize(existing.Path);
            if (ScopeGrantPolicy.IsChildPath(existingPath, path))
            {
                return new ScopeGrantAddResult(
                    ScopeGrantAddStatus.OverlapChildOfExistingParent,
                    existingPath);
            }
        }

        grants.Add(grant with { Path = path });
        store.Save(grants);
        return new ScopeGrantAddResult(ScopeGrantAddStatus.Added);
    }

    public void RemoveGrant(string path)
    {
        var normalized = ScopeGrantPolicy.Normalize(path);
        var grants = store.List()
            .Where(g => !ScopeGrantPolicy.Normalize(g.Path).Equals(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
        store.Save(grants);
    }

    public void SetGrantFileKinds(string path, GrantFileKinds kinds)
    {
        if (kinds == GrantFileKinds.None)
        {
            throw new RecallFaultException(RecallFault.GrantFileKindsEmpty);
        }

        var normalized = ScopeGrantPolicy.Normalize(path);
        var grants = store.List().ToList();
        var index = grants.FindIndex(g =>
            ScopeGrantPolicy.Normalize(g.Path).Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return;
        }

        grants[index] = grants[index] with { EnabledKinds = kinds };
        store.Save(grants);
    }

    public bool CanContinue()
    {
        return store.List().Count > 0;
    }
}
