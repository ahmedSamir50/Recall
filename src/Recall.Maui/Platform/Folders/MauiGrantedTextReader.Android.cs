#if ANDROID
using Android.Provider;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Text;

namespace Recall.Maui.Platform;

public sealed partial class MauiGrantedTextReader
{
    private partial bool TryPlatform(
        ScopeFolderGrant grant,
        CancellationToken cancellationToken,
        out IAsyncEnumerable<GrantedTextDocument> documents)
    {
        if (!grant.Path.StartsWith(ContentUri.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            documents = NoDocuments;
            return false;
        }

        documents = ReadContentTreeAsync(grant, files.Store.Load(), cancellationToken);
        return true;
    }

    private const int FlagPartialFile = 1 << 15;
    private const int FlagVirtualDocument = 1 << 9;

    private static LocalContentState FlagsState(int flags) =>
        AndroidDocumentPresence.FromFlags(
            (flags & FlagPartialFile) != 0,
            (flags & FlagVirtualDocument) != 0,
            OperatingSystem.IsAndroidVersionAtLeast(36),
            availableLocally: null,
            downloadProgress: false,
            downloadError: false);

    private async IAsyncEnumerable<GrantedTextDocument> ReadContentTreeAsync(
        ScopeFolderGrant grant,
        UserSkipAdditions user,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var resolver = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.ContentResolver;
        var tree = Android.Net.Uri.Parse(grant.Path);
        if (resolver is null || tree is null)
        {
            yield break;
        }

        var rootId = DocumentsContract.GetTreeDocumentId(tree);
        if (string.IsNullOrEmpty(rootId))
        {
            yield break;
        }

        var shared = new List<GitIgnoreFile>();
        await foreach (var document in WalkTreeAsync(resolver, tree, rootId, string.Empty, grant, user, shared, remoteSkip, cancellationToken).ConfigureAwait(false))
        {
            yield return document;
        }
    }

    private static async IAsyncEnumerable<GrantedTextDocument> WalkTreeAsync(
        Android.Content.ContentResolver resolver,
        Android.Net.Uri tree,
        string parentId,
        string parentRelative,
        ScopeFolderGrant grant,
        UserSkipAdditions user,
        List<GitIgnoreFile> shared,
        IRemoteFileSkipReport remoteSkip,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var children = DocumentsContract.BuildChildDocumentsUriUsingTree(tree, parentId);
        if (children is null)
        {
            yield break;
        }

        using var cursor = resolver.Query(
            children,
            [
                DocumentsContract.Document.ColumnDocumentId,
                DocumentsContract.Document.ColumnDisplayName,
                DocumentsContract.Document.ColumnMimeType,
                DocumentsContract.Document.ColumnLastModified,
                DocumentsContract.Document.ColumnFlags,
            ],
            null,
            null,
            null);
        if (cursor is null)
        {
            yield break;
        }

        var idColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDocumentId);
        var nameColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDisplayName);
        var mimeColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnMimeType);
        var modifiedColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnLastModified);
        var flagsColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnFlags);
        var rows = new List<(string Id, string Name, string Mime, long ModifiedMs, int Flags)>();
        string? gitIgnoreId = null;
        var gitIgnoreFlags = 0;
        string? gitDirId = null;
        while (cursor.MoveToNext())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = idColumn >= 0 ? cursor.GetString(idColumn) ?? string.Empty : string.Empty;
            var name = nameColumn >= 0 ? cursor.GetString(nameColumn) ?? string.Empty : string.Empty;
            var mime = mimeColumn >= 0 ? cursor.GetString(mimeColumn) ?? string.Empty : string.Empty;
            var modifiedMs = 0L;
            if (modifiedColumn >= 0 && !cursor.IsNull(modifiedColumn))
            {
                modifiedMs = cursor.GetLong(modifiedColumn);
            }

            var flags = flagsColumn >= 0 && !cursor.IsNull(flagsColumn) ? cursor.GetInt(flagsColumn) : 0;
            var isDir = string.Equals(mime, DocumentsContract.Document.MimeTypeDir, StringComparison.Ordinal);
            if (!isDir && string.Equals(name, ".gitignore", StringComparison.OrdinalIgnoreCase))
            {
                gitIgnoreId = id;
                gitIgnoreFlags = flags;
                rows.Add((id, name, mime, modifiedMs, flags));
                continue;
            }

            if (isDir && string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase))
            {
                gitDirId = id;
                continue;
            }

            rows.Add((id, name, mime, modifiedMs, flags));
        }

        var added = 0;
        if (!string.IsNullOrEmpty(gitIgnoreId))
        {
            var ignoreUri = DocumentsContract.BuildDocumentUriUsingTree(tree, gitIgnoreId);
            if (ignoreUri is not null && LocalContentPolicy.MayReadBytes(FlagsState(gitIgnoreFlags)))
            {
                using var ignoreStream = resolver.OpenInputStream(ignoreUri);
                if (ignoreStream is not null)
                {
                    using var ignoreReader = new StreamReader(ignoreStream, detectEncodingFromByteOrderMarks: true);
                    var ignoreText = await ignoreReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                    var lines = ignoreText.Split(["\r\n", "\n"], StringSplitOptions.None);
                    shared.Add(new GitIgnoreFile(parentRelative, lines));
                    added++;
                }
            }
        }

        if (!string.IsNullOrEmpty(gitDirId))
        {
            var excludeLines = ReadExcludeLines(resolver, tree, gitDirId, cancellationToken);
            if (excludeLines is not null)
            {
                shared.Add(new GitIgnoreFile(parentRelative, excludeLines));
                added++;
            }
        }

        try
        {
            foreach (var (id, name, mime, modifiedMs, flags) in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.Equals(name, ".gitignore", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(mime, DocumentsContract.Document.MimeTypeDir, StringComparison.Ordinal))
                {
                    if (id.Length == 0)
                    {
                        continue;
                    }

                    if (FileAdmission.IsBlockedFolder(name, user))
                    {
                        continue;
                    }

                    var childRelative = string.IsNullOrEmpty(parentRelative) ? name : parentRelative + Path.AltDirectorySeparatorChar + name;
                    if (GitIgnoreMatcher.IsIgnored(childRelative, shared)
                        || GitIgnoreMatcher.LastDecision(childRelative, true, shared) == GitIgnoreDecision.Ignore)
                    {
                        continue;
                    }

                    await foreach (var document in WalkTreeAsync(resolver, tree, id, childRelative, grant, user, shared, remoteSkip, cancellationToken).ConfigureAwait(false))
                    {
                        yield return document;
                    }

                    continue;
                }

                if (id.Length == 0)
                {
                    continue;
                }

                var relativeFile = string.IsNullOrEmpty(parentRelative) ? name : parentRelative + Path.AltDirectorySeparatorChar + name;
                var gitIgnored = GitIgnoreMatcher.IsIgnored(relativeFile, shared);
                if (FileAdmission.Decide(name, grant.EnabledKinds, user, gitIgnored) != FileAdmissionResult.Read)
                {
                    continue;
                }

                var documentUri = DocumentsContract.BuildDocumentUriUsingTree(tree, id);
                if (documentUri is null)
                {
                    continue;
                }

                if (!LocalContentPolicy.MayReadBytes(FlagsState(flags)))
                {
                    remoteSkip.Add(documentUri.ToString()!);
                    continue;
                }

                using var stream = resolver.OpenInputStream(documentUri);
                if (stream is null)
                {
                    continue;
                }

                var sample = new byte[TextFileSignature.SampleBytes];
                var sampleCount = await stream.ReadAsync(sample, cancellationToken).ConfigureAwait(false);
                var isCode = FileAdmission.IsCodeFile(name);
                var safe = isCode
                    ? TextFileSignature.IsSafeCodeText(sample.AsSpan(0, sampleCount))
                    : TextFileSignature.IsSafeText(sample.AsSpan(0, sampleCount));
                if (!safe)
                {
                    continue;
                }

                using var buffered = new MemoryStream(sampleCount + 1024);
                await buffered.WriteAsync(sample.AsMemory(0, sampleCount), cancellationToken).ConfigureAwait(false);
                await stream.CopyToAsync(buffered, cancellationToken).ConfigureAwait(false);
                buffered.Position = 0;
                using var reader = new StreamReader(buffered, detectEncodingFromByteOrderMarks: true);
                var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                var modified = DateTimeOffset.UnixEpoch;
                if (modifiedMs > 0)
                {
                    modified = DateTimeOffset.FromUnixTimeMilliseconds(modifiedMs);
                }

                yield return new GrantedTextDocument(documentUri.ToString()!, name, text, modified);
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

    private static IReadOnlyList<string>? ReadExcludeLines(
        Android.Content.ContentResolver resolver,
        Android.Net.Uri tree,
        string gitDirId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var info = FindChildId(resolver, tree, gitDirId, "info");
        if (info.Id is null)
        {
            return null;
        }

        var exclude = FindChildId(resolver, tree, info.Id, "exclude");
        if (exclude.Id is null)
        {
            return null;
        }

        var excludeUri = DocumentsContract.BuildDocumentUriUsingTree(tree, exclude.Id);
        if (excludeUri is null)
        {
            return null;
        }

        if (!LocalContentPolicy.MayReadBytes(FlagsState(exclude.Flags)))
        {
            return null;
        }

        using var stream = resolver.OpenInputStream(excludeUri);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        return text.Split(["\r\n", "\n"], StringSplitOptions.None);
    }

    private static (string? Id, int Flags) FindChildId(
        Android.Content.ContentResolver resolver,
        Android.Net.Uri tree,
        string parentId,
        string childName)
    {
        var children = DocumentsContract.BuildChildDocumentsUriUsingTree(tree, parentId);
        if (children is null)
        {
            return (null, 0);
        }

        using var cursor = resolver.Query(
            children,
            [
                DocumentsContract.Document.ColumnDocumentId,
                DocumentsContract.Document.ColumnDisplayName,
                DocumentsContract.Document.ColumnFlags,
            ],
            null,
            null,
            null);
        if (cursor is null)
        {
            return (null, 0);
        }

        var idColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDocumentId);
        var nameColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnDisplayName);
        var flagsColumn = cursor.GetColumnIndex(DocumentsContract.Document.ColumnFlags);
        while (cursor.MoveToNext())
        {
            var id = idColumn >= 0 ? cursor.GetString(idColumn) ?? string.Empty : string.Empty;
            var name = nameColumn >= 0 ? cursor.GetString(nameColumn) ?? string.Empty : string.Empty;
            var flags = flagsColumn >= 0 && !cursor.IsNull(flagsColumn) ? cursor.GetInt(flagsColumn) : 0;
            if (string.Equals(name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return (id, flags);
            }
        }

        return (null, 0);
    }
}
#endif
