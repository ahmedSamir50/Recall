using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;
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

    public VisionFileIngest(
        IVisionEmbeddingEncoder encoder,
        IMediaVectorIndex index,
        IEncoderFileHash hashes,
        IIngestCursorStore cursors,
        IVideoFrameSamplerService? sampler = null)
    {
        this.encoder = encoder;
        this.index = index;
        this.hashes = hashes;
        this.cursors = cursors;
        this.sampler = sampler;
    }

    public async Task IndexImageAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        cancellationToken.ThrowIfCancellationRequested();
        if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Image) || !File.Exists(path))
        {
            return;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var vector = new float[MediaCollections.Width];
        encoder.EmbedJpeg(bytes, vector);
        var hash = hashes.VisionHash ?? string.Empty;
        var insert = MediaIndexInsert.Picture(bytes, path, string.Empty, vector, hash, true, grant.Path);
        cursors.FinishPath(path, ContentIdBuilder.HashBytes(bytes), grant.Path, hash, [new ChunkWrite(insert.ContentId, 0)]);
        index.Create(MediaCollections.Image, MediaCollections.Width);
        index.TryInsert(MediaCollections.Image, insert);
    }

    public async Task IndexVideoFramesAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        cancellationToken.ThrowIfCancellationRequested();
        if (sampler is null || !grant.EnabledKinds.HasFlag(GrantFileKinds.Video) || !File.Exists(path))
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
            var insert = MediaIndexInsert.VideoFrame(
                bytes,
                path,
                string.Empty,
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
        foreach (var insert in inserts)
        {
            index.TryInsert(MediaCollections.Video, insert);
        }

    }

    private static string FormatSeconds(double seconds) =>
        seconds.ToString("0.######", CultureInfo.InvariantCulture);
}
