## src/Recall.Core/Scope/ScopeWatchRoots.cs

```csharp
namespace Recall.Core.Scope;
public static class ScopeWatchRoots
ScopeWatchRoots
    public static IReadOnlyList<string> Select(IReadOnlyList<string>? paths)
    if (paths is null || paths.Count == 0)
    if (string.IsNullOrWhiteSpace(path))
    if (normalized.Any(existing => existing.Equals(value, StringComparison.OrdinalIgnoreCase)))
    if (normalized.Any(other => ScopeGrantPolicy.IsChildPath(other, path)))
```

## src/Recall.Core/Scope/ScopeGrantPolicy.cs

```csharp
namespace Recall.Core.Scope;
public static class ScopeGrantPolicy
    public static bool IsChildPath(string parentPath, string childPath)
    public static bool IsInsideGrant(string grantPath, string filePath)
    public static string Normalize(string path)
        if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(childPath))
        if (parent.Equals(child, StringComparison.OrdinalIgnoreCase))
        if (!child.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
        if (string.IsNullOrWhiteSpace(grantPath) || string.IsNullOrWhiteSpace(filePath))
        if (file.Equals(grant, StringComparison.OrdinalIgnoreCase))
        if (trimmed.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
```


## src/Recall.Core/Scope/ScopeGrantService.cs

```csharp
namespace Recall.Core.Scope;
public sealed class ScopeGrantService : IScopeManagerService
    public ScopeGrantService(IScopeGrantStore store)
    public IReadOnlyList<ScopeFolderGrant> ListGrants()
    public ScopeGrantAddResult TryAddGrant(ScopeFolderGrant grant)
    public void RemoveGrant(string path)
    public void SetGrantFileKinds(string path, GrantFileKinds kinds)
    public bool CanContinue()
        if (grants.Any(g => ScopeGrantPolicy.Normalize(g.Path).Equals(path, StringComparison.OrdinalIgnoreCase)))
            if (ScopeGrantPolicy.IsChildPath(existingPath, path))
        if (kinds == GrantFileKinds.None)
            throw new RecallFaultException(RecallFault.GrantFileKindsEmpty);
        if (index < 0)
```



```csharp
if (string.IsNullOrWhiteSpace(grant.Path))
if (children.Count > 0)
``` 
## src/Recall.Core/Scope/ScopeFolderGrant.cs

```csharp
namespace Recall.Core.Scope;
public sealed record ScopeFolderGrant(
```

No branch.


## src/Recall.Core/Scope/ScopeGrantAddResult.cs

```csharp
namespace Recall.Core.Scope;
public enum ScopeGrantAddStatus
public sealed record ScopeGrantAddResult(ScopeGrantAddStatus Status, string? Message = null);
```

No branch.


## src/Recall.Core/Scope/GrantFileKinds.cs

```csharp
namespace Recall.Core.Scope;
public enum GrantFileKinds
```

No branch.


## src/Recall.Core/Scope/ContentUri.cs

```csharp
namespace Recall.Core.Scope;
public static class ContentUri
```

No branch.



```csharp
if (string.IsNullOrWhiteSpace(path))
if (path.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
``` 
## src/Recall.Core/Scope/GitIgnoreMatcher.cs

