#if WINDOWS
using Recall.Core.Ingest;
using Recall.Core.Ports;
using System.Diagnostics.CodeAnalysis;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Storage.Streams;

namespace Recall.Maui.Platform;

public sealed class WindowsVideoFrameSampler : IVideoFrameSamplerService
{
    private const int ThumbnailWidth = 640;

    private const int ThumbnailHeight = 360;

    private const int FrameBatchSize = 64;

    public IReadOnlyList<Recall.Core.Ports.VideoFrame> SampleEveryTwoSeconds(Stream media)
    {
        ArgumentNullException.ThrowIfNull(media);
        if (Environment.OSVersion.Version.Build < FrameCadence.WindowsMinimumBuild)
        {
            throw new NotSupportedException(FrameCadence.WindowsMinimumBuildMessage);
        }

        var cacheRoot = TryCacheDirectory() ?? Path.GetTempPath();
        var path = Path.Combine(cacheRoot, $"recall-video-{Guid.NewGuid():N}.mp4");
        try
        {
            return SampleBlocking(media, path);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [SuppressMessage("Performance", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead", Justification = "The sampler port is synchronous. WinRT work starts on the thread pool so it does not capture the UI synchronization context.")]
    private static IReadOnlyList<Recall.Core.Ports.VideoFrame> SampleBlocking(Stream media, string path)
    {
        using (var file = File.Create(path))
        {
            media.CopyTo(file);
        }

        return Task.Run(() => ComposeAsync(path)).GetAwaiter().GetResult();
    }

    private static async Task<IReadOnlyList<Recall.Core.Ports.VideoFrame>> ComposeAsync(string path)
    {
        var files = await Windows.Storage.StorageFile.GetFileFromPathAsync(path).AsTask().ConfigureAwait(false);
        var clip = await MediaClip.CreateFromFileAsync(files).AsTask().ConfigureAwait(false);
        var composition = new MediaComposition();
        composition.Clips.Add(clip);
        var duration = composition.Duration;
        if (duration <= TimeSpan.Zero)
        {
            return [];
        }

        var starts = FrameCadence.Starts(duration.TotalSeconds);
        var frames = new List<Recall.Core.Ports.VideoFrame>();
        for (var offset = 0; offset < starts.Count; offset += FrameBatchSize)
        {
            var count = Math.Min(FrameBatchSize, starts.Count - offset);
            var batch = new List<TimeSpan>(count);
            for (var index = 0; index < count; index++)
            {
                batch.Add(TimeSpan.FromSeconds(starts[offset + index]));
            }

            var snapshots = await composition
                .GetThumbnailsAsync(
                    batch,
                    ThumbnailWidth,
                    ThumbnailHeight,
                    VideoFramePrecision.NearestFrame)
                .AsTask()
                .ConfigureAwait(false);
            for (var index = 0; index < snapshots.Count; index++)
            {
                var start = starts[offset + index];
                using var snapshot = snapshots[index];
                if (snapshot is null)
                {
                    continue;
                }

                using var netStream = snapshot.AsStreamForRead();
                using var jpeg = new MemoryStream();
                await netStream.CopyToAsync(jpeg).ConfigureAwait(false);
                frames.Add(new VideoFrame(jpeg.ToArray(), start, FrameCadence.End(start, duration.TotalSeconds)));
            }
        }

        return frames;
    }

    private static string? TryCacheDirectory()
    {
        try
        {
            return FileSystem.CacheDirectory;
        }
        catch (Exception)
        {
            return null;
        }
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
