using Recall.Core.Scope;

namespace Recall.UI.Components;

public static class FolderGrantMessages
{
    public static string? OverlapKey(ScopeGrantAddStatus status)
    {
        if (status == ScopeGrantAddStatus.OverlapChildOfExistingParent)
        {
            return "Folders_OverlapWarning";
        }

        if (status == ScopeGrantAddStatus.OverlapParentReplacesChildren)
        {
            return "Folders_OverlapWarning";
        }

        return null;
    }
}
