#if ANDROID
using Android.Content;
using Recall.Core.Scope;

namespace Recall.Maui.Platform;

public sealed partial class MauiGrantedFolderAccessService
{
    private partial void RestorePersistedAccessPlatform(IReadOnlyList<ScopeFolderGrant> grants)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity is null)
        {
            return;
        }

        foreach (var path in grants.Select(grant => grant.Path))
        {
            if (!path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var uri = Android.Net.Uri.Parse(path)!;
                activity.ContentResolver?.TakePersistableUriPermission(
                    uri,
                    ActivityFlags.GrantReadUriPermission);
            }
            catch
            {
                // Grant may have been revoked; picker flow will surface denial later.
            }
        }
    }
}
#endif
