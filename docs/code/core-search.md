## src/Recall.Core/Search/TextSearchService.cs

```csharp
namespace Recall.Core.Search;
public sealed class TextSearchService : ITextSearchService
    public TextSearchService(
        ITextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        IIndexPolicy indexPolicy,
        IIngestCursorStore cursors,
        IEncoderFileHash encoderHash,
        IMediaVectorIndex media,
        IVisionEmbeddingEncoder? visionEncoder = null)
    public async Task<IReadOnlyList<TextSearchHit>> FindAsync(
    internal IReadOnlyList<TextSearchHit> SearchMediaWords(
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
if (grantedPaths.Count == 0 && mediaGrantPaths.Count == 0)
if (grantedPaths.Count > 0)
if (tryMedia)
if (!present.Add(hit.SourcePath))
if (mediaFilter.Length == 0)
if (info.Exists)
catch (UnauthorizedAccessException)
catch (IOException)
``` 
```csharp
if (fileBudget <= 0)
if (!best.TryGetValue(hit.SourcePath, out var existing) || PreferMedia(hit, existing))
if (winners.Count > fileBudget)
if (candidate.Score > current.Score)
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


## src/Recall.Core/Search/Bm25OnlyRank.cs

```csharp
namespace Recall.Core.Search;
public static class Bm25OnlyRank
    public static int Count(string rawText, string query)
    public static IReadOnlyList<TextSearchHit> Merge(
        IReadOnlyList<TextSearchHit> ftsHits,
        IReadOnlyList<Bm25OnlyRow> rows,
        string query,
        IReadOnlyList<ScopeFolderGrant> grants)
        if (overlap == 0)
        if (grantPath is null)
        if (tokens.Count > 0)
        if (Rune.IsWhiteSpace(rune))
        if (builder.Length > 0)
        if (used[i])
        if (string.Equals(rawPieces[i], piece, StringComparison.Ordinal))
        if (byScore != 0)
        if (byId != 0)
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
public const string Audio = "items_audio";
public const string ImageOld = "items_image_old";
public const string VideoOld = "items_video_old";
public const string AudioOld = "items_audio_old";
public const int Width = 768;
public const int OldWidth = 512;
public static readonly string[] PictureCollections = [Image, Video, ImageOld, VideoOld];
public static readonly string[] AudioCollections = [Audio, AudioOld];
public static readonly string[] WordCollections = [Image, Video, Audio, ImageOld, VideoOld, AudioOld];
public static bool IsMedia(string collectionName) =>
public static int WidthFor(string collectionName)
public static string EncoderHashFor(string collectionName, IEncoderFileHash hashes)
```

Long collections are 768 wide; `_old` partners are 512. A text collection name is rejected. `if (string.Equals(collectionName, Image, StringComparison.Ordinal)` selects long widths. `if (string.Equals(collectionName, ImageOld, StringComparison.Ordinal)` selects 512 for `_old` names. `if (string.Equals(collectionName, Audio, StringComparison.Ordinal)` selects the audio encoder hash.

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

## src/Recall.Core/Search/MediaHitStart.cs

```csharp
namespace Recall.Core.Search;
public static class MediaHitStart
    public static double Seconds(TextSearchHit hit)
    public static double Seconds(MediaSearchHit hit)
    public static bool Seeks(double startSeconds) => startSeconds > 0;
```

`Seconds` reads the start seconds off a text or media hit and throws on a null hit. `Seeks` is true only above zero, so a start of 0 keeps the launcher. No branch.

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

Empty bytes and a null grant list return no hits. No image- or video-enabled grant returns no hits without embedding. Otherwise the JPEG is embedded once, each of `PictureCollections` is searched with the 768 query or a 512 cut, rows collapse per path with `Collapse.EarliestStart`, lists fuse with `AgeRrf.Fuse`, then take the file cap. No text collection is queried.


## src/Recall.Core/Search/AudioSearchService.cs

```csharp
namespace Recall.Core.Search;
public sealed class AudioSearchService : IAudioSearchService
public async Task<IReadOnlyList<MediaSearchHit>> FindAsync(
    ReadOnlyMemory<float> mono16k,
    IReadOnlyList<ScopeFolderGrant> grants,
    CancellationToken cancellationToken = default)
if (mono16k.IsEmpty || grants is null)
if (grantedPaths.Count == 0)
if (filter.Length == 0)
```

Empty PCM or no audio grant returns no hits. Otherwise PCM is embedded once, long and `_old` audio collections are searched with the matching query width, paths outside the grant drop, per-path collapse runs, `AgeRrf.Fuse` merges, then the file cap applies.

## src/Recall.Core/Search/AgeRrf.cs

```csharp
namespace Recall.Core.Search;
public static class AgeRrf
public static IReadOnlyList<MediaSearchHit> Fuse(IReadOnlyList<IReadOnlyList<MediaSearchHit>> lists)
if (list.Count == 0)
if (!seen.Add(hit.SourcePath))
if (scores.TryGetValue(hit.SourcePath, out var existing))
if (IsBetterRepresentative(hit, rank, listIndex, existing.Hit, existing.BestRank, existing.ListIndex))
if (scores.Count == 0)
if (candidateRank < currentRank)
if (candidateRank > currentRank)
if (candidate.StartSeconds < current.StartSeconds)
if (candidate.StartSeconds > current.StartSeconds)
```

Fuses multiple ranked lists with the same unweighted RRF term `1 / (HybridFusion.RrfRankConstant + rank + 1)`. One hit per path within each list; representative tie-break uses lowest rank, then smaller `StartSeconds`, then earlier list index; final order is score descending, then start, then path.

