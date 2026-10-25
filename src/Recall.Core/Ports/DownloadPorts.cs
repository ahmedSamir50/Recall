using Recall.Core.Download;

namespace Recall.Core.Ports;

public interface IModelDownloadService
{
    TextModelDownloadSnapshot GetTextDownloadSnapshot();

    Task DownloadTextModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default);

    void CancelTextDownload();

    Task RetryTextDownloadAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default);

    Task DownloadVisionModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default);

    TextModelDownloadSnapshot GetVisionDownloadSnapshot();

    TextModelDownloadSnapshot GetAudioDownloadSnapshot();

    Task DownloadAudioModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default);

    Task DownloadOcrModelsAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default);
}

public interface ISecureStorageService
{
}
