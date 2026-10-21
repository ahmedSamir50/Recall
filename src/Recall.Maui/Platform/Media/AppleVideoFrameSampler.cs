#if IOS || MACCATALYST
using AVFoundation;
using CoreMedia;
using Foundation;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using UIKit;

namespace Recall.Maui.Platform;

public sealed class AppleVideoFrameSampler : IVideoFrameSamplerService
{
    public IReadOnlyList<VideoFrame> SampleEveryTwoSeconds(Stream media)
    {
        ArgumentNullException.ThrowIfNull(media);
        var path = WriteTempFile(media);
        try
        {
            using var url = NSUrl.FromFilename(path);
            using var asset = AVAsset.FromUrl(url);
            var duration = asset.Duration.Seconds;
            if (duration <= 0)
            {
                return [];
            }

            using var generator = AVAssetImageGenerator.FromAsset(asset);
            generator.AppliesPreferredTrackTransform = true;
            var frames = new List<VideoFrame>();
            foreach (var start in FrameCadence.Starts(duration))
            {
                var time = CMTime.FromSeconds(start, 600);
#pragma warning disable CA1422
                using var image = generator.CopyCGImageAtTime(time, out _, out _);
#pragma warning restore CA1422
                if (image is null)
                {
                    continue;
                }

                using var uiImage = UIImage.FromImage(image);
                using var data = uiImage.AsJPEG(0.9f);
                if (data is null)
                {
                    continue;
                }

                frames.Add(new VideoFrame(data.ToArray(), start, FrameCadence.End(start, duration)));
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