```csharp
namespace Recall.Core.Scope;
public sealed record GitIgnoreFile(string DirectoryRelative, IReadOnlyList<string> Lines);
public readonly record struct GitIgnorePattern(string Body, bool Negated, bool DirectoryOnly, bool Anchored);
public enum GitIgnoreDecision
public static class GitIgnoreMatcher
    public static bool TryParse(string line, out GitIgnorePattern pattern)
    public static GitIgnoreDecision LastDecision(string relativePath, bool isDirectory, IReadOnlyList<GitIgnoreFile> files)
    public static bool IsIgnored(string relativePath, IReadOnlyList<GitIgnoreFile> files)
    public static IReadOnlyList<GitIgnoreFile> LoadDirectoryFiles(string grantRoot, string directory)
    if (string.IsNullOrWhiteSpace(line))
    if (line.StartsWith("\\#", StringComparison.Ordinal) || line.StartsWith("\\!", StringComparison.Ordinal))
    if (string.IsNullOrEmpty(body))
    if (body.EndsWith("/", StringComparison.Ordinal) && body.Length > 0)
    if (body.StartsWith("/", StringComparison.Ordinal))
    if (line[0] == '#')
    if (rest.StartsWith("!", StringComparison.Ordinal))
    if (string.IsNullOrEmpty(rest))
    if (rest.EndsWith("/", StringComparison.Ordinal))
    if (rest.StartsWith("/", StringComparison.Ordinal))
    if (string.IsNullOrEmpty(relativePath) || files is null || files.Count == 0)
    if (string.IsNullOrEmpty(candidate))
    if (string.IsNullOrEmpty(dir))
    else if (candidate.Equals(dir, StringComparison.Ordinal))
    else if (candidate.StartsWith(dir + "/", StringComparison.Ordinal))
    if (file.Lines is null)
    if (!TryParse(line, out var pattern))
    if (pattern.DirectoryOnly && !isDirectory)
    if (pattern.Anchored)
    if (!matched)
    if (matched)
    if (LastDecision(prefix, true, files) == GitIgnoreDecision.Ignore)
    if (string.IsNullOrWhiteSpace(grantRoot) || string.IsNullOrWhiteSpace(directory))
    if (!ScopeGrantPolicy.IsInsideGrant(grantRoot, directory))
    if (grantNorm.Equals(dirNorm, StringComparison.OrdinalIgnoreCase))
    else if (ScopeGrantPolicy.IsChildPath(grantNorm, dirNorm))
    if (File.Exists(ignorePath))
    if (File.Exists(excludePath))
    if (c == '*')
    if (i + 1 < pattern.Length && pattern[i + 1] == '*')
    if (after == '/')
    if (beforeSlash && (i + 2 >= pattern.Length))
    if (c == '?')
    if (c == '[')
    if (close < 0)
    if (content.Length == 0)
    if (content[0] == '!')
    if (c == '\\')
    if ("+()^$.{}|".IndexOf(c, StringComparison.Ordinal) >= 0)
    catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
```



## src/Recall.Core/Scope/FileAdmission.cs

```csharp
namespace Recall.Core.Scope;
public enum FileAdmissionResult
public static class FileAdmission
    if (string.IsNullOrWhiteSpace(fileName))
    if (IsSecret(fileName))
    if (IsDatabase(fileName))
    if (IsLockFile(fileName))
    if (user.FileNames.Any(candidate => string.Equals(candidate, fileName, StringComparison.OrdinalIgnoreCase)))
    if (!string.IsNullOrEmpty(extension)
    if (IsMinified(fileName))
    if (gitIgnored)
    if (IsCodeFile(fileName))
    if (kinds.HasFlag(GrantFileKinds.Code))
    if (GrantTextFiles.IsTextFile(fileName))
    if (kinds.HasFlag(GrantFileKinds.Text))
    if (string.IsNullOrWhiteSpace(folderName))
    if (BuiltInSkips.ContainsFolder(folderName))
    if (user.FolderNames.Any(candidate => string.Equals(candidate, folderName, StringComparison.OrdinalIgnoreCase)))
    if (!string.IsNullOrEmpty(extension))
    if (BuiltInSkips.ContainsSecretExtension(withoutDot))
    if (BuiltInSkips.ContainsSecretName(fileName))
    if (fileName.Equals("id_rsa", StringComparison.OrdinalIgnoreCase)
    if (fileName.StartsWith(".env", StringComparison.OrdinalIgnoreCase)
    if (!fileName.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase)
    if (bare.Equals("secret", StringComparison.OrdinalIgnoreCase)
    if (string.IsNullOrEmpty(extension))
    if (!string.IsNullOrEmpty(extension) && BuiltInSkips.ContainsCodeExtension(extension))
    if (BuiltInSkips.ContainsExactCodeName(fileName))
```



## src/Recall.Core/Scope/BuiltInSkips.cs

