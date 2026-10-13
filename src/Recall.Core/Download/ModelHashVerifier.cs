using System.Security.Cryptography;

namespace Recall.Core.Download;

public static class ModelHashVerifier
{
    public static async Task<string> ComputeSha256HexAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static async Task<bool> MatchesExpectedHashAsync(
        string path,
        string expectedSha256HexLower,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        var actual = await ComputeSha256HexAsync(path, cancellationToken).ConfigureAwait(false);
        return string.Equals(actual, expectedSha256HexLower, StringComparison.OrdinalIgnoreCase);
    }
}
