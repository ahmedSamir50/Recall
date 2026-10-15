using System.Runtime.CompilerServices;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public sealed class DirectoryGrantedTextReader : IGrantedTextReader
{
    private readonly IUserSkipStore skips;

    public DirectoryGrantedTextReader(IUserSkipStore skips)
    {
        this.skips = skips;
    }

    public IUserSkipStore Store => skips;

    public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(
        ScopeFolderGrant grant,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Directory.Exists(grant.Path))
        {
            yield break;
        }

        var user = skips.Load();
        var shared = new List<GitIgnoreFile>();
        await foreach (var document in WalkAsync(grant.Path, grant.Path, grant.EnabledKinds, user, shared, cancellationToken).ConfigureAwait(false))
        {
            yield return document;
        }
    }

    private static async IAsyncEnumerable<GrantedTextDocument> WalkAsync(
        string grantRoot,
        string directory,
        GrantFileKinds kinds,
        UserSkipAdditions user,
        List<GitIgnoreFile> shared,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var added = AddDirectoryIgnores(grantRoot, directory, shared);
        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = Path.GetFileName(entry);
                if (Directory.Exists(entry))
                {
                    if (FileAdmission.IsBlockedFolder(name, user))
                    {
                        continue;
                    }

                    var relativeDir = ToRelative(grantRoot, entry);
                    if (IsDirectoryIgnored(relativeDir, shared))
                    {
                        continue;
                    }

                    await foreach (var document in WalkAsync(grantRoot, entry, kinds, user, shared, cancellationToken).ConfigureAwait(false))
                    {
                        yield return document;
                    }

                    continue;
                }

                var relativeFile = ToRelative(grantRoot, entry);
                var gitIgnored = GitIgnoreMatcher.IsIgnored(relativeFile, shared);
                if (FileAdmission.Decide(name, kinds, user, gitIgnored) != FileAdmissionResult.Read)
                {
                    continue;
                }

                var isCode = FileAdmission.IsCodeFile(name);
                var safe = isCode
                    ? await TextFileSignature.FileIsSafeCodeTextAsync(entry, cancellationToken).ConfigureAwait(false)
                    : await TextFileSignature.FileIsSafeTextAsync(entry, cancellationToken).ConfigureAwait(false);
                if (!safe)
                {
                    continue;
                }

                var text = await File.ReadAllTextAsync(entry, cancellationToken).ConfigureAwait(false);
                var modified = new DateTimeOffset(File.GetLastWriteTimeUtc(entry), TimeSpan.Zero);
                yield return new GrantedTextDocument(entry, name, text, modified);
            }
        }
        finally
        {
            if (added > 0)
            {
                shared.RemoveRange(shared.Count - added, added);
            }
        }
    }

    private static int AddDirectoryIgnores(string grantRoot, string directory, List<GitIgnoreFile> shared)
    {
        var added = 0;
        var relative = ToRelative(grantRoot, directory);
        if (relative == ".")
        {
            relative = string.Empty;
        }

        var ignorePath = Path.Combine(directory, ".gitignore");
        if (File.Exists(ignorePath))
        {
            var lines = File.ReadAllLines(ignorePath);
            shared.Add(new GitIgnoreFile(relative, lines));
            added++;
        }

        var excludePath = Path.Combine(directory, ".git", "info", "exclude");
        if (File.Exists(excludePath))
        {
            var lines = File.ReadAllLines(excludePath);
            shared.Add(new GitIgnoreFile(relative, lines));
            added++;
        }

        return added;
    }

    private static bool IsDirectoryIgnored(string relativeDir, List<GitIgnoreFile> shared)
    {
        if (string.IsNullOrEmpty(relativeDir) || relativeDir == ".")
        {
            return false;
        }

        if (GitIgnoreMatcher.IsIgnored(relativeDir, shared))
        {
            return true;
        }

        return GitIgnoreMatcher.LastDecision(relativeDir, true, shared) == GitIgnoreDecision.Ignore;
    }

    private static string ToRelative(string grantRoot, string path)
    {
        var relative = Path.GetRelativePath(grantRoot, path).Replace('\\', '/');
        return relative;
    }
}
