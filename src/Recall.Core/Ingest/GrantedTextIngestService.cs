using Recall.Core.Checkpoints;
using Recall.Core.Download;
using Recall.Core.Ocr;
using Recall.Core.Ports;
using Recall.Core.Runtime;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public sealed class GrantedTextIngestService : IGrantedTextIngestService
{
    public const string PictureIngestNotRegistered = "Picture ingest is not registered.";

    private readonly IScopeManagerService scope;
    private readonly IGrantedTextReader reader;
    private readonly TextIngestEngine engine;
    private readonly IIngestCursorStore cursors;
    private readonly ITextVectorIndex index;
    private readonly LocalOpLogWriter opLog;
    private readonly IEncoderFileHash encoderFileHash;
    private readonly IAudioFileIngest? audio;
    private readonly IngestScheduler? scheduler;
    private readonly IVisionFileIngest? vision;
    private readonly IModelDownloadService? downloads;
    private readonly IPdfPageRenderer? pageRenderer;
    private readonly IMemoryBudget memory;
    private readonly MemoryGate gate;
    private readonly IIngestResourceReclaimer reclaimer;
    private readonly SemaphoreSlim pass = new(1, 1);
    private int pending;
    private readonly object failureGate = new();
    private readonly List<string> failedPaths = new();

    public GrantedTextIngestService(
        IScopeManagerService scope,
        IGrantedTextReader reader,
        TextIngestEngine engine,
        IIngestCursorStore cursors,
        ITextVectorIndex index,
        LocalOpLogWriter opLog,
        IEncoderFileHash encoderFileHash,
        IMemoryBudget memory,
        MemoryGate gate,
        IIngestResourceReclaimer reclaimer,
        IAudioFileIngest? audio = null,
        IngestScheduler? scheduler = null,
        IVisionFileIngest? vision = null,
        IModelDownloadService? downloads = null,
        IPdfPageRenderer? pageRenderer = null)
    {
        this.scope = scope;
        this.reader = reader;
        this.engine = engine;
        this.cursors = cursors;
        this.index = index;
        this.opLog = opLog;
        this.encoderFileHash = encoderFileHash;
        this.memory = memory;
        this.gate = gate;
        this.reclaimer = reclaimer;
        this.audio = audio;
        this.scheduler = scheduler;
        this.vision = vision;
        this.downloads = downloads;
        this.pageRenderer = pageRenderer;
    }

    public IReadOnlyList<string> FailedPaths
    {
        get
        {
            lock (failureGate)
            {
                return failedPaths.ToArray();
            }
        }
    }

    public bool PassRunning => pass.CurrentCount == 0;

    public async Task IngestGrantedTextAsync(CancellationToken cancellationToken = default)
    {
        if (!await pass.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            Interlocked.Exchange(ref pending, 1);
            return;
        }

        try
        {
            lock (failureGate)
            {
                failedPaths.Clear();
            }

            foreach (var grant in scope.ListGrants())
            {
                await IngestGrantAsync(grant.Path, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            pass.Release();
            if (Volatile.Read(ref pending) == 1)
            {
                Interlocked.Exchange(ref pending, 0);
                _ = IngestGrantedTextAsync(cancellationToken);
            }
        }
    }

    public async Task IngestGrantAsync(string path, CancellationToken cancellationToken = default)
    {
        var grant = scope.ListGrants()
            .FirstOrDefault(g => ScopeGrantPolicy.Normalize(g.Path).Equals(ScopeGrantPolicy.Normalize(path), StringComparison.OrdinalIgnoreCase));
        if (grant is null)
        {
            return;
        }

        var textReady = downloads is null || downloads.GetTextDownloadSnapshot().Phase == TextModelDownloadPhase.Ready;
        var visionReady = downloads is null || downloads.GetVisionDownloadSnapshot().Phase == TextModelDownloadPhase.Ready;
        var audioReady = downloads is null || downloads.GetAudioDownloadSnapshot().Phase == TextModelDownloadPhase.Ready;

        var textOrCodeEnabled = grant.EnabledKinds.HasFlag(GrantFileKinds.Text) || grant.EnabledKinds.HasFlag(GrantFileKinds.Code);
        var imageEnabled = grant.EnabledKinds.HasFlag(GrantFileKinds.Image);
        var videoEnabled = grant.EnabledKinds.HasFlag(GrantFileKinds.Video);
        var audioEnabled = grant.EnabledKinds.HasFlag(GrantFileKinds.Audio);

        var skippedModel = (textOrCodeEnabled && !textReady)
            || (imageEnabled && !visionReady)
            || (videoEnabled && !visionReady)
            || ((audioEnabled || videoEnabled) && audio is not null && !audioReady);

        var runText = textOrCodeEnabled && textReady;
        var runImages = imageEnabled && visionReady;
        var runVideos = videoEnabled && visionReady;
        var runAudio = audio is not null && audioReady && audioEnabled;

        if (!runText && !runImages && !runVideos && !runAudio)
        {
            return;
        }

        if (!ObserveGate())
        {
            return;
        }

        cursors.MarkGrantReading(grant.Path);

        if (runText)
        {
            await foreach (var document in reader.ReadAsync(grant, cancellationToken).ConfigureAwait(false))
            {
                if (!ObserveGate())
                {
                    return;
                }

                try
                {
                    await IngestDocumentAsync(grant, document, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    if (HasCommittedPdfPages(document.Path))
                    {
                        RecordStopped(document.Path);
                    }
                    else
                    {
                        RecordFailure(document.Path);
                    }
                }
            }
        }

        if (runImages)
        {
            await foreach (var imagePath in reader.ListImagesAsync(grant, cancellationToken).ConfigureAwait(false))
            {
                if (vision is null)
                {
                    throw new InvalidOperationException(PictureIngestNotRegistered);
                }

                try
                {
                    await vision.IndexImageAsync(grant, imagePath, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    RecordFailure(imagePath);
                }
            }
        }

        if (runVideos)
        {
            await foreach (var mediaPath in reader.ListAudioAndVideoAsync(grant, cancellationToken).ConfigureAwait(false))
            {
                var name = Path.GetFileName(mediaPath);
                if (!MediaFileKinds.IsVideo(name))
                {
                    continue;
                }

                if (vision is null)
                {
                    throw new InvalidOperationException(PictureIngestNotRegistered);
                }

                try
                {
                    await vision.IndexVideoFramesAsync(grant, mediaPath, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    RecordFailure(mediaPath);
                }
            }
        }

        if (runAudio)
        {
            await audio!.IndexGrantAsync(grant, cancellationToken).ConfigureAwait(false);
        }

        if (runText)
        {
            CloseLeftoverReading(grant, GrantFileKinds.Text);
            CloseLeftoverReading(grant, GrantFileKinds.Code);
        }

        if (runImages)
        {
            CloseLeftoverReading(grant, GrantFileKinds.Image);
        }

        if (runVideos)
        {
            CloseLeftoverReading(grant, GrantFileKinds.Video);
        }

        if (runAudio)
        {
            CloseLeftoverReading(grant, GrantFileKinds.Audio);
        }

        if (!skippedModel && (runText || runImages || runVideos))
        {
            cursors.MarkGrantFinished(grant.Path);
        }
    }

    public async Task IngestDocumentAsync(ScopeFolderGrant grant, GrantedTextDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        ArgumentNullException.ThrowIfNull(document);
        var contentHash = ContentIdBuilder.HashSourceContent(document.Text);
        var row = cursors.GetFile(document.Path);
        var encoderHash = encoderFileHash.TextHash ?? string.Empty;
        if (EncoderRepair.NeedsRepair(row?.EncoderHash, encoderHash))
        {
            cursors.MarkReading(document.Path, contentHash, grant.Path);
#pragma warning disable S3267
            var repairChunks = cursors.ListChunks(document.Path).Where(static c => !c.Retired).ToList();
#pragma warning restore S3267
            cursors.RetirePath(document.Path);
#pragma warning disable S3267
            foreach (var chunk in repairChunks)
#pragma warning restore S3267
            {
                if (cursors.AnyOtherLivePath(chunk.ContentId, document.Path))
                {
                    cursors.EnqueuePending(chunk.ContentId);
                    continue;
                }

                if (index.TryDelete(chunk.ContentId))
                {
                    opLog.AppendDelete(chunk.ContentId);
                }
            }
        }
        else if (IngestCursorPolicy.Decide(row, contentHash) == IngestCursorDecision.Skip)
        {
            return;
        }

        cursors.MarkReading(document.Path, contentHash, grant.Path);
        var codeFile = FileAdmission.IsCodeFile(document.FileName);
        var result = await engine.IngestFileAsync(
            document.Path,
            document.Text,
            document.ModifiedAt,
            grant.Path,
            encoderHash,
            codeFile,
            cancellationToken).ConfigureAwait(false);

        if (result.Stop == IngestStopReason.MemoryBudget)
        {
            cursors.MarkStopped(document.Path, IngestStopKeys.MemoryBudget);
            reclaimer.ReleaseOptionalEncoders();
            RecordStopped(document.Path);
            return;
        }

        var writes = result.Chunks
            .Select(c => new ChunkWrite(c.ContentId, c.Ordinal, c.PageStart, c.PageEnd, c.CharStart, c.CharEnd))
            .ToList();
        var bm25Only = result.Chunks
            .Where(c => !c.Dense)
            .Select(c => new Bm25OnlyRow(c.ContentId, document.Path, c.Bm25Raw))
            .ToList();
        if (result.OcrPageNumbers.Count > 0)
        {
            OcrPageWaitlist.Apply(document.Path, result.OcrPageNumbers, cursors, scheduler);
        }

        if (result.ImageFastPathPages.Count > 0 && pageRenderer is not null)
        {
            if (vision is null)
            {
                throw new InvalidOperationException(PictureIngestNotRegistered);
            }

            foreach (var pageNumber in result.ImageFastPathPages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var image = pageRenderer.Render(document.Path, pageNumber);
                    writes.Add(vision.EmbedPdfPageImage(grant, document.Path, pageNumber, image));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    RecordStopped(document.Path);
                    return;
                }
            }
        }

        cursors.FinishPath(document.Path, result.ContentHash, grant.Path, encoderHash, writes, bm25Only);
        memory.RecordObserved();
        gate.OnFileCompleted();

        foreach (var retiredId in cursors.ListRetiredIds(document.Path))
        {
            if (cursors.AnyLivePath(retiredId))
            {
                continue;
            }

            if (index.TryDelete(retiredId))
            {
                opLog.AppendDelete(retiredId);
                cursors.EnqueuePending(retiredId);
            }
        }
    }

    private bool ObserveGate()
    {
        if (memory.Verdict == DeviceVerdict.BelowMinimum)
        {
            gate.Block();
            return false;
        }

        gate.Observe(
            memory.Pressure,
            memory.CapacityBytes,
            memory.WorkingSetBytes,
            memory.EffectiveFloorBytes,
            DateTimeOffset.UtcNow);
        return gate.IsOpen();
    }

    private void CloseLeftoverReading(ScopeFolderGrant grant, GrantFileKinds kind)
    {
        foreach (var row in cursors.ListFiles(grant.Path))
        {
            if (row.State != IngestCursorState.Reading)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(row.MissingStatusKey))
            {
                continue;
            }

            if (!IngestPathKind.TryGetKind(row.Path, out var pathKind) || pathKind != kind)
            {
                continue;
            }

            if (HasCommittedPdfPages(row.Path))
            {
                continue;
            }

            cursors.MarkMissing(row.Path);
            RecordFailure(row.Path);
        }
    }

    private void RecordFailure(string path)
    {
        cursors.MarkMissing(path);
        RecordStopped(path);
    }

    private void RecordStopped(string path)
    {
        lock (failureGate)
        {
            if (!failedPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                failedPaths.Add(path);
            }
        }
    }

    private bool HasCommittedPdfPages(string path)
    {
        foreach (var chunk in cursors.ListChunks(path))
        {
            if (!chunk.Missing && !chunk.Retired && chunk.PageEnd > 0)
            {
                return true;
            }
        }

        return false;
    }
}
