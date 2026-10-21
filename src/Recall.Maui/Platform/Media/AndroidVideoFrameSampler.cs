#if ANDROID
using Android.Graphics;
using Android.Media;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Path = System.IO.Path;
using Stream = System.IO.Stream;

namespace Recall.Maui.Platform;

public sealed class AndroidVideoFrameSampler : IVideoFrameSamplerService
{
    public IReadOnlyList<VideoFrame> SampleEveryTwoSeconds(Stream media)
    {
        ArgumentNullException.ThrowIfNull(media);
        var path = WriteTempFile(media);
        try
        {
            using var retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(path);
            var durationMilliseconds = long.TryParse(
                retriever.ExtractMetadata(MetadataKey.Duration),
                out var parsed)
                ? parsed
                : 0;
            if (durationMilliseconds <= 0)
            {
                return [];
            }

            var durationSeconds = durationMilliseconds / 1000d;
            var frames = new List<VideoFrame>();
            foreach (var start in FrameCadence.Starts(durationSeconds))
            {
                using var bitmap = retriever.GetFrameAtTime((long)(start * 1_000_000), Option.Closest);
                if (bitmap is null)
                {
                    continue;
                }

                using var jpeg = new MemoryStream();
                if (!bitmap.Compress(Bitmap.CompressFormat.Jpeg!, 90, jpeg))
                {
                    continue;
                }

                frames.Add(new VideoFrame(jpeg.ToArray(), start, FrameCadence.End(start, durationSeconds)));
            }

            return frames;
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string WriteTempFile(Stream media)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, $"recall-video-{Guid.NewGuid():N}.mp4");
        using var file = File.Create(path);
        media.CopyTo(file);
        return path;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // The temp file is best-effort cleanup.
        }
    }
}
#endif
