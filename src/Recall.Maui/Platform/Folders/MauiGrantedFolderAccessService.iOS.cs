#if IOS || MACCATALYST
using Foundation;
using Recall.Core.Scope;

namespace Recall.Maui.Platform;

public sealed partial class MauiGrantedFolderAccessService
{
    private partial void RestorePersistedAccessPlatform(IReadOnlyList<ScopeFolderGrant> grants)
    {
#pragma warning disable S3267
        foreach (var grant in grants)
#pragma warning restore S3267
        {
            if (string.IsNullOrWhiteSpace(grant.ReopenToken))
            {
                continue;
            }

            try
            {
                var bytes = Convert.FromBase64String(grant.ReopenToken);
                var url = NSUrl.FromBookmarkData(
                    NSData.FromArray(bytes),
                    default,
                    null,
                    out _,
                    out _);
                url?.StartAccessingSecurityScopedResource();
            }
            catch
            {
                // Bookmark may be stale until the person re-picks the folder.
            }
        }
    }
}
#endif
