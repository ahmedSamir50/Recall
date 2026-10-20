using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public static class FilesystemScopeFileReader
{
    public static async Task<GrantedTextDocument?> TryReadAsync(
        ScopeFolderGrant grant,
        string path,
        IUserSkipStore skips,
        CancellationToken cancellationToken,
        ILocalContentProbe? probe = null,
        IRemoteFileSkipReport? remoteSkip = null)
    {
        ArgumentNullException.ThrowIfNull(grant);
        ArgumentNullException.ThrowIfNull(skips);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!File.Exists(path))
        {
            return null;
        }

        var contentState = probe?.Probe(path) ?? LocalContentState.Local;
        if (!LocalContentPolicy.MayReadBytes(contentState))
        {
            remoteSkip?.Add(path);
            return null;
        }

        var user = skips.Load();
        var fileName = Path.GetFileName(path);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            var relativeDir = GetRelative(grant.Path, directory);
            if (!string.IsNullOrWhiteSpace(relativeDir) && relativeDir != ".")
            {
                var segments = relativeDir.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Any(segment => FileAdmission.IsBlockedFolder(segment, user)))
                {
                    return null;
                }
            }

            var folderName = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrWhiteSpace(folderName) && FileAdmission.IsBlockedFolder(folderName, user))
            {
                var relativeCheck = GetRelative(grant.Path, directory).Replace('\\', '/');
                if (!string.IsNullOrWhiteSpace(relativeCheck))
                {
                    return null;
                }
            }
        }

        var grantRoot = grant.Path;
        var fileDirectory = Path.GetDirectoryName(path) ?? grantRoot;
        var ignoreFiles = GitIgnoreMatcher.LoadDirectoryFiles(grantRoot, fileDirectory);
        var relativeFile = GetRelative(grantRoot, path).Replace('\\', '/');
        var ignored = GitIgnoreMatcher.IsIgnored(relativeFile, ignoreFiles);
        if (ignored)
        {
            return null;
        }

        if (FileAdmission.Decide(fileName, grant.EnabledKinds, user, ignored) != FileAdmissionResult.Read)
        {
            return null;
        }

        var isCode = FileAdmission.IsCodeFile(fileName);
        var safe = isCode
            ? await TextFileSignature.FileIsSafeCodeTextAsync(path, cancellationToken).ConfigureAwait(false)
            : await TextFileSignature.FileIsSafeTextAsync(path, cancellationToken).ConfigureAwait(false);
        if (!safe)
        {
            return null;
        }

        var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        var modified = new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);
        return new GrantedTextDocument(path, fileName, text, modified);
    }

    private static string GetRelative(string grantRoot, string path)
    {
        try
        {
            return Path.GetRelativePath(grantRoot, path);
        }
        catch (Exception ex)
        {
            // Relative path fails for unrelated roots; fall back to the full path.
            GC.KeepAlive(ex);
            return path;
        }
    }
}
