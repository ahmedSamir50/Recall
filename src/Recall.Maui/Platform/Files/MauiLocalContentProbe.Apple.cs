#if IOS || MACCATALYST
using Foundation;
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiLocalContentProbe
{
    private static LocalContentState ProbePlatform(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LocalContentState.Unknown;
        }

        if (AppleICloudPresence.IsStubName(Path.GetFileName(path)))
        {
            return LocalContentState.Remote;
        }

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return LocalContentState.Unknown;
        }

        try
        {
            var url = NSUrl.FromFilename(path);
            if (url is null)
            {
                return LocalContentState.Unknown;
            }

            NSObject? ubiquitousObject = null;
            NSError? ubiquitousError;
            if (!url.TryGetResource(NSUrl.IsUbiquitousItemKey, out ubiquitousObject, out ubiquitousError)
                || ubiquitousError is not null)
            {
                return LocalContentState.Unknown;
            }

            bool? isUbiquitous = ubiquitousObject switch
            {
                NSNumber number => number.BoolValue,
                null => null,
                _ => null,
            };

            string? status = null;
            if (isUbiquitous == true)
            {
                NSObject? statusObject = null;
                NSError? statusError;
                if (!url.TryGetResource(NSUrl.UbiquitousItemDownloadingStatusKey, out statusObject, out statusError)
                    || statusError is not null)
                {
                    return LocalContentState.Unknown;
                }

                status = statusObject switch
                {
                    NSString ns => ns.ToString(),
                    _ => statusObject?.ToString(),
                };

                if (string.Equals(status, NSUrl.UbiquitousItemDownloadingStatusDownloaded, StringComparison.Ordinal))
                {
                    status = "Downloaded";
                }
                else if (string.Equals(status, NSUrl.UbiquitousItemDownloadingStatusCurrent, StringComparison.Ordinal))
                {
                    status = "Current";
                }
                else if (string.Equals(status, NSUrl.UbiquitousItemDownloadingStatusNotDownloaded, StringComparison.Ordinal))
                {
                    status = "NotDownloaded";
                }
            }

            return AppleICloudPresence.FromResource(false, isUbiquitous, status);
        }
        catch
        {
            return LocalContentState.Unknown;
        }
    }
}
#endif
