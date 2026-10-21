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
public static MediaSearchHit EarliestStart(IReadOnlyList<MediaSearchHit> hits)
if (hits.Count == 0)
if (candidate.Score > best.Score)
if (Math.Abs(candidate.Score - best.Score) < 1e-12 && candidate.Ordinal < best.Ordinal)
if (Math.Abs(candidate.Score - best.Score) < 1e-12 && candidate.StartSeconds < best.StartSeconds)
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

## src/Recall.Core/Indexing/MediaCollections.cs

```csharp
namespace Recall.Core.Indexing;
public static class MediaCollections
public const string Image = "items_image";
public const string Video = "items_video";
public const int Width = 768;
public static bool IsMedia(string collectionName) =>
```

Picture rows live in `items_image` and video frame rows live in `items_video`. Both are 768 wide. A text collection name is rejected. No branch.

## src/Recall.Core/Indexing/MediaIndexInsert.cs

```csharp
namespace Recall.Core.Indexing;
public sealed record MediaIndexInsert(
    string ContentId,
    string SourcePath,
    string FileName,
    string Words,
    ReadOnlyMemory<float> Vector768,
    string EncoderHash,
    bool PathLive,
    string GrantPath,
    string StartSeconds,
    string EndSeconds);
```

Seconds are stored as invariant strings so a picture row can leave them unset and parse back to zero. No branch.

## src/Recall.Core/Search/MediaSearchHit.cs

```csharp
namespace Recall.Core.Search;
public sealed record MediaSearchHit(
    string ContentId,
    string SourcePath,
    double StartSeconds,
    double EndSeconds,
    double Score);
public sealed record MediaSearchRequest(
    string CollectionName,
    ReadOnlyMemory<float> QueryVector768,
    string Filter,
    int TopK);
```

`MediaSearchHit` is the picture or frame card. `MediaSearchRequest` names one media collection, one 768-wide query vector, a grant filter, and a top-k. No branch.

## src/Recall.Core/Search/PictureQueryGate.cs

```csharp
namespace Recall.Core.Search;
public enum PictureQueryDecision
public static class PictureQueryGate
public static PictureQueryDecision Decide(TextModelDownloadPhase phase) => phase switch
```

Not started, downloading, and verifying all map to `Wait`. Ready maps to `Search`. Every other phase maps to `Fail`. No branch statement is used; the switch expression is exhaustive over the four phases plus a fallback.

## src/Recall.Core/Search/PictureSearchService.cs

```csharp
namespace Recall.Core.Search;
public sealed class PictureSearchService : IPictureSearchService
public Task<IReadOnlyList<MediaSearchHit>> FindAsync(
if (jpeg.IsEmpty || grants is null)
if (grantedPaths.Count == 0)
if (filter.Length == 0)
```

Empty bytes and a null grant list return no hits. No image- or video-enabled grant returns no hits without embedding. Otherwise the JPEG is embedded once, the image and video collections are searched with a text-filter build, and rows outside the enabled grants are dropped. Rows collapse per path with `Collapse.EarliestStart`, then take the file cap. No text collection is queried.

