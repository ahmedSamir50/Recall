using System.Security.Cryptography;
using Recall.Core.Text;
using Recall.Core.Vocabulary;

namespace Recall.Core.Indexing;

public sealed record MediaIndexInsert(
    string ContentId,
    string SourcePath,
    string FileName,
    string Words,
    ReadOnlyMemory<float> Vector768,
    string EncoderHash,
    bool PathLive,
    string GrantPath,
    string StartSeconds,
    string EndSeconds)
{
    public static MediaIndexInsert Picture(
        ReadOnlySpan<byte> fileBytes,
        string sourcePath,
        string words,
        ReadOnlyMemory<float> vector768,
        string encoderHash,
        bool pathLive,
        string grantPath)
    {
        return new(
            ContentIdBuilder.Build(Modality.Image, HashFile(fileBytes), StoredWidth.W768, 0),
            sourcePath,
            Path.GetFileName(sourcePath),
            words,
            vector768,
            encoderHash,
            pathLive,
            grantPath,
            string.Empty,
            string.Empty);
    }

    public static MediaIndexInsert VideoFrame(
        ReadOnlySpan<byte> fileBytes,
        string sourcePath,
        string words,
        ReadOnlyMemory<float> vector768,
        string encoderHash,
        bool pathLive,
        string grantPath,
        int frameOrdinal,
        string startSeconds,
        string endSeconds)
    {
        return new(
            ContentIdBuilder.Build(Modality.Video, HashFile(fileBytes), StoredWidth.W768, frameOrdinal),
            sourcePath,
            Path.GetFileName(sourcePath),
            words,
            vector768,
            encoderHash,
            pathLive,
            grantPath,
            startSeconds,
            endSeconds);
    }

    private static string HashFile(ReadOnlySpan<byte> fileBytes) =>
        Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant();
}
