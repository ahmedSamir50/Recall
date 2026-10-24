using Recall.Core.Age;
using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.Core.Vocabulary;

namespace Recall.Core.Ingest;

public sealed class AudioFileIngest : IAudioFileIngest
{
    private readonly IAudioSource source;
    private readonly ISiblingFiles siblings;
    private readonly IAudioEmbeddingEncoder encoder;
    private readonly IMediaVectorIndex index;
    private readonly IAudioOffReport report;
    private readonly IEncoderFileHash hashes;
    private readonly IGrantedTextReader reader;
    private readonly IIngestCursorStore cursors;
    private readonly AgeMoveService? ageMove;

    public AudioFileIngest(
        IAudioSource source,
        ISiblingFiles siblings,
        IAudioEmbeddingEncoder encoder,
        IMediaVectorIndex index,
        IAudioOffReport report,
        IEncoderFileHash hashes,
        IGrantedTextReader reader,
        IIngestCursorStore cursors,
        AgeMoveService? ageMove = null)
    {
        this.source = source;
        this.siblings = siblings;
        this.encoder = encoder;
        this.index = index;
        this.report = report;
        this.hashes = hashes;
        this.reader = reader;
        this.cursors = cursors;
        this.ageMove = ageMove;
    }

    public async Task IndexGrantAsync(ScopeFolderGrant grant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        cancellationToken.ThrowIfCancellationRequested();
        await foreach (var path in ListMediaPathsAsync(grant, cancellationToken).ConfigureAwait(false))
        {
            await IndexAsync(grant, path, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task IndexAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        cancellationToken.ThrowIfCancellationRequested();
        var fileName = Path.GetFileName(path);
        if (SiblingTranscript.IsCaptionFile(fileName))
        {
            return;
        }

        if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Audio))
        {
            report.Report();
            return;
        }

        if (ShouldSkipShort(path))
        {
            return;
        }

        var fileBytes = source.Read(path);
        if (fileBytes is null)
        {
            return;
        }

        await IngestPcmAsync(grant, path, fileBytes, cancellationToken).ConfigureAwait(false);
    }

    private async IAsyncEnumerable<string> ListMediaPathsAsync(
        ScopeFolderGrant grant,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var path in reader.ListAudioAndVideoAsync(grant, cancellationToken).ConfigureAwait(false))
        {
            yield return path;
        }
    }

    private async Task IngestPcmAsync(
        ScopeFolderGrant grant,
        string path,
        byte[] fileBytes,
        CancellationToken cancellationToken)
    {
        var contentHash = ContentIdBuilder.HashBytes(fileBytes);
        var row = cursors.GetFile(path);
        if (IngestCursorPolicy.Decide(row, contentHash) == IngestCursorDecision.Skip)
        {
            return;
        }

        cursors.MarkReading(path, contentHash, grant.Path);

        float[] pcm;
        if (WavPcm.IsWav(fileBytes))
        {
            try
            {
                pcm = WavPcm.Read(fileBytes);
            }
            catch (InvalidDataException)
            {
                return;
            }
        }
        else
        {
            try
            {
                pcm = source.ReadCompressed(fileBytes);
            }
            catch (NotSupportedException)
            {
                return;
            }
        }

        var durationMilliseconds = (int)(pcm.LongLength * 1000L / AudioWindows.SampleRate);
        var windows = AudioWindows.Plan(durationMilliseconds);
        if (windows.Count == 0)
        {
            return;
        }

        var words = MediaWords.Join(null, SiblingTranscript.Join(SiblingTexts(path)), null);
        var encoderHash = hashes.AudioHash ?? string.Empty;
        var sessionOwned = encoder.IsLoaded || MediaContextPolicy.OneContext;
        var inserts = new List<MediaIndexInsert>(windows.Count);
        var writes = new List<ChunkWrite>(windows.Count);
        for (var ordinal = 0; ordinal < windows.Count; ordinal++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var window = windows[ordinal];
            var slice = WindowPcm(pcm, window);
            var vector = new float[MediaCollections.Width];
            encoder.EmbedPcm(slice, vector);
            var insert = MediaIndexInsert.AudioWindow(
                fileBytes,
                path,
                words,
                vector,
                encoderHash,
                true,
                grant.Path,
                ordinal,
                AudioWindows.Seconds(window.StartMilliseconds),
                AudioWindows.Seconds(window.EndMilliseconds));
            inserts.Add(insert);
            writes.Add(new ChunkWrite(
                insert.ContentId,
                ordinal,
                0,
                0,
                SampleIndex(window.StartMilliseconds),
                SampleIndex(window.EndMilliseconds)));
        }

        cursors.FinishPath(path, contentHash, grant.Path, encoderHash, writes);
        index.Create(MediaCollections.Audio, MediaCollections.Width);
        var storedLong = false;
        foreach (var insert in inserts)
        {
            storedLong |= index.TryInsert(MediaCollections.Audio, insert) || index.TryRead(insert.ContentId, out _);
        }

        if (storedLong)
        {
            cursors.UpsertMediaFile(path, Modality.Audio);
            ageMove?.TryMovePath(path, Modality.Audio, DateTimeOffset.UtcNow);
        }

        if (!sessionOwned)
        {
            encoder.CloseAudio();
        }
        foreach (var retiredId in cursors.ListRetiredIds(path))
        {
            if (cursors.AnyLivePath(retiredId))
            {
                continue;
            }

            if (index.TryDelete(retiredId))
            {
                cursors.EnqueuePending(retiredId);
            }
        }
    }

    private bool ShouldSkipShort(string path)
    {
        var row = cursors.FindMediaFile(path, Modality.Audio);
        return row is not null && row.Width == MediaCollections.OldWidth && !cursors.IsPinned(path);
    }

    private List<string> SiblingTexts(string mediaPath)
    {
        var mediaFileName = Path.GetFileName(mediaPath);
        var candidates = siblings.List(mediaPath)
            .Where(sibling => SiblingTranscript.Matches(mediaFileName, Path.GetFileName(sibling.Name)))
            .ToList();
        var texts = new List<string>();
        foreach (var name in SiblingTranscript.Order(candidates
            .Where(sibling => sibling.Text is not null)
            .Select(sibling => sibling.Name)
            .ToList()))
        {
            var sibling = candidates.First(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            texts.Add(SiblingTranscript.ExtractCues(Path.GetExtension(name), sibling.Text));
        }

        return texts;
    }

    private static int SampleIndex(int milliseconds) =>
        (int)((long)milliseconds * AudioWindows.SampleRate / 1000);

    private static ReadOnlyMemory<float> WindowPcm(float[] pcm, AudioWindow window)
    {
        var startSample = (long)window.StartMilliseconds * AudioWindows.SampleRate / 1000;
        var endSample = (long)window.EndMilliseconds * AudioWindows.SampleRate / 1000;
        if (startSample >= pcm.LongLength)
        {
            return ReadOnlyMemory<float>.Empty;
        }

        var length = (int)Math.Min(endSample, pcm.LongLength) - (int)startSample;
        return pcm.AsMemory((int)startSample, Math.Max(0, length));
    }
}
