using Recall.Core.Ports;

namespace Recall.Core.Scope;

public static class AppleICloudPresence
{
    public static bool IsStubName(string fileName) =>
        fileName.StartsWith(".", StringComparison.Ordinal) && fileName.EndsWith(".icloud", StringComparison.OrdinalIgnoreCase);

    public static LocalContentState FromResource(bool stubName, bool? isUbiquitous, string? status)
    {
        if (stubName)
        {
            return LocalContentState.Remote;
        }

        if (isUbiquitous is null)
        {
            return LocalContentState.Unknown;
        }

        if (!isUbiquitous.Value)
        {
            return LocalContentState.Local;
        }

        if (string.Equals(status, "Downloaded", StringComparison.Ordinal))
        {
            return LocalContentState.Local;
        }

        if (string.Equals(status, "Current", StringComparison.Ordinal) || string.Equals(status, "NotDownloaded", StringComparison.Ordinal))
        {
            return LocalContentState.Remote;
        }

        return LocalContentState.Unknown;
    }
}
