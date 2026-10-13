using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Maui.Platform;

public sealed partial class MauiGrantedFolderAccessService : IGrantedFolderAccessService
{
    private readonly IScopeGrantStore grants;

    public MauiGrantedFolderAccessService(IScopeGrantStore grants)
    {
        this.grants = grants;
    }

    public void RestorePersistedAccess() => RestorePersistedAccessPlatform(grants.List());

    private partial void RestorePersistedAccessPlatform(IReadOnlyList<ScopeFolderGrant> grants);
}
