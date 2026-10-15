## src/Recall.Core/Text/TextIngestEngine.cs

```csharp
namespace Recall.Core.Text;
public sealed class TextIngestEngine
    public TextIngestEngine(
        ITextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        IIndexPolicy indexPolicy,
        ITextTokenCounter tokenCounter,
        LocalOpLogWriter opLog)
    public async Task IngestFileAsync(string filePath, string rawText, DateTimeOffset modifiedAt, CancellationToken cancellationToken)
            if (index.TryInsert(insert))
```


## src/Recall.Core/Text/TextChunkPlanner.cs

```csharp
namespace Recall.Core.Text;
public static class TextChunkPlanner
public readonly record struct TextChunkPiece(string PrefixedText, string RawText, int Ordinal);
    public static IReadOnlyList<TextChunkPiece> PlanChunks(
        string title,
        string content,
        Func<string, int> countTokens)
        if (countTokens(whole) <= MaxTokenBudget)
            if (end <= start)
            if (countTokens(wrapped) <= MaxTokenBudget)
        if (index > 0 && index < text.Length && char.IsLowSurrogate(text[index]))
        if (start >= text.Length)
        if (end < text.Length && char.IsHighSurrogate(text[start]) && char.IsLowSurrogate(text[end]))
```


## src/Recall.Core/Text/EmbeddingTaskPrefixes.cs

```csharp
namespace Recall.Core.Text;
public static class EmbeddingTaskPrefixes
    public static string WrapSearchQuery(string query)
    public static string WrapTitledDocument(string title, string content)
        if (query.StartsWith(SearchTask, StringComparison.Ordinal))
            throw new RecallFaultException(RecallFault.SearchPrefixAppliedTwice);
        if (content.StartsWith(TitleMarker, StringComparison.Ordinal) && content.Contains(TextJoiner, StringComparison.Ordinal))
            throw new RecallFaultException(RecallFault.DocumentPrefixAppliedTwice);
        if (safeTitle.Length == 0)
```


## src/Recall.Core/Text/ContentIdBuilder.cs

```csharp
namespace Recall.Core.Text;
public static class ContentIdBuilder
    public static string Build(Modality modality, string sourceContentHash, StoredWidth width, int chunkOrdinal)
    public static string HashSourceContent(string rawText)
        if (chunkOrdinal < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkOrdinal));
```


## src/Recall.Core/Text/GrantTextFiles.cs

```csharp
namespace Recall.Core.Text;
public static class GrantTextFiles
    public static void Use(TextIngestOptions options)
    public static bool IsTextFile(string path)
```

No branch.


## src/Recall.Core/Text/TextFileSignature.cs

```csharp
namespace Recall.Core.Text;
public static class TextFileSignature
    public static bool IsSafeText(ReadOnlySpan<byte> sample)
    public static async Task<bool> FileIsSafeTextAsync(string path, CancellationToken cancellationToken)
        if (sample.IsEmpty || HasBlockedMagic(sample))
        if (HasUtf16Bom(sample))
        if (start.Length >= 3 && start[0] == 0xEF && start[1] == 0xBB && start[2] == 0xBF)
        if (StartsWithAscii(start, "#!") || StartsWithAscii(start, "#requires") || StartsWithAscii(start, "<#"))
            if (value == 0)
            if (value < 9 || (value > 13 && value < 32))
        if (sample.Length < ascii.Length)
            if (sample[i] != ascii[i])
        if (sample.Length >= 2 && sample[0] == (byte)'M' && sample[1] == (byte)'Z')
        if (sample.Length >= 4 && sample[0] == 0x7F && sample[1] == (byte)'E' && sample[2] == (byte)'L' && sample[3] == (byte)'F')
        if (sample.Length >= 4 && IsMachO(sample))
        if (sample.Length >= 4 && sample[0] == 0xCA && sample[1] == 0xFE && sample[2] == 0xBA && sample[3] == 0xBE)
        if (sample.Length >= 4 && sample[0] == (byte)'P' && sample[1] == (byte)'K' && sample[2] == 3 && sample[3] == 4)
        if (sample.Length >= 4 && sample[0] == 0 && sample[1] == (byte)'a' && sample[2] == (byte)'s' && sample[3] == (byte)'m')
```


## src/Recall.Core/Text/TextIngestOptions.cs

```csharp
namespace Recall.Core.Text;
public sealed class TextIngestOptions
    public string[] ExtensionList()
```

No branch.


## src/Recall.Core/Ingest/GrantedTextIngestService.cs

```csharp
namespace Recall.Core.Ingest;
public sealed class GrantedTextIngestService : IGrantedTextIngestService
    public GrantedTextIngestService(IScopeManagerService scope, IGrantedTextReader reader, TextIngestEngine engine)
    public async Task IngestGrantedTextAsync(CancellationToken cancellationToken = default)
            if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Text) && !grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
```


## src/Recall.Core/Ingest/GrantedTextDocument.cs

```csharp
namespace Recall.Core.Ingest;
public sealed record GrantedTextDocument(string Path, string FileName, string Text, DateTimeOffset ModifiedAt);
```

No branch.


## src/Recall.Core/Ingest/DirectoryGrantedTextReader.cs

```csharp
namespace Recall.Core.Ingest;
public sealed class DirectoryGrantedTextReader : IGrantedTextReader
    public DirectoryGrantedTextReader(IUserSkipStore skips)
    public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(
        ScopeFolderGrant grant,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        if (!Directory.Exists(grant.Path))
        if (Directory.Exists(entry))
        if (FileAdmission.IsBlockedFolder(name, user))
        if (IsDirectoryIgnored(relativeDir, shared))
        if (GitIgnoreMatcher.IsIgnored(relativeDir, shared))
        if (GitIgnoreMatcher.LastDecision(relativeDir, true, shared) == GitIgnoreDecision.Ignore
        if (FileAdmission.Decide(name, kinds, user, gitIgnored) != FileAdmissionResult.Read)
        if (!safe)
        if (added > 0)
        if (relative == ".")
        if (File.Exists(ignorePath))
        if (File.Exists(excludePath))
        if (string.IsNullOrEmpty(relativeDir) || relativeDir == ".")
```


## src/Recall.Core/Ingest/IGrantedTextReader.cs

```csharp
namespace Recall.Core.Ingest;
public interface IGrantedTextReader
    IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, CancellationToken cancellationToken);
```

No branch.
