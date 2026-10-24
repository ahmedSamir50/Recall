using System.Buffers.Binary;

namespace Recall.Core.Ingest;

public static class WavPcm
{
    private static ReadOnlySpan<byte> Riff => "RIFF"u8;

    private static ReadOnlySpan<byte> Wave => "WAVE"u8;

    private static ReadOnlySpan<byte> FmtId => "fmt "u8;

    private static ReadOnlySpan<byte> DataId => "data"u8;

    // KSDATAFORMAT_SUBTYPE_PCM = 00000001-0000-0010-8000-00aa00389b71
    private static ReadOnlySpan<byte> PcmSubFormat =>
        [0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71];

    private const short PcmTag = 1;

    private const short ExtensibleTag = unchecked((short)0xFFFE);

    private const string NotPcmMessage = "WAV encoding is not 16-bit PCM.";

    public static bool IsWav(ReadOnlyMemory<byte> file)
    {
        if (file.Length < 12)
        {
            return false;
        }

        var data = file.Span;
        return data[..4].SequenceEqual(Riff) && data[8..12].SequenceEqual(Wave);
    }

    public static float[] Read(ReadOnlyMemory<byte> file)
    {
        var data = file.Span;
        if (data.Length < 12)
        {
            throw new InvalidDataException("WAV header is short.");
        }

        if (!data[..4].SequenceEqual(Riff) || !data[8..12].SequenceEqual(Wave))
        {
            throw new InvalidDataException("WAV form is not RIFF.");
        }

        var (channels, sampleRate) = ValidateFormat(FindChunk(data, FmtId));

        if (!TryFindChunk(data, DataId, out var payload))
        {
            throw new InvalidDataException("WAV data chunk is missing.");
        }

        var bytesPerFrame = 2 * channels;
        var frameCount = payload.Length / bytesPerFrame;
        var samples = new float[frameCount];
        for (var frame = 0; frame < frameCount; frame++)
        {
            var left = BinaryPrimitives.ReadInt16LittleEndian(payload[(frame * bytesPerFrame)..]);
            var value = left / 32768d;
            if (channels == 2)
            {
                var right = BinaryPrimitives.ReadInt16LittleEndian(payload[((frame * bytesPerFrame) + 2)..]);
                value = (left + right) / 2d / 32768d;
            }

            samples[frame] = (float)value;
        }

        return AudioResample.To16k(samples, sampleRate);
    }

    private static ReadOnlySpan<byte> FindChunk(ReadOnlySpan<byte> data, ReadOnlySpan<byte> id)
    {
        TryFindChunk(data, id, out var content);
        return content;
    }

    private static bool TryFindChunk(ReadOnlySpan<byte> data, ReadOnlySpan<byte> id, out ReadOnlySpan<byte> content)
    {
        var offset = 12;
        while (offset + 8 <= data.Length)
        {
            var chunkId = data[offset..(offset + 4)];
            var size = BinaryPrimitives.ReadInt32LittleEndian(data[(offset + 4)..]);
            if (size < 0)
            {
                break;
            }

            var available = Math.Min(size, data.Length - offset - 8);
            if (chunkId.SequenceEqual(id))
            {
                content = data.Slice(offset + 8, available);
                return true;
            }

            offset += 8 + size + (size & 1);
        }

        content = ReadOnlySpan<byte>.Empty;
        return false;
    }

    private static (int Channels, int SampleRate) ValidateFormat(ReadOnlySpan<byte> fmt)
    {
        if (fmt.Length < 16)
        {
            throw new InvalidDataException(NotPcmMessage);
        }

        var format = BinaryPrimitives.ReadInt16LittleEndian(fmt);
        var channels = BinaryPrimitives.ReadInt16LittleEndian(fmt[2..]);
        var sampleRate = BinaryPrimitives.ReadInt32LittleEndian(fmt[4..]);
        var bits = BinaryPrimitives.ReadInt16LittleEndian(fmt[14..]);
        if ((format != PcmTag && format != ExtensibleTag)
            || bits != 16
            || (channels != 1 && channels != 2)
            || sampleRate <= 0)
        {
            throw new InvalidDataException(NotPcmMessage);
        }

        if (format == ExtensibleTag)
        {
            if (fmt.Length < 40)
            {
                throw new InvalidDataException(NotPcmMessage);
            }

            var cbSize = BinaryPrimitives.ReadInt16LittleEndian(fmt[16..]);
            if (cbSize < 22 || !fmt[24..40].SequenceEqual(PcmSubFormat))
            {
                throw new InvalidDataException(NotPcmMessage);
            }
        }

        return (channels, sampleRate);
    }
}
