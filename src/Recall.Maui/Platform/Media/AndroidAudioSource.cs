#if ANDROID
using Android.Media;
using Java.Nio;
using Recall.Core.Ingest;
using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed class AndroidAudioSource : IAudioSource
{
    private const int InputTimeoutMicroseconds = 10_000;

    private const int OutputTimeoutMicroseconds = 10_000;

    private const int MaxDrainIterations = 1_000_000;

    public byte[]? Read(string path) =>
        string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? null : File.ReadAllBytes(path);

    public float[] ReadCompressed(byte[] file)
    {
        var path = WriteTempFile(file);
        try
        {
            return Decode(path);
        }
        catch (IOException)
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }
        catch (Java.Lang.Exception)
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
        using var extractor = new MediaExtractor();
        extractor.SetDataSource(path);
        var trackIndex = FindAudioTrack(extractor, out var trackFormat, out var mime);
        if (trackIndex < 0)
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }

        extractor.SelectTrack(trackIndex);
        using var codec = MediaCodec.CreateDecoderByType(mime);
        codec.Configure(trackFormat, null, null, MediaCodecConfigFlags.None);
        codec.Start();
        var samples = Drain(extractor, codec);
        var outputFormat = codec.OutputFormat;
        switch (outputFormat.GetInteger(MediaFormat.KeyChannelCount))
        {
            case 1:
                return AudioResample.To16k(samples, outputFormat.GetInteger(MediaFormat.KeySampleRate));
            case 2:
                return AudioResample.To16k(MonoDownMix(samples), outputFormat.GetInteger(MediaFormat.KeySampleRate));
            default:
                throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }
    }

    private static int FindAudioTrack(MediaExtractor extractor, out MediaFormat format, out string mime)
    {
        format = null!;
        mime = string.Empty;
        for (var index = 0; index < extractor.TrackCount; index++)
        {
            var candidate = extractor.GetTrackFormat(index);
            var candidateMime = candidate.GetString(MediaFormat.KeyMime) ?? string.Empty;
            if (candidateMime.StartsWith("audio/", StringComparison.Ordinal))
            {
                format = candidate;
                mime = candidateMime;
                return index;
            }
        }

        return -1;
    }

    private static float[] Drain(MediaExtractor extractor, MediaCodec codec)
    {
        var raw = new MemoryStream();
        var inputDone = false;
        var outputDone = false;
        var bufferInfo = new MediaCodec.BufferInfo();
        var iterations = 0;
        while (!outputDone)
        {
            iterations++;
            if (iterations > MaxDrainIterations)
            {
                throw new InvalidOperationException(AudioDecoders.FailureMessage);
            }

            if (!inputDone)
            {
                var inputIndex = codec.DequeueInputBuffer(InputTimeoutMicroseconds);
                if (inputIndex >= 0)
                {
                    var input = codec.GetInputBuffer(inputIndex)!;
                    input.Clear();
                    var size = extractor.ReadSampleData(input, 0);
                    if (size < 0)
                    {
                        codec.QueueInputBuffer(inputIndex, 0, 0, 0, MediaCodecBufferFlags.EndOfStream);
                        inputDone = true;
                    }
                    else
                    {
                        input.Limit(size);
                        codec.QueueInputBuffer(inputIndex, 0, size, extractor.SampleTime, (MediaCodecBufferFlags)0);
                        extractor.Advance();
                    }
                }
            }

            var status = codec.DequeueOutputBuffer(bufferInfo, OutputTimeoutMicroseconds);
            switch (status)
            {
                case (int)MediaCodecInfoState.TryAgainLater:
                case (int)MediaCodecInfoState.OutputBuffersChanged:
                    continue;
                case (int)MediaCodecInfoState.OutputFormatChanged:
                    RequirePcm16(codec.OutputFormat);
                    continue;
            }

            if (status < 0)
            {
                continue;
            }

            RequirePcm16(codec.OutputFormat);
            var output = codec.GetOutputBuffer(status)!;
            output.Position(bufferInfo.Offset);
            output.Limit(bufferInfo.Offset + bufferInfo.Size);
            output.Order(ByteOrder.NativeOrder());
            var shorts = output.AsShortBuffer();
            var count = shorts.Remaining();
            var samples = new short[count];
            shorts.Get(samples, 0, count);
            foreach (var sample in samples)
            {
                if (BitConverter.IsLittleEndian)
                {
                    raw.WriteByte((byte)(sample & 0xFF));
                    raw.WriteByte((byte)((sample >> 8) & 0xFF));
                }
                else
                {
                    raw.WriteByte((byte)((sample >> 8) & 0xFF));
                    raw.WriteByte((byte)(sample & 0xFF));
                }
            }

            if (bufferInfo.Flags.HasFlag(MediaCodecBufferFlags.EndOfStream))
            {
                outputDone = true;
            }

            codec.ReleaseOutputBuffer(status, false);
        }

        var bytes = raw.ToArray();
        var floats = new float[bytes.Length / 2];
        for (var index = 0; index < floats.Length; index++)
        {
            var sample = BitConverter.ToInt16(bytes, index * 2);
            floats[index] = sample / 32768f;
        }

        return floats;
    }

    private static void RequirePcm16(MediaFormat format)
    {
        if (!format.ContainsKey(MediaFormat.KeyPcmEncoding))
        {
            return;
        }

        var encoding = format.GetInteger(MediaFormat.KeyPcmEncoding);
        if (encoding != (int)Android.Media.Encoding.Pcm16bit)
        {
            throw new InvalidOperationException(AudioDecoders.FailureMessage);
        }
    }

    private static float[] MonoDownMix(float[] stereo)
    {
        var frames = stereo.Length / 2;
        var mono = new float[frames];
        for (var frame = 0; frame < frames; frame++)
        {
            mono[frame] = (stereo[frame * 2] + stereo[(frame * 2) + 1]) / 2f;
        }

        return mono;
    }

    private static string WriteTempFile(byte[] file)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, $"recall-audio-{Guid.NewGuid():N}");
        File.WriteAllBytes(path, file);
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