```csharp
namespace Recall.Core.Scope;
public static class BuiltInSkips
    if (string.IsNullOrEmpty(name))
    if (string.IsNullOrEmpty(extensionWithDot))
    if (string.IsNullOrEmpty(fileName))
    if (string.IsNullOrEmpty(extensionWithoutDot))
```



## 

```csharp
namespace Recall.Adapters.DependencyInjection;
public type AdapterServiceCollectionExtensions
AdapterServiceCollectionExtensions
public static class AdapterServiceCollectionExtensions
public static IServiceCollection AddRecallAdapters(

No branch.
``` 



## src/Recall.Core/Checkpoints/CheckpointSql.cs

```csharp
namespace Recall.Core.Checkpoints;
public type CheckpointSql
CheckpointSql
public static class CheckpointSql
public const string FileName = "checkpoints.db";
public const string CreateFileTable =
public const string CreateChunkTable =
public const string CreateChunkPathIndex =
public const string CreateGrantTable =
public const string AddEncoderHashColumn =
public const string AddChunkPageStartColumn =
public const string AddChunkPageEndColumn =
public const string AddChunkCharStartColumn =
public const string AddChunkCharEndColumn =
public const string CreateOcrPageTable =
public const string EnsureSchema =

No branch.
``` 

`ingest_chunk` gains `page_start`, `page_end`, `char_start`, and `char_end`, each `INTEGER NOT NULL DEFAULT 0`, added with `ALTER TABLE ingest_chunk ADD COLUMN IF NOT EXISTS`. `char_start` and `char_end` are indexes into the whole document string, so the second patch of one file does not start at 0. `ocr_page(path TEXT NOT NULL, page_number INTEGER NOT NULL, PRIMARY KEY(path, page_number))` holds the pages `BrokenArabic.NeedsOcr` flags; those pages are not embedded and wait for the OCR epic. There is no `document_patch` table.



## src/Recall.Core/Checkpoints/IIngestCursorStore.cs

```csharp
namespace Recall.Core.Checkpoints;
public type IIngestCursorStore
IIngestCursorStore
public interface IIngestCursorStore
public sealed record MediaFileRow(string Path, Modality Modality, int Width);
public sealed record AgeCutPending(string LongId, string ShortId);
public sealed record PinResumeRow(string Path, string LongId, string ShortId);
public void ReplaceDocumentPatches(string path, IReadOnlyList<ChunkWrite> patches);
public void ReplaceOcrPages(string path, IReadOnlyList<int> pageNumbers);
public IReadOnlyList<int> ListOcrPages(string path);
public void UpsertMediaFile(string path, Modality modality);
public MediaFileRow? FindMediaFile(string path, Modality modality);
public void SetMediaWidth(string path, Modality modality, int width);
public void CommitAgeCut(string fromContentId, string toContentId);
public void CompleteAgeCut(string longId);
public bool IsPinned(string path);
public void StagePinResume(string path, string longId, string shortId);
public void CompletePinResume(string path);
public void ResetTextIngest()

No branch.
``` 



## src/Recall.Core/Checkpoints/IngestCursorModels.cs

```csharp
namespace Recall.Core.Checkpoints;
public type IngestCursorState
IngestCursorState
public type IngestCursorDecision
IngestCursorDecision
public type IngestFileRow
IngestFileRow
public type IngestAliasRow
IngestAliasRow
public type Bm25OnlyRow
Bm25OnlyRow
public type ChunkWrite
ChunkWrite
public type IngestChunkRow
IngestChunkRow
public type GrantScanRow
GrantScanRow
public type IngestCursorPolicy
IngestCursorPolicy
public enum IngestCursorState
public enum IngestCursorDecision
public sealed record IngestFileRow(string Path, string ContentHash, IngestCursorState State, string GrantPath, string EncoderHash);
public sealed record IngestAliasRow(string ContentId, string Path, bool Missing, bool Retired);
public sealed record Bm25OnlyRow(string ContentId, string Path, string RawText);
public sealed record ChunkWrite(string ContentId, int Ordinal, int PageStart = 0, int PageEnd = 0, int CharStart = 0, int CharEnd = 0);
public sealed record IngestChunkRow(string ContentId, string Path, int Ordinal, bool Missing, bool Retired, int PageStart = 0, int PageEnd = 0, int CharStart = 0, int CharEnd = 0);
public sealed record GrantScanRow(string GrantPath, IngestCursorState State);
public static class IngestCursorPolicy
public static IngestCursorDecision Decide(IngestFileRow? row, string contentHash)
    if (string.IsNullOrWhiteSpace(contentHash))
    if (row is null)
    if (row.State == IngestCursorState.Reading)
    if (row.State == IngestCursorState.Missing)
    if (row.State == IngestCursorState.Finished
``` 



