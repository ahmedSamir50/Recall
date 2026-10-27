## src/Recall.Core/Download/DownloadTransfer.cs

```csharp
namespace Recall.Core.Download;
public enum DownloadBodyPlacement
public static class DownloadTransfer
    public static DownloadBodyPlacement Place(int statusCode, long existingPartialBytes)
    public static long? ExpectedTotalBytes(int statusCode, long existingPartialBytes, long? responseContentLength)
    public static bool ReceivedFullBody(long bytesOnDisk, long? expectedTotalBytes)
        if (existingPartialBytes <= 0)
        if (statusCode == StatusPartialContent)
        if (statusCode == StatusRangeNotSatisfiable)
        if (statusCode == StatusRangeNotSatisfiable)
        if (statusCode == StatusPartialContent)
        if (expectedTotalBytes is null)
```


## src/Recall.Core/Download/TextModelDownloadUiPolicy.cs

```csharp
namespace Recall.Core.Download;
public static class TextModelDownloadUiPolicy
    public static bool ShouldAutoStartDownload(TextModelDownloadPhase phase) =>
    public static bool ShouldOfferRetry(TextModelDownloadPhase phase) =>
    public static bool ShouldPublishProgress(int lastPublishedPercent, int nextPercent) =>
    public static TextModelDownloadSnapshot MarkVerifying(TextModelDownloadSnapshot snapshot) =>
    public static int ComputeOverallPercent(int completedFiles, int fileCount, long bytesReceived, long? totalBytes)
    public static int ComputePercent(long bytesReceived, long? totalBytes)
        if (fileCount <= 0)
        if (completedFiles < 0)
        if (completedFiles > fileCount)
        if (overall <= 0)
        if (overall >= 1)
        if (totalBytes is null or <= 0)
        if (ratio <= 0)
        if (ratio >= 1)
```


```csharp
ModelDownloadStart
DownloadSnapshotSlot
if (ocr)
if (vision)
```

## src/Recall.Core/Download/TextModelDownloadOptions.cs

```csharp
namespace Recall.Core.Download;
public sealed class TextModelDownloadOptions
```

No branch.


## src/Recall.Core/Download/ModelDownloadModels.cs

```csharp
namespace Recall.Core.Download;
public enum TextModelDownloadPhase
public sealed record ModelDownloadProgress(
public sealed record TextModelDownloadSnapshot(
public enum TextModelDownloadFault
public static class ModelWeightDownloadRequestBuilder
    public static Uri BuildRequestUri(string publicFileName, Uri baseUri)
    public static bool ContainsForbiddenPayload(
        string? documentName,
        string? documentText,
        ReadOnlyMemory<float>? vector,
        string? deviceId,
        string? nativeDeviceName,
        string? customName) =>
```

No branch.


## src/Recall.Core/Download/ModelHashVerifier.cs

```csharp
namespace Recall.Core.Download;
public static class ModelHashVerifier
    public static async Task<string> ComputeSha256HexAsync(string path, CancellationToken cancellationToken = default)
    public static async Task<bool> MatchesExpectedHashAsync(
        string path,
        string expectedSha256HexLower,
        CancellationToken cancellationToken = default)
        if (!File.Exists(path))
```


## src/Recall.Core/Ports/DownloadPorts.cs

```csharp
namespace Recall.Core.Ports;
public interface IModelDownloadService
public interface ISecureStorageService
    TextModelDownloadSnapshot GetTextDownloadSnapshot();
    Task DownloadTextModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default);
    void CancelTextDownload();
    Task RetryTextDownloadAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default);
```

No branch.


## src/Recall.Core/Catalog/ModelCatalog.cs

```csharp
namespace Recall.Core.Catalog;
public sealed class ModelCatalogOptions
public static class ModelCatalog
public sealed record ModelCatalogEntry(Modality Modality, string ModelId);
    public static void Use(ModelCatalogOptions options)
```

No branch.


## src/Recall.Core/Download/DefaultEncoderFileHash.cs

```csharp
namespace Recall.Core.Download;
public type DefaultEncoderFileHash
DefaultEncoderFileHash
No branch.
``` 

## src/Recall.Core/Download/TextModelReadyIngest.cs

```csharp
namespace Recall.Core.Download;
public type TextModelReadyIngest
TextModelReadyIngest
public static async Task ReadGrantsAsync(
if (phase != TextModelDownloadPhase.Ready)
``` 

## src/Recall.Core/Download/VisionModelDownloadOptions.cs

```csharp
namespace Recall.Core.Download;
public sealed class VisionModelDownloadOptions
public const string SectionName = "VisionModel";
public string ModelBaseUrl { get; init; }
public string ModelFileName { get; init; }
public string ModelSha256 { get; init; }
public string RelativeModelDirectory { get; init; }
```

Defaults pin the mmproj file `mmproj-embeddinggemma-2-Q8_0.gguf`, its Hub SHA-256, the `ggml-org/embeddinggemma-2-GGUF` resolve base, and `models/vision`. No branch.

## src/Recall.Core/Download/AudioModelDownloadOptions.cs

```csharp
namespace Recall.Core.Download;
public sealed class AudioModelDownloadOptions
public const string SectionName = "AudioModel";
public string ModelBaseUrl { get; set; } = "https://huggingface.co/ggml-org/embeddinggemma-2-GGUF/resolve/main/";
public string ModelFileName { get; set; } = "mmproj-embeddinggemma-2-Q8_0.gguf";
public string ModelSha256 { get; set; } = "c4a8a52691ecef40618438928bdf9e68379b854e24166f292592353db0aab64f";
public string RelativeModelDirectory { get; set; } = "models/vision";
public Uri ModelBaseUri => new(ModelBaseUrl);
```

## src/Recall.Core/Download/AudioDownloadJoin.cs

```csharp
namespace Recall.Core.Download;
public enum AudioDownloadJoin
public static class AudioDownloadJoinPolicy
public static AudioDownloadJoin Decide(TextModelDownloadPhase visionPhase, bool fileReady)
if (fileReady || visionPhase == TextModelDownloadPhase.Ready)
if (visionPhase is TextModelDownloadPhase.Downloading or TextModelDownloadPhase.Verifying)
```

## src/Recall.Core/Download/ModelDownloadRowKind.cs

```csharp
public enum ModelDownloadRowKind
public static class ModelDownloadRowKindExtensions
```

## src/Recall.Core/Download/WindowIngestPolicy.cs

```csharp
public static class WindowIngestPolicy
```

Window ingest starts only after the text model download phase is ready.
