#if WINDOWS
using System.Diagnostics.CodeAnalysis;
using Windows.Media.Editing;
using Windows.Media.Core;
using Windows.Storage.Streams;
using Recall.Core.Ingest;
using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed class WindowsVideoFrameSampler : IVideoFrameSamplerService
{
    private const int ThumbnailWidth = 640;

    private const int ThumbnailHeight = 360;

    public IReadOnlyList<Recall.Core.Ports.VideoFrame> SampleEveryTwoSeconds(Stream media)
    {
        ArgumentNullException.ThrowIfNull(media);
        if (Environment.OSVersion.Version.Build < FrameCadence.WindowsMinimumBuild)
        {
            throw new NotSupportedException(FrameCadence.WindowsMinimumBuildMessage);
        }

        var path = Path.Combine(FileSystem.CacheDirectory, $"recall-video-{Guid.NewGuid():N}.mp4");
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
        var snapshots = await composition
            .GetThumbnailsAsync(
                starts.Select(start => TimeSpan.FromSeconds(start)).ToList(),
                ThumbnailWidth,
                ThumbnailHeight,
                VideoFramePrecision.NearestFrame)
            .AsTask()
            .ConfigureAwait(false);
        var frames = new List<Recall.Core.Ports.VideoFrame>();
        for (var index = 0; index < snapshots.Count; index++)
        {
            var start = starts[index];
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

        return frames;
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