## src/Recall.Core/Checkpoints/SqliteIngestCursorStore.cs

```csharp
namespace Recall.Core.Checkpoints;
public type SqliteIngestCursorStore
SqliteIngestCursorStore
public sealed class SqliteIngestCursorStore : IIngestCursorStore
public SqliteIngestCursorStore(string databasePath)
public IngestFileRow? GetFile(string path)
public IReadOnlyList<IngestFileRow> ListFiles(string grantPath)
public IReadOnlyList<IngestChunkRow> ListChunks(string path)
public IngestChunkRow? FindChunk(string contentId)
public void MarkReading(string path, string contentHash, string grantPath)
public void MarkFinished(string path, string contentHash, string grantPath, IReadOnlyList<(string ContentId, int Ordinal)> chunks)
public void RenamePath(string previousPath, string nextPath)
public void MarkMissing(string path)
public void RetirePath(string path)
public void MarkGrantReading(string grantPath)
public void MarkGrantFinished(string grantPath)
public void ResetTextIngest()
public bool IsGrantFinished(string grantPath)
public void ReplaceDocumentPatches(string path, IReadOnlyList<ChunkWrite> patches)
public void ReplaceOcrPages(string path, IReadOnlyList<int> pageNumbers)
    if (string.IsNullOrWhiteSpace(path))
    if (string.IsNullOrWhiteSpace(path))
public IReadOnlyList<int> ListOcrPages(string path)
    if (string.IsNullOrWhiteSpace(path))
    if (string.IsNullOrWhiteSpace(databasePath))
    if (reader.Read())
    if (string.IsNullOrWhiteSpace(path))
    if (string.IsNullOrWhiteSpace(chunk.ContentId))
    if (string.IsNullOrWhiteSpace(patch.ContentId))
    if (!ChunkRowExists(connection, transaction, chunk.ContentId))
    if (string.IsNullOrWhiteSpace(contentId) || string.IsNullOrWhiteSpace(path))
    if (string.IsNullOrWhiteSpace(row.ContentId) || string.IsNullOrWhiteSpace(row.Path))
    if (string.IsNullOrWhiteSpace(path))
    if (!writeLock.Wait(0))
    if (!reader.Read())
    if (value is null || value == DBNull.Value)
    if (acquired)
    if (!string.IsNullOrWhiteSpace(directory))
    if (!HasColumn(connection, transaction, "PRAGMA table_info(ingest_file)", "encoder_hash"))
    if (HasColumn(connection, transaction, "PRAGMA table_info(ingest_chunk)", columnName))
    if (!string.Equals(tableName, "ingest_file", StringComparison.Ordinal))
    if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
    if (existing.Add(key))
    if (width is not (MediaCollections.Width or MediaCollections.OldWidth))
    if (string.IsNullOrWhiteSpace(fromContentId) || string.IsNullOrWhiteSpace(toContentId))
    if (string.IsNullOrWhiteSpace(longId))
    if (string.IsNullOrWhiteSpace(longId) || string.IsNullOrWhiteSpace(shortId))
``` 

`ReplaceDocumentPatches` deletes the live `ingest_chunk` rows for one path and writes the given patch rows with their page and char spans. A blank path throws `ArgumentException` with message `Path must not be blank.`, and a null patch list throws `ArgumentNullException`. `ingest_chunk` gains `page_start`, `page_end`, `char_start`, and `char_end`, each `INTEGER NOT NULL DEFAULT 0`, added with `ALTER TABLE ingest_chunk ADD COLUMN IF NOT EXISTS`. A plain note stores 0 for both page columns. Shrink, alias, and delete keep using `ingest_chunk` only.




