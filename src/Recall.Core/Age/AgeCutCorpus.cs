using Recall.Core.Faults;
using Recall.Core.Indexing;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.Core.Vocabulary;

namespace Recall.Core.Age;

public static class AgeCutCorpus
{
    public const string DefaultDemoRoot = @"D:\A_S\Recall_DemoData\recall_demo_dataset";

    private const string DemoRoot = DefaultDemoRoot;

    public static Func<Modality, IReadOnlyList<AgeCutItem>?>? TryBuildItems { get; set; }

    private static IVisionEmbeddingEncoder? visionEncoder;
    private static IAudioEmbeddingEncoder? audioEncoder;
    private static IVideoFrameSamplerService? videoSampler;
    private static IAudioSource? audioSource;
    private static IIngestModelSession? ingestSession;

    public static void Configure(
        IVisionEmbeddingEncoder vision,
        IAudioEmbeddingEncoder audio,
        IVideoFrameSamplerService sampler,
        IAudioSource source,
        IIngestModelSession? session = null)
    {
        visionEncoder = vision;
        audioEncoder = audio;
        videoSampler = sampler;
        audioSource = source;
        ingestSession = session;
    }

    public static bool TryLoad(Modality modality, out IReadOnlyList<AgeCutItem> items)
    {
        if (modality == Modality.Text)
        {
            throw new RecallFaultException(RecallFault.AgeCutTextForbidden);
        }

        if (TryBuildItems is not null)
        {
            var built = TryBuildItems(modality);
            if (built is null || built.Count == 0)
            {
                items = Array.Empty<AgeCutItem>();
                return false;
            }

            items = built;
            return true;
        }

        if (visionEncoder is null || audioEncoder is null || videoSampler is null || audioSource is null || !Directory.Exists(DemoRoot))
        {
            items = Array.Empty<AgeCutItem>();
            return false;
        }

        var paths = ListDemoPaths(modality);
        if (paths.Count == 0)
        {
            items = Array.Empty<AgeCutItem>();
            return false;
        }

        var list = new List<AgeCutItem>(paths.Count);
        var id = 0;
        var leasedVision = false;
        if (ingestSession is not null && MediaContextPolicy.OneContext)
        {
            ingestSession.OpenVision();
            leasedVision = true;
        }

        try
        {
            foreach (var path in paths)
            {
                var vector = EmbedPath(modality, path);
                if (vector is null || vector.Length != MediaCollections.Width)
                {
                    items = Array.Empty<AgeCutItem>();
                    return false;
                }

                list.Add(new AgeCutItem(id++, vector, vector));
            }

            items = list;
            return true;
        }
        finally
        {
            if (leasedVision)
            {
                ingestSession!.CloseVision();
            }
        }
    }

    private static List<string> ListDemoPaths(Modality modality)
    {
        var subfolder = modality switch
        {
            Modality.Image => "images",
            Modality.Video => "video",
            Modality.Audio => "audio",
            _ => string.Empty,
        };
        var folder = Path.Combine(DemoRoot, subfolder);
        if (!Directory.Exists(folder))
        {
            return [];
        }

        Func<string, bool> match = modality switch
        {
            Modality.Image => MediaFileKinds.IsImage,
            Modality.Video => MediaFileKinds.IsVideo,
            Modality.Audio => MediaFileKinds.IsAudio,
            _ => static _ => false,
        };

        return Directory
            .EnumerateFiles(folder)
            .Where(match)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
    }

    private static float[]? EmbedPath(Modality modality, string path)
    {
        try
        {
            return modality switch
            {
                Modality.Image => EmbedImage(path),
                Modality.Video => EmbedVideo(path),
                Modality.Audio => EmbedAudio(path),
                _ => null,
            };
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static float[] EmbedImage(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var vector = new float[MediaCollections.Width];
        visionEncoder!.EmbedJpeg(bytes, vector);
        return vector;
    }

    private static float[] EmbedVideo(string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var stream = new MemoryStream(bytes, writable: false);
        var frames = videoSampler!.SampleEveryTwoSeconds(stream);
        if (frames.Count == 0)
        {
            throw new InvalidOperationException("No frames sampled.");
        }

        var vector = new float[MediaCollections.Width];
        visionEncoder!.EmbedJpeg(frames[0].Jpeg, vector);
        return vector;
    }

    private static float[] EmbedAudio(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var pcm = WavPcm.IsWav(bytes) ? WavPcm.Read(bytes) : audioSource!.ReadCompressed(bytes);
        var durationMilliseconds = (int)(pcm.LongLength * 1000L / AudioWindows.SampleRate);
        var windows = AudioWindows.Plan(durationMilliseconds);
        if (windows.Count == 0)
        {
            throw new InvalidOperationException("No audio windows.");
        }

        var window = windows[0];
        var start = window.StartMilliseconds * AudioWindows.SampleRate / 1000;
        var end = window.EndMilliseconds * AudioWindows.SampleRate / 1000;
        var length = Math.Min(end - start, pcm.Length - start);
        var slice = new float[length];
        Array.Copy(pcm, start, slice, 0, length);
        var vector = new float[MediaCollections.Width];
        audioEncoder!.EmbedPcm(slice, vector);
        return vector;
    }
}
