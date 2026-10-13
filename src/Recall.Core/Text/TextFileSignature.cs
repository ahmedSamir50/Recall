namespace Recall.Core.Text;

public static class TextFileSignature
{
    public const int SampleBytes = 512;

    public static bool IsSafeText(ReadOnlySpan<byte> sample)
    {
        if (sample.IsEmpty || HasBlockedMagic(sample))
        {
            return false;
        }

        if (HasUtf16Bom(sample))
        {
            return sample.Length > 2;
        }

        var start = sample;
        if (start.Length >= 3 && start[0] == 0xEF && start[1] == 0xBB && start[2] == 0xBF)
        {
            start = start[3..];
        }

        if (StartsWithAscii(start, "#!") || StartsWithAscii(start, "#requires") || StartsWithAscii(start, "<#"))
        {
            return false;
        }

        var controls = 0;
        foreach (var value in sample)
        {
            if (value == 0)
            {
                return false;
            }

            if (value < 9 || (value > 13 && value < 32))
            {
                controls++;
            }
        }

        return controls * 20 <= sample.Length;
    }

    public static async Task<bool> FileIsSafeTextAsync(string path, CancellationToken cancellationToken)
    {
        var buffer = new byte[SampleBytes];
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, SampleBytes, FileOptions.Asynchronous);
        var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        return IsSafeText(buffer.AsSpan(0, read));
    }

    private static bool HasUtf16Bom(ReadOnlySpan<byte> sample) =>
        sample.Length >= 2 && ((sample[0] == 0xFF && sample[1] == 0xFE) || (sample[0] == 0xFE && sample[1] == 0xFF));

    private static bool StartsWithAscii(ReadOnlySpan<byte> sample, string ascii)
    {
        if (sample.Length < ascii.Length)
        {
            return false;
        }

        for (var i = 0; i < ascii.Length; i++)
        {
            if (sample[i] != ascii[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasBlockedMagic(ReadOnlySpan<byte> sample)
    {
        if (sample.Length >= 2 && sample[0] == (byte)'M' && sample[1] == (byte)'Z')
        {
            return true;
        }

        if (sample.Length >= 4 && sample[0] == 0x7F && sample[1] == (byte)'E' && sample[2] == (byte)'L' && sample[3] == (byte)'F')
        {
            return true;
        }

        if (sample.Length >= 4 && IsMachO(sample))
        {
            return true;
        }

        if (sample.Length >= 4 && sample[0] == 0xCA && sample[1] == 0xFE && sample[2] == 0xBA && sample[3] == 0xBE)
        {
            return true;
        }

        if (sample.Length >= 4 && sample[0] == (byte)'P' && sample[1] == (byte)'K' && sample[2] == 3 && sample[3] == 4)
        {
            return true;
        }

        if (sample.Length >= 4 && sample[0] == 0 && sample[1] == (byte)'a' && sample[2] == (byte)'s' && sample[3] == (byte)'m')
        {
            return true;
        }

        return false;
    }

    private static bool IsMachO(ReadOnlySpan<byte> sample) =>
        (sample[0] == 0xFE && sample[1] == 0xED && sample[2] == 0xFA && (sample[3] == 0xCE || sample[3] == 0xCF))
        || (sample[0] == 0xCE && sample[1] == 0xFA && sample[2] == 0xED && sample[3] == 0xFE)
        || (sample[0] == 0xCF && sample[1] == 0xFA && sample[2] == 0xED && sample[3] == 0xFE);
}
