using Recall.Core.Scope;

namespace Recall.UI.Pages;

public static class IndexNowPanel
{
    public static bool ShouldShow(IReadOnlyList<ScopeFolderGrant>? offers)
    {
        if (offers is null)
        {
            return false;
        }

        if (offers.Count == 0)
        {
            return false;
        }

        return true;
    }
}