```csharp
if (duplicate is not null)
if (liveById.ContainsKey(chunk.ContentId))
``` 
```csharp
if (!ChunkRowExists(connection, transaction, patch.ContentId))
if (!TryAcquireWrite())
if (writeDepth > 0 && writeOwner == id)
if (writeDepth > 0)
if (Volatile.Read(ref spanReady) != 0)
if (spanReady != 0)
```

```csharp
if (!IngestPathKind.TryGetKind(path, out var pathKind) || pathKind != kind)
if (IngestKindCountPolicy.IsSeen(state))
if (IngestKindCountPolicy.IsFinished(state))
if (enabled == GrantFileKinds.None)
if (enabled.HasFlag(kind))
if (!IngestPathKind.TryGetKind(path, out var pathKind) || !finishedByKind.ContainsKey(pathKind))
if (finishedByKind.ContainsKey(kind))
if (string.IsNullOrWhiteSpace(grantPath))
if (!HasColumn(connection, transaction, "PRAGMA table_info(ingest_file)", "missing_status_key"))
```

## src/Recall.Core/Scope/DisabledScopeWatcher.cs

```csharp
namespace Recall.Core.Scope;
public type DisabledScopeWatcher
DisabledScopeWatcher
public sealed class DisabledScopeWatcher : IFileScopeWatcherService
public int StartCount { get; private set; }
public bool Watching { get; private set; }
public void Start(IReadOnlyList<ScopeFolderGrant> grants, IScopeChangeSink sink)
public void Stop()

No branch.
``` 



## src/Recall.Core/Scope/ForegroundGrantDiff.cs

```csharp
namespace Recall.Core.Scope;
public type WatchedFile
WatchedFile
public type ForegroundGrantDiff
ForegroundGrantDiff
public readonly record struct WatchedFile(string Path, string ContentHash);
public static class ForegroundGrantDiff
public static IReadOnlyList<ScopeChange> Diff(
    if (current is not null)
    if (string.IsNullOrWhiteSpace(file.Path))
    if (stored is not null)
    if (!storedByPath.TryGetValue(pair.Key, out var row))
    if (!string.Equals(row.ContentHash, pair.Value, StringComparison.Ordinal))
    if (pair.Value.State == IngestCursorState.Missing)
    if (created.Count == 1 && deleted.Count == 1
    if (created.Count > 0 && deleted.Count > 0)
    if (deletedByHash.TryGetValue(hash, out var delList)
``` 



## src/Recall.Core/Scope/ScopeChange.cs

```csharp
namespace Recall.Core.Scope;
public type ScopeChangeKind
ScopeChangeKind
public type ScopeChange
ScopeChange
public enum ScopeChangeKind
public sealed record ScopeChange(ScopeChangeKind Kind, string Path, string? PreviousPath);

No branch.
``` 



## src/Recall.Core/Scope/ScopeChangeBuffer.cs

```csharp
namespace Recall.Core.Scope;
public type ScopeChangeBufferAddResult
ScopeChangeBufferAddResult
public type ScopeChangeBuffer
ScopeChangeBuffer
public enum ScopeChangeBufferAddResult
public sealed class ScopeChangeBuffer
public bool NeedsRescan { get { lock (gate) { return needsRescan; } } }
public int Count { get { lock (gate) { return items.Count; } } }
public ScopeChangeBufferAddResult TryAdd(ScopeChange change)
public IReadOnlyList<ScopeChange> Drain()
public void AcknowledgeRescan()
public void RequestRescan()
    if (string.IsNullOrWhiteSpace(change.Path))
    if (items.ContainsKey(change.Path))
    if (items.Count >= ScopeWatchLimits.ChangeCapacity)
``` 



## src/Recall.Core/Scope/ScopeWatcherKindPolicy.cs

