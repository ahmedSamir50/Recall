## src/Recall.Core/Search/TextSearchService.cs

```csharp
namespace Recall.Core.Search;
public sealed class TextSearchService : ITextSearchService
    public TextSearchService(ITextEmbeddingEncoder encoder, ITextVectorIndex index, IIndexPolicy indexPolicy)
    public async Task<IReadOnlyList<TextSearchHit>> FindAsync(
        string query,
        IReadOnlyList<ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default)
        if (grantedPaths.Count == 0)
```


## src/Recall.Core/Search/TextSearchModels.cs

```csharp
namespace Recall.Core.Search;
public sealed record TextIndexInsert(
public sealed record TextSearchRequest(
public sealed record TextSearchHit(
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
