using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Text;
using System.Runtime.CompilerServices;

namespace Recall.Core.Ingest;

public sealed class DirectoryGrantedTextReader : IGrantedTextReader
{
    private readonly IUserSkipStore skips;
    private readonly ILocalContentProbe probe;
    private readonly IRemoteFileSkipReport remoteSkip;
    private readonly IDatasetSkipReport datasetSkip;

    public DirectoryGrantedTextReader(
        IUserSkipStore skips,
        ILocalContentProbe probe,
        IRemoteFileSkipReport remoteSkip,
        IDatasetSkipReport datasetSkip)
    {
        this.skips = skips;
        this.probe = probe;
        this.remoteSkip = remoteSkip;
        this.datasetSkip = datasetSkip;
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
        await foreach (var document in WalkAsync(grant.Path, grant.Path, grant.EnabledKinds, user, shared, probe, remoteSkip, datasetSkip, cancellationToken).ConfigureAwait(false))
        {
            yield return document;
        }
    }

    public async IAsyncEnumerable<string> ListAudioAndVideoAsync(
        ScopeFolderGrant grant,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Directory.Exists(grant.Path))
        {
            yield break;
        }

        var user = skips.Load();
        var shared = new List<GitIgnoreFile>();
        await foreach (var path in WalkMediaAsync(grant.Path, grant.Path, grant.EnabledKinds, user, shared, cancellationToken).ConfigureAwait(false))
        {
            yield return path;
        }
    }

    private async IAsyncEnumerable<string> WalkMediaAsync(
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

                    var directChildNames = Directory.EnumerateFileSystemEntries(entry)
                        .Select(Path.GetFileName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Select(name => name!)
                        .ToList();
                    var isGrantRoot = string.Equals(entry, grantRoot, StringComparison.OrdinalIgnoreCase);
                    if (DatasetFolderSkip.ShouldSkip(entry, isGrantRoot, directChildNames))
                    {
                        datasetSkip.Add(entry);
                        continue;
                    }

                    await foreach (var path in WalkMediaAsync(grantRoot, entry, kinds, user, shared, cancellationToken).ConfigureAwait(false))
                    {
                        yield return path;
                    }

                    continue;
                }

                var relativeFile = ToRelative(grantRoot, entry);
                var gitIgnored = GitIgnoreMatcher.IsIgnored(relativeFile, shared);
                if (gitIgnored)
                {
                    continue;
                }

                if (!MediaFileKinds.IsAudio(name) && !MediaFileKinds.IsVideo(name))
                {
                    continue;
                }

                if (MediaIngestFilter.UserSkipped(name, user) || !MediaIngestFilter.KindAllowed(kinds, name))
                {
                    continue;
                }

                if (!LocalContentPolicy.MayReadBytes(probe.Probe(entry)))
                {
                    remoteSkip.Add(entry);
                    continue;
                }

                yield return entry;
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

    private static async IAsyncEnumerable<GrantedTextDocument> WalkAsync(
        string grantRoot,
        string directory,
        GrantFileKinds kinds,
        UserSkipAdditions user,
        List<GitIgnoreFile> shared,
        ILocalContentProbe probe,
        IRemoteFileSkipReport remoteSkip,
        IDatasetSkipReport datasetSkip,
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

                    var directChildNames = Directory.EnumerateFileSystemEntries(entry)
                        .Select(Path.GetFileName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Select(name => name!)
                        .ToList();
                    var isGrantRoot = string.Equals(entry, grantRoot, StringComparison.OrdinalIgnoreCase);
                    if (DatasetFolderSkip.ShouldSkip(entry, isGrantRoot, directChildNames))
                    {
                        datasetSkip.Add(entry);
                        continue;
                    }

                    await foreach (var document in WalkAsync(grantRoot, entry, kinds, user, shared, probe, remoteSkip, datasetSkip, cancellationToken).ConfigureAwait(false))
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

                if (!LocalContentPolicy.MayReadBytes(probe.Probe(entry)))
                {
                    remoteSkip.Add(entry);
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
