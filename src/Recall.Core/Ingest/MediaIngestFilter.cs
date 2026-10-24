using Recall.Core.Preferences;
using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public static class MediaIngestFilter
{
    public static bool UserSkipped(string fileName, UserSkipAdditions user)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return true;
        }

        if (FileAdmission.IsSecret(fileName) || FileAdmission.IsDatabase(fileName) || FileAdmission.IsLockFile(fileName))
        {
            return true;
        }

        if (user.FileNames.Any(candidate => string.Equals(candidate, fileName, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(extension)
            && user.Extensions.Any(candidate => string.Equals(candidate, extension, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    public static bool KindAllowed(GrantFileKinds kinds, string fileName)
    {
        if (MediaFileKinds.IsImage(fileName))
        {
            return kinds.HasFlag(GrantFileKinds.Image);
        }

        if (MediaFileKinds.IsVideo(fileName))
        {
            return kinds.HasFlag(GrantFileKinds.Video);
        }

        if (MediaFileKinds.IsAudio(fileName))
        {
            return kinds.HasFlag(GrantFileKinds.Audio);
        }

        return false;
    }
}
