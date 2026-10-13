using System.Security.Cryptography;
using System.Text;
using Recall.Core.Vocabulary;

namespace Recall.Core.Text;

public static class ContentIdBuilder
{
    public static string Build(Modality modality, string sourceContentHash, StoredWidth width, int chunkOrdinal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceContentHash);
        if (chunkOrdinal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkOrdinal));
        }

        var preimage = $"{(int)modality}|{sourceContentHash}|{(int)width}|{chunkOrdinal}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(preimage));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string HashSourceContent(string rawText)
    {
        ArgumentNullException.ThrowIfNull(rawText);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawText));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
