using Recall.Core.Age;
using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.Core.Vocabulary;
using System.Globalization;

namespace Recall.Core.Ingest;

public interface IVisionFileIngest
{
    Task IndexImageAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default);

    Task IndexVideoFramesAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default);
}

public sealed class VisionFileIngest : IVisionFileIngest
{
    private readonly IVisionEmbeddingEncoder encoder;
    private readonly IMediaVectorIndex index;
    private readonly IEncoderFileHash hashes;
    private readonly IIngestCursorStore cursors;
    private readonly IVideoFrameSamplerService? sampler;
    private readonly AgeMoveService? ageMove;

    public VisionFileIngest(
        IVisionEmbeddingEncoder encoder,
        IMediaVectorIndex index,
        IEncoderFileHash hashes,
        IIngestCursorStore cursors,
        IVideoFrameSamplerService? sampler = null,
        AgeMoveService? ageMove = null)
    {
        this.encoder = encoder;
        this.index = index;
        this.hashes = hashes;
        this.cursors = cursors;
        this.sampler = sampler;
        this.ageMove = ageMove;
    }

    public async Task IndexImageAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        cancellationToken.ThrowIfCancellationRequested();
        if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Image) || !File.Exists(path))
        {
            return;
        }

        if (ShouldSkipShort(path, Modality.Image))
        {
            return;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var vector = new float[MediaCollections.Width];
        encoder.EmbedJpeg(bytes, vector);
        var hash = hashes.VisionHash ?? string.Empty;
        var words = MediaWords.Join(null, null, null);
        var insert = MediaIndexInsert.Picture(bytes, path, words, vector, hash, true, grant.Path);
        cursors.FinishPath(path, ContentIdBuilder.HashBytes(bytes), grant.Path, hash, [new ChunkWrite(insert.ContentId, 0)]);
        index.Create(MediaCollections.Image, MediaCollections.Width);
        var inserted = index.TryInsert(MediaCollections.Image, insert);
        if (inserted || index.TryRead(insert.ContentId, out _))
        {
            cursors.UpsertMediaFile(path, Modality.Image);
            ageMove?.TryMovePath(path, Modality.Image, DateTimeOffset.UtcNow);
        }
    }

    public async Task IndexVideoFramesAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        cancellationToken.ThrowIfCancellationRequested();
        if (sampler is null || !grant.EnabledKinds.HasFlag(GrantFileKinds.Video) || !File.Exists(path))
        {
            return;
        }

        if (ShouldSkipShort(path, Modality.Video))
        {
            return;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<VideoFrame> frames;
        using (var stream = new MemoryStream(bytes, writable: false))
        {
            frames = sampler.SampleEveryTwoSeconds(stream);
        }

        if (frames.Count == 0)
        {
            return;
        }

        var hash = hashes.VisionHash ?? string.Empty;
        var inserts = new List<MediaIndexInsert>(frames.Count);
        var writes = new List<ChunkWrite>(frames.Count);
        for (var ordinal = 0; ordinal < frames.Count; ordinal++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var frame = frames[ordinal];
            var vector = new float[MediaCollections.Width];
            encoder.EmbedJpeg(frame.Jpeg, vector);
            var words = MediaWords.Join(null, null, null);
            var insert = MediaIndexInsert.VideoFrame(
                bytes,
                path,
                words,
                vector,
                hash,
                true,
                grant.Path,
                ordinal,
                FormatSeconds(frame.StartSeconds),
                FormatSeconds(frame.EndSeconds));
            inserts.Add(insert);
            writes.Add(new ChunkWrite(insert.ContentId, ordinal));
        }

        cursors.FinishPath(path, ContentIdBuilder.HashBytes(bytes), grant.Path, hash, writes);
        index.Create(MediaCollections.Video, MediaCollections.Width);
        var storedLong = false;
        foreach (var insert in inserts)
        {
            storedLong |= index.TryInsert(MediaCollections.Video, insert) || index.TryRead(insert.ContentId, out _);
        }

        if (storedLong)
        {
            cursors.UpsertMediaFile(path, Modality.Video);
            ageMove?.TryMovePath(path, Modality.Video, DateTimeOffset.UtcNow);
        }
    }

    private bool ShouldSkipShort(string path, Modality modality)
    {
        var row = cursors.FindMediaFile(path, modality);
        return row is not null && row.Width == MediaCollections.OldWidth && !cursors.IsPinned(path);
    }

    private static string FormatSeconds(double seconds) =>
        seconds.ToString("0.######", CultureInfo.InvariantCulture);
}
