using System.Net.Http.Headers;
using Recall.Core.Download;
using Recall.Core.Faults;
using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed class MauiModelDownloadService : IModelDownloadService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IGrantedTextIngestService ingest;
    private readonly TextModelDownloadOptions options;
    private readonly string modelDirectory;
    private readonly object gate = new();
    private CancellationTokenSource? downloadCts;
    private int lastPublishedPercent = -1;
    private TextModelDownloadSnapshot snapshot = new(TextModelDownloadPhase.NotStarted, 0, null);

    public MauiModelDownloadService(
        IHttpClientFactory httpClientFactory,
        IGrantedTextIngestService ingest,
        TextModelDownloadOptions options)
    {
        this.httpClientFactory = httpClientFactory;
        this.ingest = ingest;
        this.options = options;
        modelDirectory = Path.Combine(FileSystem.AppDataDirectory, options.RelativeModelDirectory);
        Directory.CreateDirectory(modelDirectory);
    }

    public TextModelDownloadSnapshot GetTextDownloadSnapshot()
    {
        lock (gate)
        {
            return snapshot;
        }
    }

    public Task DownloadTextModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    {
        return RunDownloadAsync(progress, cancellationToken);
    }

    public void CancelTextDownload()
    {
        lock (gate)
        {
            downloadCts?.Cancel();
            snapshot = snapshot with { Phase = TextModelDownloadPhase.Cancelled };
        }
    }

    public Task RetryTextDownloadAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    {
        return RunDownloadAsync(progress, cancellationToken);
    }

    private async Task RunDownloadAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken)
    {
        var files = new (string Name, Uri BaseUri, string Sha256)[]
        {
            (options.OnnxFileName, options.OnnxBaseUri, options.OnnxSha256),
            (options.OnnxDataFileName, options.OnnxBaseUri, options.OnnxDataSha256),
            (options.TokenizerFileName, options.RepositoryBaseUri, options.TokenizerSha256),
        };
        CancellationTokenSource cts;
        lock (gate)
        {
            downloadCts?.Cancel();
            downloadCts?.Dispose();
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            downloadCts = cts;
            lastPublishedPercent = -1;
            snapshot = snapshot with { Phase = TextModelDownloadPhase.Downloading, Fault = null, CompletedFiles = 0, FileCount = files.Length };
        }

        var token = cts.Token;
        try
        {
            for (var index = 0; index < files.Length; index++)
            {
                var file = files[index];
                await DownloadFileAsync(file.Name, file.BaseUri, file.Sha256, progress, token, index, files.Length).ConfigureAwait(false);
            }

            lock (gate)
            {
                snapshot = snapshot with { Phase = TextModelDownloadPhase.Ready, Fault = null };
            }

            await ingest.IngestGrantedTextAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            lock (gate)
            {
                snapshot = snapshot with { Phase = TextModelDownloadPhase.Cancelled };
            }
        }
        catch (RecallFaultException fault) when (fault.Fault == RecallFault.DownloadHashMismatch)
        {
            lock (gate)
            {
                snapshot = snapshot with { Phase = TextModelDownloadPhase.HashFailed, Fault = TextModelDownloadFault.HashMismatch };
            }
        }
        catch (RecallFaultException fault) when (fault.Fault == RecallFault.DownloadIncomplete)
        {
            lock (gate)
            {
                snapshot = snapshot with { Phase = TextModelDownloadPhase.Interrupted, Fault = TextModelDownloadFault.Incomplete };
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            lock (gate)
            {
                snapshot = snapshot with { Phase = TextModelDownloadPhase.Interrupted, Fault = TextModelDownloadFault.Transport };
            }
        }
    }

    private async Task DownloadFileAsync(
        string fileName,
        Uri baseUri,
        string expectedSha256,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken,
        int completedFiles,
        int fileCount)
    {
        var targetPath = Path.Combine(modelDirectory, fileName);
        if (await ModelHashVerifier.MatchesExpectedHashAsync(targetPath, expectedSha256, cancellationToken).ConfigureAwait(false))
        {
            PublishProgress(progress, completedFiles + 1, fileCount, 0, null);
            return;
        }

        await DownloadBodyAsync(fileName, baseUri, expectedSha256, progress, cancellationToken, allowRange: true, completedFiles, fileCount).ConfigureAwait(false);
        PublishProgress(progress, completedFiles + 1, fileCount, 0, null);
    }

    private async Task DownloadBodyAsync(
        string fileName,
        Uri baseUri,
        string expectedSha256,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken,
        bool allowRange,
        int completedFiles,
        int fileCount)
    {
        var targetPath = Path.Combine(modelDirectory, fileName);
        var partialPath = targetPath + ".partial";
        var uri = ModelWeightDownloadRequestBuilder.BuildRequestUri(fileName, baseUri);
        var existing = allowRange && File.Exists(partialPath) ? new FileInfo(partialPath).Length : 0L;
        if (!allowRange)
        {
            TryDelete(partialPath);
            existing = 0;
        }

        var client = httpClientFactory.CreateClient(nameof(MauiModelDownloadService));
        if (client.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(options.UserAgentProduct, options.UserAgentVersion));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (existing > 0)
        {
            request.Headers.Range = new RangeHeaderValue(existing, null);
        }

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var status = (int)response.StatusCode;
        if (status == DownloadTransfer.StatusRangeNotSatisfiable)
        {
            if (await ModelHashVerifier.MatchesExpectedHashAsync(partialPath, expectedSha256, cancellationToken).ConfigureAwait(false))
            {
                MovePartial(partialPath, targetPath);
                return;
            }

            TryDelete(partialPath);
            if (allowRange)
            {
                await DownloadBodyAsync(fileName, baseUri, expectedSha256, progress, cancellationToken, allowRange: false, completedFiles, fileCount).ConfigureAwait(false);
                return;
            }

            throw new RecallFaultException(RecallFault.DownloadIncomplete);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException();
        }

        var placement = DownloadTransfer.Place(status, existing);
        var expectedTotal = DownloadTransfer.ExpectedTotalBytes(status, existing, response.Content.Headers.ContentLength);
        var append = placement == DownloadBodyPlacement.Append;
        await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        await using (var file = new FileStream(
            partialPath,
            append ? FileMode.OpenOrCreate : FileMode.Create,
            FileAccess.Write,
            FileShare.None))
        {
            if (append)
            {
                file.Seek(existing, SeekOrigin.Begin);
            }

            var buffer = new byte[81920];
            long totalRead = append ? existing : 0;
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                totalRead += read;
                PublishProgress(progress, completedFiles, fileCount, totalRead, expectedTotal);
            }

            await file.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (!DownloadTransfer.ReceivedFullBody(totalRead, expectedTotal))
            {
                throw new RecallFaultException(RecallFault.DownloadIncomplete);
            }
        }

        if (!await ModelHashVerifier.MatchesExpectedHashAsync(partialPath, expectedSha256, cancellationToken).ConfigureAwait(false))
        {
            TryDelete(partialPath);
            throw new RecallFaultException(RecallFault.DownloadHashMismatch);
        }

        MovePartial(partialPath, targetPath);
    }

    private void PublishProgress(
        IProgress<ModelDownloadProgress>? progress,
        int completedFiles,
        int fileCount,
        long bytesReceived,
        long? totalBytes)
    {
        var percent = TextModelDownloadUiPolicy.ComputeOverallPercent(completedFiles, fileCount, bytesReceived, totalBytes);
        lock (gate)
        {
            snapshot = snapshot with
            {
                Phase = TextModelDownloadPhase.Downloading,
                BytesReceived = bytesReceived,
                CompletedFiles = completedFiles,
                FileCount = fileCount,
                CurrentFileTotal = totalBytes,
                Fault = null,
            };
        }

        if (!TextModelDownloadUiPolicy.ShouldPublishProgress(lastPublishedPercent, percent))
        {
            return;
        }

        lastPublishedPercent = percent;
        progress?.Report(new ModelDownloadProgress(bytesReceived, totalBytes, completedFiles, fileCount));
    }

    private static void MovePartial(string partialPath, string targetPath)
    {
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        File.Move(partialPath, targetPath);
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
