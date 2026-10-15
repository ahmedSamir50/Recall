using Recall.Core.Scope;

namespace Recall.Core.Search;

public enum LocalFileProbe
{
    Empty = 0,
    ContentUri = 1,
    Filesystem = 2,
}

public enum LocalFileOpenResult
{
    Opened = 0,
    Missing = 1,
    Failed = 2,
}

public static class LocalFileOpenPolicy
{
    public static LocalFileProbe Classify(string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return LocalFileProbe.Empty;
        }

        if (sourcePath.StartsWith(ContentUri.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return LocalFileProbe.ContentUri;
        }

        return LocalFileProbe.Filesystem;
    }

    public static LocalFileOpenResult Decide(LocalFileProbe probe, bool filesystemExists, bool launcherOpened, bool platformAccepted, bool threw)
    {
        if (threw)
        {
            return LocalFileOpenResult.Failed;
        }

        if (probe == LocalFileProbe.Empty)
        {
            return LocalFileOpenResult.Failed;
        }

        if (probe == LocalFileProbe.ContentUri)
        {
            return platformAccepted ? LocalFileOpenResult.Opened : LocalFileOpenResult.Failed;
        }

        if (!filesystemExists)
        {
            return LocalFileOpenResult.Missing;
        }

        return launcherOpened ? LocalFileOpenResult.Opened : LocalFileOpenResult.Failed;
    }

    public static bool Exists(LocalFileProbe probe, bool filesystemExists, bool contentStreamOpened)
    {
        if (probe == LocalFileProbe.Empty)
        {
            return false;
        }

        if (probe == LocalFileProbe.ContentUri)
        {
            return contentStreamOpened;
        }

        return filesystemExists;
    }
}
