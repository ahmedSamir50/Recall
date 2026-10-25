using Recall.Core.Download;
using Recall.Core.Faults;
using Recall.Core.Ocr;
using Recall.Core.Ports;
using System.Net.Http.Headers;

namespace Recall.Maui.Platform;

public sealed class MauiModelDownloadService : IModelDownloadService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IGrantedTextIngestService ingest;
    private readonly TextModelDownloadOptions options;
    private readonly VisionModelDownloadOptions visionOptions;
    private readonly AudioModelDownloadOptions audioOptions;
    private readonly string modelDirectory;
    private readonly string visionDirectory;
    private readonly string ocrDirectory;
    private readonly object gate = new();
    private CancellationTokenSource? downloadCts;
    private CancellationTokenSource? visionDownloadCts;
    private Task? visionDownloadInFlight;
    private int lastPublishedPercent = -1;
    private int lastPublishedVisionPercent = -1;
    private TextModelDownloadSnapshot snapshot = new(TextModelDownloadPhase.NotStarted, 0, null);
    private TextModelDownloadSnapshot visionSnapshot = new(TextModelDownloadPhase.NotStarted, 0, null);
    private TextModelDownloadSnapshot audioSnapshot = new(TextModelDownloadPhase.NotStarted, 0, null);

    public MauiModelDownloadService(
        IHttpClientFactory httpClientFactory,
        IGrantedTextIngestService ingest,
        TextModelDownloadOptions options,
        VisionModelDownloadOptions visionOptions)
        : this(httpClientFactory, ingest, options, visionOptions, new AudioModelDownloadOptions())
    {
    }

    public MauiModelDownloadService(
        IHttpClientFactory httpClientFactory,
        IGrantedTextIngestService ingest,
        TextModelDownloadOptions options,
        VisionModelDownloadOptions visionOptions,
        AudioModelDownloadOptions audioOptions)
    {
        this.httpClientFactory = httpClientFactory;
        this.ingest = ingest;
        this.options = options;
        this.visionOptions = visionOptions;
        this.audioOptions = audioOptions;
        modelDirectory = Path.Combine(FileSystem.AppDataDirectory, options.RelativeModelDirectory);
        visionDirectory = Path.Combine(FileSystem.AppDataDirectory, visionOptions.RelativeModelDirectory);
        ocrDirectory = Path.Combine(FileSystem.AppDataDirectory, OcrModelFiles.Directory);
        Directory.CreateDirectory(modelDirectory);
        Directory.CreateDirectory(visionDirectory);
        Directory.CreateDirectory(ocrDirectory);
    }

    public Task DownloadOcrModelsAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return DownloadOcrFilesAsync(progress, cancellationToken);
    }

    private async Task DownloadOcrFilesAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken)
    {
        (string LocalName, string RemoteName, string Url, string Sha256)[] files =
        [
            (OcrModelFiles.Detector.LocalName, OcrModelFiles.Detector.RemoteName, OcrModelFiles.Detector.Url, OcrModelFiles.Detector.Sha256),
            (OcrModelFiles.EnglishReader.LocalName, OcrModelFiles.EnglishReader.RemoteName, OcrModelFiles.EnglishReader.Url, OcrModelFiles.EnglishReader.Sha256),
            (OcrModelFiles.ArabicReader.LocalName, OcrModelFiles.ArabicReader.RemoteName, OcrModelFiles.ArabicReader.Url, OcrModelFiles.ArabicReader.Sha256),
        ];
        for (var index = 0; index < files.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = files[index];
            await DownloadOcrFileAsync(file.Item1, file.Item2, file.Item3, file.Item4, progress, cancellationToken, index, files.Length).ConfigureAwait(false);
        }
    }

    private async Task DownloadOcrFileAsync(
        string localName,
        string remoteName,
        string baseUrl,
        string expectedSha256,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken,
        int completedFiles,
        int fileCount)
    {
        var targetPath = Path.Combine(ocrDirectory, localName);
        if (await ModelHashVerifier.MatchesExpectedHashAsync(targetPath, expectedSha256, cancellationToken).ConfigureAwait(false))
        {
            PublishProgress(progress, vision: false, completedFiles + 1, fileCount, 0, null);
            return;
        }

        await DownloadBodyAsync(
            remoteName,
            new Uri(baseUrl),
            expectedSha256,
            ocrDirectory,
            vision: false,
            progress,
            cancellationToken,
            allowRange: true,
            completedFiles,
            fileCount).ConfigureAwait(false);
        var downloadedPath = Path.Combine(ocrDirectory, remoteName);
        if (!string.Equals(localName, remoteName, StringComparison.Ordinal) && File.Exists(downloadedPath))
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(downloadedPath, targetPath);
        }
    }

    public TextModelDownloadSnapshot GetTextDownloadSnapshot()
    {
        lock (gate)
        {
            return snapshot;
        }
    }

    public TextModelDownloadSnapshot GetVisionDownloadSnapshot()
    {
        lock (gate)
        {
            return visionSnapshot;
        }
    }

    public TextModelDownloadSnapshot GetAudioDownloadSnapshot()
    {
        lock (gate)
        {
            return audioSnapshot;
        }
    }

    public Task DownloadTextModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    {
        return RunDownloadAsync(
            [(options.ModelFileName, options.ModelBaseUri, options.ModelSha256)],
            modelDirectory,
            vision: false,
            ingestAfter: true,
            progress,
            cancellationToken);
    }

    public async Task DownloadVisionModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    {
        Task downloadTask;
        lock (gate)
        {
            if (visionDownloadInFlight is not null)
            {
                downloadTask = visionDownloadInFlight;
            }
            else
            {
                downloadTask = RunDownloadAsync(
                    [(visionOptions.ModelFileName, visionOptions.ModelBaseUri, visionOptions.ModelSha256)],
                    visionDirectory,
                    vision: true,
                    ingestAfter: false,
                    progress,
                    cancellationToken);
                visionDownloadInFlight = downloadTask;
            }
        }

        try
        {
            await downloadTask.ConfigureAwait(false);
        }
        finally
        {
            lock (gate)
            {
                if (visionDownloadInFlight == downloadTask)
                {
                    visionDownloadInFlight = null;
                }
            }
        }
    }

    public async Task DownloadAudioModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    {
        var fileReady = await ModelHashVerifier.MatchesExpectedHashAsync(
            Path.Combine(visionDirectory, audioOptions.ModelFileName),
            audioOptions.ModelSha256,
            cancellationToken).ConfigureAwait(false);

        TextModelDownloadSnapshot visionPhaseSnapshot;
        lock (gate)
        {
            visionPhaseSnapshot = visionSnapshot;
        }

        var decision = AudioDownloadJoinPolicy.Decide(visionPhaseSnapshot.Phase, fileReady);
        switch (decision)
        {
            case AudioDownloadJoin.UseReady:
                SetAudioSnapshot(current => current with { Phase = TextModelDownloadPhase.Ready, Fault = null });
                return;
            case AudioDownloadJoin.WaitForCurrent:
                await AwaitVisionInFlightAsync().ConfigureAwait(false);
                break;
            default:
                await DownloadVisionModelAsync(progress, cancellationToken).ConfigureAwait(false);
                break;
        }

        CopyVisionSnapshotToAudio();
    }

    private async Task AwaitVisionInFlightAsync()
    {
        Task? inFlight;
        lock (gate)
        {
            inFlight = visionDownloadInFlight;
        }

        if (inFlight is not null)
        {
            await inFlight.ConfigureAwait(false);
        }
    }

    private void CopyVisionSnapshotToAudio()
    {
        lock (gate)
        {
            audioSnapshot = visionSnapshot;
        }
    }

    private void SetAudioSnapshot(Func<TextModelDownloadSnapshot, TextModelDownloadSnapshot> update)
    {
        lock (gate)
        {
            audioSnapshot = update(audioSnapshot);
        }
    }

    public void CancelTextDownload()
    {
        lock (gate)
        {
            downloadCts?.Cancel();
            snapshot = snapshot with { Phase = TextModelDownloadPhase.Cancelled };
        }
    }

    public void ClearLocalDownloadState()
    {
        lock (gate)
        {
            downloadCts?.Cancel();
            downloadCts?.Dispose();
            downloadCts = null;
            visionDownloadCts?.Cancel();
            visionDownloadCts?.Dispose();
            visionDownloadCts = null;
            visionDownloadInFlight = null;
            lastPublishedPercent = -1;
            lastPublishedVisionPercent = -1;
            snapshot = new TextModelDownloadSnapshot(TextModelDownloadPhase.NotStarted, 0, null);
            visionSnapshot = new TextModelDownloadSnapshot(TextModelDownloadPhase.NotStarted, 0, null);
            audioSnapshot = new TextModelDownloadSnapshot(TextModelDownloadPhase.NotStarted, 0, null);
        }

        ClearDirectory(modelDirectory);
        ClearDirectory(visionDirectory);
    }

    public Task RetryTextDownloadAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    {
        return RunDownloadAsync(
            [(options.ModelFileName, options.ModelBaseUri, options.ModelSha256)],
            modelDirectory,
            vision: false,
            ingestAfter: true,
            progress,
            cancellationToken);
    }

    private async Task RunDownloadAsync(
        (string Name, Uri BaseUri, string Sha256)[] files,
        string directory,
        bool vision,
        bool ingestAfter,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource cts;
        lock (gate)
        {
            if (vision)
            {
                visionDownloadCts?.Cancel();
                visionDownloadCts?.Dispose();
                cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                visionDownloadCts = cts;
                lastPublishedVisionPercent = -1;
                visionSnapshot = visionSnapshot with { Phase = TextModelDownloadPhase.Downloading, Fault = null, CompletedFiles = 0, FileCount = files.Length };
            }
            else
            {
                downloadCts?.Cancel();
                downloadCts?.Dispose();
                cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                downloadCts = cts;
                lastPublishedPercent = -1;
                snapshot = snapshot with { Phase = TextModelDownloadPhase.Downloading, Fault = null, CompletedFiles = 0, FileCount = files.Length };
            }
        }

        var token = cts.Token;
        try
        {
            for (var index = 0; index < files.Length; index++)
            {
                var file = files[index];
                await DownloadFileAsync(file.Name, file.BaseUri, file.Sha256, directory, vision, progress, token, index, files.Length).ConfigureAwait(false);
            }

            SetSnapshot(vision, current => current with { Phase = TextModelDownloadPhase.Ready, Fault = null });
            if (ingestAfter)
            {
                await TextModelReadyIngest.ReadGrantsAsync(TextModelDownloadPhase.Ready, ingest, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            SetSnapshot(vision, current => current with { Phase = TextModelDownloadPhase.Cancelled });
        }
        catch (RecallFaultException fault) when (fault.Fault == RecallFault.DownloadHashMismatch)
        {
            SetSnapshot(vision, current => current with { Phase = TextModelDownloadPhase.HashFailed, Fault = TextModelDownloadFault.HashMismatch });
        }
        catch (RecallFaultException fault) when (fault.Fault == RecallFault.DownloadIncomplete)
        {
            SetSnapshot(vision, current => current with { Phase = TextModelDownloadPhase.Interrupted, Fault = TextModelDownloadFault.Incomplete });
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            SetSnapshot(vision, current => current with { Phase = TextModelDownloadPhase.Interrupted, Fault = TextModelDownloadFault.Transport });
        }
    }

    private async Task DownloadFileAsync(
        string fileName,
        Uri baseUri,
        string expectedSha256,
        string directory,
        bool vision,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken,
        int completedFiles,
        int fileCount)
    {
        var targetPath = Path.Combine(directory, fileName);
        if (await ModelHashVerifier.MatchesExpectedHashAsync(targetPath, expectedSha256, cancellationToken).ConfigureAwait(false))
        {
            PublishProgress(progress, vision, completedFiles + 1, fileCount, 0, null);
            return;
        }

        await DownloadBodyAsync(fileName, baseUri, expectedSha256, directory, vision, progress, cancellationToken, allowRange: true, completedFiles, fileCount).ConfigureAwait(false);
        PublishProgress(progress, vision, completedFiles + 1, fileCount, 0, null);
    }

    private async Task DownloadBodyAsync(
        string fileName,
        Uri baseUri,
        string expectedSha256,
        string directory,
        bool vision,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken,
        bool allowRange,
        int completedFiles,
        int fileCount)
    {
        var targetPath = Path.Combine(directory, fileName);
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
            SetSnapshot(vision, TextModelDownloadUiPolicy.MarkVerifying);

            if (await ModelHashVerifier.MatchesExpectedHashAsync(partialPath, expectedSha256, cancellationToken).ConfigureAwait(false))
            {
                MovePartial(partialPath, targetPath);
                return;
            }

            TryDelete(partialPath);
            if (allowRange)
            {
                await DownloadBodyAsync(fileName, baseUri, expectedSha256, directory, vision, progress, cancellationToken, allowRange: false, completedFiles, fileCount).ConfigureAwait(false);
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
                PublishProgress(progress, vision, completedFiles, fileCount, totalRead, expectedTotal);
            }

            await file.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (!DownloadTransfer.ReceivedFullBody(totalRead, expectedTotal))
            {
                throw new RecallFaultException(RecallFault.DownloadIncomplete);
            }
        }

        SetSnapshot(vision, TextModelDownloadUiPolicy.MarkVerifying);

        if (!await ModelHashVerifier.MatchesExpectedHashAsync(partialPath, expectedSha256, cancellationToken).ConfigureAwait(false))
        {
            TryDelete(partialPath);
            throw new RecallFaultException(RecallFault.DownloadHashMismatch);
        }

        MovePartial(partialPath, targetPath);
    }

    private void PublishProgress(
        IProgress<ModelDownloadProgress>? progress,
        bool vision,
        int completedFiles,
        int fileCount,
        long bytesReceived,
        long? totalBytes)
    {
        var percent = TextModelDownloadUiPolicy.ComputeOverallPercent(completedFiles, fileCount, bytesReceived, totalBytes);
        var shouldPublish = false;
        lock (gate)
        {
            if (vision)
            {
                visionSnapshot = visionSnapshot with
                {
                    Phase = TextModelDownloadPhase.Downloading,
                    BytesReceived = bytesReceived,
                    CompletedFiles = completedFiles,
                    FileCount = fileCount,
                    CurrentFileTotal = totalBytes,
                    Fault = null,
                };
                shouldPublish = TextModelDownloadUiPolicy.ShouldPublishProgress(lastPublishedVisionPercent, percent);
                if (shouldPublish)
                {
                    lastPublishedVisionPercent = percent;
                }
            }
            else
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
                shouldPublish = TextModelDownloadUiPolicy.ShouldPublishProgress(lastPublishedPercent, percent);
                if (shouldPublish)
                {
                    lastPublishedPercent = percent;
                }
            }
        }

        if (!shouldPublish)
        {
            return;
        }

        progress?.Report(new ModelDownloadProgress(bytesReceived, totalBytes, completedFiles, fileCount));
    }

    private void SetSnapshot(bool vision, Func<TextModelDownloadSnapshot, TextModelDownloadSnapshot> update)
    {
        lock (gate)
        {
            if (vision)
            {
                visionSnapshot = update(visionSnapshot);
            }
            else
            {
                snapshot = update(snapshot);
            }
        }
    }

    private static void ClearDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            TryDelete(file);
        }
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
