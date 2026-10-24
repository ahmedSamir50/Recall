#if IOS || MACCATALYST
using AudioToolbox;
using AVFoundation;
using CoreMedia;
using Foundation;
using Recall.Core.Ingest;
using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed class AppleAudioSource : IAudioSource
{
    public byte[]? Read(string path) =>
        string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? null : File.ReadAllBytes(path);

    public float[] ReadCompressed(byte[] file)
    {
        var path = WriteTempFile(file);
        try
        {
            return Decode(path);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static float[] Decode(string path)
    {
        using var url = NSUrl.FromFilename(path);
        using var asset = AVAsset.FromUrl(url);
        var audioTracks = asset.GetTracks(AVMediaTypes.Audio);
        if (audioTracks is null || audioTracks.Length == 0)
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }

        using var track = audioTracks[0];
        var outputSettings = new AudioSettings
        {
            Format = AudioFormatType.LinearPCM,
            SampleRate = 16000,
            NumberChannels = 1,
            LinearPcmBitDepth = 16,
            LinearPcmFloat = false,
            LinearPcmBigEndian = false,
            LinearPcmNonInterleaved = false,
        };

        using var reader = new AVAssetReader(asset, out var readerError);
        if (reader is null || readerError is not null)
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }

        using var output = new AVAssetReaderTrackOutput(track, outputSettings);
        reader.AddOutput(output);
        if (!reader.StartReading())
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }

        var samples = new List<float>();
        while (reader.Status == AVAssetReaderStatus.Reading)
        {
            using var sampleBuffer = output.CopyNextSampleBuffer();
            if (sampleBuffer is null)
            {
                break;
            }

            AppendPcm(samples, sampleBuffer);
        }

        if (reader.Status == AVAssetReaderStatus.Failed || samples.Count == 0)
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }

        return samples.ToArray();
    }

    private static void AppendPcm(List<float> destination, CMSampleBuffer sampleBuffer)
    {
        using var blockBuffer = sampleBuffer.GetDataBuffer();
        if (blockBuffer is null)
        {
            return;
        }

        var length = blockBuffer.DataLength;
        if (length <= 0)
        {
            return;
        }

        var copyError = blockBuffer.CopyDataBytes(0, length, out var bytes);
        if (copyError != CMBlockBufferError.None || bytes is null)
        {
            return;
        }
        for (var index = 0; index + 1 < bytes.Length; index += 2)
        {
            var sample = (short)(bytes[index] | (bytes[index + 1] << 8));
            destination.Add(sample / 32768f);
        }
    }

    private static string WriteTempFile(byte[] file)
    {
        var extension = GuessExtension(file);
        var path = Path.Combine(FileSystem.CacheDirectory, $"recall-audio-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, file);
        return path;
    }

    private static string GuessExtension(ReadOnlySpan<byte> file)
    {
        if (file.Length >= 12 && file[4] == (byte)'f' && file[5] == (byte)'t' && file[6] == (byte)'y' && file[7] == (byte)'p')
        {
            return ".mp4";
        }

        if (file.Length >= 3 && file[0] == (byte)'I' && file[1] == (byte)'D' && file[2] == (byte)'3')
        {
            return ".mp3";
        }

        if (file.Length >= 2 && file[0] == 0xFF && (file[1] & 0xE0) == 0xE0)
        {
            return ".mp3";
        }

        return ".m4a";
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
