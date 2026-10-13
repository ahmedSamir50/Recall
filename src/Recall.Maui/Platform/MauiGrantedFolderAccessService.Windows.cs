#if WINDOWS
using Recall.Core.Scope;

namespace Recall.Maui.Platform;

public sealed partial class MauiGrantedFolderAccessService
{
    private partial void RestorePersistedAccessPlatform(IReadOnlyList<ScopeFolderGrant> grants)
    {
        // Windows folder grants use normal paths; no security-scope restore step.
        _ = grants;
    }
}
#endif
