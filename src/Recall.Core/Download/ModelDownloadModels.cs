namespace Recall.Core.Download;

public enum TextModelDownloadPhase
{
    NotStarted = 0,
    Downloading = 1,
    Verifying = 2,
    Ready = 3,
    HashFailed = 4,
    Cancelled = 5,
    Interrupted = 6,
}

public sealed record ModelDownloadProgress(long BytesReceived, long? TotalBytes);

public sealed record TextModelDownloadSnapshot(
    TextModelDownloadPhase Phase,
    long BytesReceived,
    TextModelDownloadFault? Fault);

public enum TextModelDownloadFault
{
    HashMismatch = 1,
    Incomplete = 2,
    Transport = 3,
}

public static class ModelWeightDownloadRequestBuilder
{
    public static Uri BuildRequestUri(string publicFileName, Uri baseUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicFileName);
        ArgumentNullException.ThrowIfNull(baseUri);
        return new Uri(baseUri, publicFileName);
    }

    public static bool ContainsForbiddenPayload(
        string? documentName,
        string? documentText,
        ReadOnlyMemory<float>? vector,
        string? deviceId,
        string? nativeDeviceName,
        string? customName) =>
        !string.IsNullOrEmpty(documentName)
        || !string.IsNullOrEmpty(documentText)
        || vector is not null
        || !string.IsNullOrEmpty(deviceId)
        || !string.IsNullOrEmpty(nativeDeviceName)
        || !string.IsNullOrEmpty(customName);
}
