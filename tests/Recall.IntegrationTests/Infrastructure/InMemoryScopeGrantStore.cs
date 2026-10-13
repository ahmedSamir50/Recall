using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.IntegrationTests.Infrastructure;

internal sealed class InMemoryScopeGrantStore : IScopeGrantStore
{
    private List<ScopeFolderGrant> grants = [];

    public IReadOnlyList<ScopeFolderGrant> List() => grants;

    public void Save(IReadOnlyList<ScopeFolderGrant> grants) => this.grants = grants.ToList();
}
