## src/Recall.Core/Search/TextSearchService.cs

```csharp
namespace Recall.Core.Search;
public sealed class TextSearchService : ITextSearchService
    public TextSearchService(ITextEmbeddingEncoder encoder, ITextVectorIndex index, IIndexPolicy indexPolicy)
    public async Task<IReadOnlyList<TextSearchHit>> FindAsync(
        string query,
        IReadOnlyList<ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default)
        if (string.IsNullOrWhiteSpace(query) || grants is null)
        if (grantedPaths.Count == 0)
```



```csharp
if (cursors.PendingCount() > 0 && cursors.TryEnterWrite(out var lease) && lease is not null)
if (pending.Count > 0)
if (useInDbRrf)
if (!grantedPaths.Contains(expanded.GrantPath))
if (!byPath.TryGetValue(expanded.SourcePath, out var list))
if (applied is not null)
``` 
## src/Recall.Core/Search/TextSearchModels.cs

```csharp
namespace Recall.Core.Search;
public type TextIndexInsert
TextIndexInsert
public type TextSearchRequest
TextSearchRequest
public type TextSearchHit
TextSearchHit
public type VectorLiveState
VectorLiveState
public sealed record TextIndexInsert(
public sealed record TextSearchRequest(
public sealed record TextSearchHit(
public sealed record VectorLiveState(string ContentId, bool PathLive, string EncoderHash, string GrantPath);
```

No branch.


## src/Recall.Core/Search/TextMatchReason.cs

```csharp
namespace Recall.Core.Search;
public enum TextMatchReason
```

No branch.


## src/Recall.Core/Search/TextVectorIndexGuard.cs

```csharp
namespace Recall.Core.Search;
public static class TextVectorIndexGuard
    public static void EnsureHybridQueryUsesRrf(int rankConstant, IIndexPolicy policy)
```

No branch.


## src/Recall.Core/Search/HybridFusion.cs

```csharp
namespace Recall.Core.Search;
public static class HybridFusion
    public static void RequireRankConstant(int rankConstant)
        if (rankConstant != RrfRankConstant)
            throw new RecallFaultException(RecallFault.RrfRankConstant);
        if (scores.TryGetValue(hit.ContentId, out var existing))
```


## src/Recall.Core/Search/LocalFileOpenPolicy.cs

```csharp
namespace Recall.Core.Search;
public enum LocalFileProbe
public enum LocalFileOpenResult
public static class LocalFileOpenPolicy
    public static LocalFileProbe Classify(string? sourcePath)
    public static LocalFileOpenResult Decide(LocalFileProbe probe, bool filesystemExists, bool launcherOpened, bool platformAccepted, bool threw)
    public static bool Exists(LocalFileProbe probe, bool filesystemExists, bool contentStreamOpened)
        if (string.IsNullOrWhiteSpace(sourcePath))
        if (sourcePath.StartsWith(ContentUri.Scheme, StringComparison.OrdinalIgnoreCase))
        if (threw)
        if (probe == LocalFileProbe.Empty)
        if (probe == LocalFileProbe.ContentUri)
        if (!filesystemExists)
        if (probe == LocalFileProbe.Empty)
        if (probe == LocalFileProbe.ContentUri)
```


## src/Recall.Core/Ports/IOpenIndexedFileService.cs

```csharp
namespace Recall.Core.Ports;
public interface IOpenIndexedFileService
    Task<bool> TryOpenAsync(string sourcePath, CancellationToken cancellationToken = default);
```

No branch.


## src/Recall.Core/Search/SearchHitMarks.cs

```csharp
namespace Recall.Core.Search;
public type SearchHitMarks
SearchHitMarks
public static class SearchHitMarks
public static TextSearchHit? Apply(
    if (chunk is null)
    if (chunk.Retired)
    if (!grantPaths.Contains(hit.GrantPath))
    if (alias.Missing || alias.Retired)
```


## src/Recall.Core/Indexing/RecallIndexCaps.cs

```csharp
namespace Recall.Core.Indexing;
public type RecallIndexCaps
RecallIndexCaps
public const int RawListCap = 64;
public const int FileCap = 20;
No branch.
``` 

## src/Recall.Core/Search/Collapse.cs

```csharp
namespace Recall.Core.Search;
public type Collapse
Collapse
public static TextSearchHit MaxRrf(IReadOnlyList<TextSearchHit> hits)
if (hits.Count == 0)
if (candidate.Score > best.Score)
if (Math.Abs(candidate.Score - best.Score) < 1e-12 && candidate.Ordinal < best.Ordinal)
``` 

## src/Recall.Core/Search/TextSearchFilter.cs

```csharp
namespace Recall.Core.Search;
public type TextSearchFilter
TextSearchFilter
public const string PathLiveField = "path_live";
public const string EncoderHashField = "encoder_hash";
public const string GrantPathField = "grant_path";
public static string Build(IReadOnlySet<string> grantPaths, string encoderHash)
if (grantPaths.Count == 0)
``` 