```csharp
namespace Recall.Core.Scope;
public type ScopeWatcherKind
ScopeWatcherKind
public type ScopeWatcherKindPolicy
ScopeWatcherKindPolicy
public enum ScopeWatcherKind
public static class ScopeWatcherKindPolicy
public static ScopeWatcherKind Choose(RecallShellKind kind)
    if (kind == RecallShellKind.Windows || kind == RecallShellKind.Android || kind == RecallShellKind.MacCatalyst)
``` 



## src/Recall.Core/Scope/ScopeWatchLimits.cs

```csharp
namespace Recall.Core.Scope;
public type ScopeWatchLimits
ScopeWatchLimits
public static class ScopeWatchLimits
public const int ChangeCapacity = 64;

No branch.
``` 



## src/Recall.Core/Scope/WindowsFileScopeWatcher.cs

```csharp
namespace Recall.Core.Scope;
public type WindowsFileScopeWatcher
WindowsFileScopeWatcher
public sealed class WindowsFileScopeWatcher : IFileScopeWatcherService, IDisposable
public bool Watching { get; private set; }
public int WatcherCount => watchers.Count;
public static bool CanWatchDirectories(bool isIos, bool isMacCatalyst)
public void Start(IReadOnlyList<ScopeFolderGrant> grants, IScopeChangeSink sink)
public void Stop()
public void Dispose()
    if (isIos && !isMacCatalyst)
    if (!CanWatchDirectories(OperatingSystem.IsIOS(), OperatingSystem.IsMacCatalyst()))
    if (ContentUri.IsContent(path))
    if (!Directory.Exists(path))
    if (watchers.Count == 0)
    if (WindowsWatchEventMapper.TryMap(WindowsWatchKind.Created, args.FullPath, null, out var change))
    if (WindowsWatchEventMapper.TryMap(WindowsWatchKind.Changed, args.FullPath, null, out var change))
    if (WindowsWatchEventMapper.TryMap(WindowsWatchKind.Renamed, args.FullPath, args.OldFullPath, out var change))
    if (WindowsWatchEventMapper.TryMap(WindowsWatchKind.Deleted, args.FullPath, null, out var change))
``` 



## src/Recall.Core/Scope/WindowsWatchEventMapper.cs

```csharp
namespace Recall.Core.Scope;
public type WindowsWatchKind
WindowsWatchKind
public type WindowsWatchEventMapper
WindowsWatchEventMapper
public enum WindowsWatchKind
public static class WindowsWatchEventMapper
public static bool TryMap(WindowsWatchKind kind, string path, string? previousPath, out ScopeChange change)
    if (string.IsNullOrWhiteSpace(path))
    if (kind == WindowsWatchKind.Error)
    if (kind == WindowsWatchKind.Renamed)
    if (!string.IsNullOrWhiteSpace(previousPath))
    if (kind == WindowsWatchKind.Deleted)
    if (kind == WindowsWatchKind.Changed)
    if (kind == WindowsWatchKind.Created)
```


## src/Recall.Core/Checkpoints/CheckpointLease.cs

```csharp
namespace Recall.Core.Checkpoints;
public type CheckpointLease
CheckpointLease
public void Commit()
public void Rollback()
public void Dispose()
if (disposed)
if (disposed)
if (disposed)
if (!committed)
``` 

## src/Recall.Core/Scope/AndroidDocumentPresence.cs

```csharp
namespace Recall.Core.Scope;
public type AndroidDocumentPresence
AndroidDocumentPresence
public static LocalContentState FromFlags(
if (partial || virtualDocument)
if (downloadProgress || downloadError)
if (availableLocally == false)
``` 

## src/Recall.Core/Scope/AppleICloudPresence.cs

```csharp
namespace Recall.Core.Scope;
public type AppleICloudPresence
AppleICloudPresence
public static bool IsStubName(string fileName) =>
public static LocalContentState FromResource(bool stubName, bool? isUbiquitous, string? status)
if (stubName)
if (isUbiquitous is null)
if (!isUbiquitous.Value)
if (string.Equals(status, "Downloaded", StringComparison.Ordinal))
if (string.Equals(status, "Current", StringComparison.Ordinal) || string.Equals(status, "NotDownloaded", StringComparison.Ordinal))
``` 

