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
        CancellationTokenSource cts;
        lock (gate)
        {
            downloadCts?.Cancel();
            downloadCts?.Dispose();
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            downloadCts = cts;
            snapshot = snapshot with { Phase = TextModelDownloadPhase.Downloading, Fault = null };
        }

        var token = cts.Token;
        try
        {
            await DownloadFileAsync(options.OnnxFileName, options.OnnxBaseUri, options.OnnxSha256, progress, token).ConfigureAwait(false);
            await DownloadFileAsync(options.OnnxDataFileName, options.OnnxBaseUri, options.OnnxDataSha256, progress, token).ConfigureAwait(false);
            await DownloadFileAsync(options.TokenizerFileName, options.RepositoryBaseUri, options.TokenizerSha256, progress, token).ConfigureAwait(false);

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
        CancellationToken cancellationToken)
    {
        var targetPath = Path.Combine(modelDirectory, fileName);
        if (await ModelHashVerifier.MatchesExpectedHashAsync(targetPath, expectedSha256, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await DownloadBodyAsync(fileName, baseUri, expectedSha256, progress, cancellationToken, allowRange: true).ConfigureAwait(false);
    }

    private async Task DownloadBodyAsync(
        string fileName,
        Uri baseUri,
        string expectedSha256,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken,
        bool allowRange)
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
                await DownloadBodyAsync(fileName, baseUri, expectedSha256, progress, cancellationToken, allowRange: false).ConfigureAwait(false);
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
                progress?.Report(new ModelDownloadProgress(totalRead, expectedTotal));
                lock (gate)
                {
                    snapshot = snapshot with { BytesReceived = totalRead, Phase = TextModelDownloadPhase.Downloading };
                }
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