## src/Recall.Core/Scope/DatasetFolderSkip.cs

```csharp
namespace Recall.Core.Scope;
public type DatasetFolderSkip
DatasetFolderSkip
public static bool ShouldSkip(string folderPath, bool isGrantRoot, IReadOnlyList<string> directChildNames)
public static bool IsMachineMade(string nameWithoutExtension)
if (isGrantRoot)
if (!isMedia)
if (IsMachineMade(Path.GetFileNameWithoutExtension(name)))
if (nameWithoutExtension.Length == 32 && nameWithoutExtension.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F') && !nameWithoutExtension.Contains('-', StringComparison.Ordinal))
if (Guid.TryParse(nameWithoutExtension, out _))
if (nameWithoutExtension.Length >= 12 && nameWithoutExtension.All(static c => c is >= '0' and <= '9'))
``` 

## src/Recall.Core/Scope/DatasetSkipReport.cs

```csharp
namespace Recall.Core.Scope;
public type IDatasetSkipReport
IDatasetSkipReport
public type DatasetSkipReport
DatasetSkipReport
public IReadOnlyList<string> Paths => paths;
public void Add(string folderPath)
public void Clear() => paths.Clear();
if (string.IsNullOrWhiteSpace(folderPath))
if (!paths.Contains(folderPath, StringComparer.OrdinalIgnoreCase))
``` 

## src/Recall.Core/Scope/GrantMembership.cs

```csharp
namespace Recall.Core.Scope;
public type GrantMembership
GrantMembership
public static string? GrantPathFor(
if (documentPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
if (!documentPath.StartsWith(tree, StringComparison.OrdinalIgnoreCase))
if (documentPath.Length <= tree.Length)
``` 

## src/Recall.Core/Scope/LocalContentPolicy.cs

```csharp
namespace Recall.Core.Scope;
public type LocalContentPolicy
LocalContentPolicy
public static bool MayReadBytes(LocalContentState state) => state == LocalContentState.Local;
No branch.
``` 

## src/Recall.Core/Scope/RemoteFileSkipReport.cs

```csharp
namespace Recall.Core.Scope;
public type IRemoteFileSkipReport
IRemoteFileSkipReport
public type RemoteFileSkipReport
RemoteFileSkipReport
public const int ShownCap = 20;
public int SkippedCount => skippedCount;
public IReadOnlyList<string> Paths => shown;
public void Add(string path)
public void Clear()
if (string.IsNullOrWhiteSpace(path))
if (shown.Count < ShownCap && !shown.Contains(path, StringComparer.OrdinalIgnoreCase))
``` 

## src/Recall.Core/Scope/WindowsCloudAttributes.cs

```csharp
namespace Recall.Core.Scope;
public type WindowsCloudAttributes
WindowsCloudAttributes
public const int Offline = 0x1000;
public const int RecallOnOpen = 0x00040000;
public const int RecallOnDataAccess = 0x00400000;
public const int Pinned = 0x00080000;
public const int Unpinned = 0x00100000;
public static LocalContentState FromFindData(bool found, int attributes)
if (!found)
if ((attributes & Offline) != 0 || (attributes & RecallOnOpen) != 0 || (attributes & RecallOnDataAccess) != 0)
```

## src/Recall.Core/Scope/MediaFileKinds.cs

```csharp
namespace Recall.Core.Scope;
public static class MediaFileKinds
public const string AudioAccept = ".mp3,.m4a,.wav,.flac,.ogg,.aac";
public static bool IsAudio(string fileName) => HasExtension(fileName, AudioExtensions);
public static bool IsVideo(string fileName) => HasExtension(fileName, VideoExtensions);
public static bool IsImage(string fileName) => HasExtension(fileName, ImageExtensions);
if (string.IsNullOrEmpty(fileName))
if (extension.Length == 0)
```

Holds the audio, video, and image extension sets plus the shared AudioAccept filter string. Null and blank names are not media.

