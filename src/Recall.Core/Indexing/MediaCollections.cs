using Recall.Core.Ports;

namespace Recall.Core.Indexing;

public static class MediaCollections
{
    public const string Image = "items_image";

    public const string Video = "items_video";

    public const string Audio = "items_audio";

    public const string ImageOld = "items_image_old";

    public const string VideoOld = "items_video_old";

    public const string AudioOld = "items_audio_old";

    public const int Width = 768;

    public const int OldWidth = 512;

    public static readonly string[] PictureCollections = [Image, Video, ImageOld, VideoOld];

    public static readonly string[] AudioCollections = [Audio, AudioOld];

    public static readonly string[] WordCollections = [Image, Video, Audio, ImageOld, VideoOld, AudioOld];

    public static bool IsMedia(string collectionName) =>
        string.Equals(collectionName, Image, StringComparison.Ordinal)
        || string.Equals(collectionName, Video, StringComparison.Ordinal)
        || string.Equals(collectionName, Audio, StringComparison.Ordinal)
        || string.Equals(collectionName, ImageOld, StringComparison.Ordinal)
        || string.Equals(collectionName, VideoOld, StringComparison.Ordinal)
        || string.Equals(collectionName, AudioOld, StringComparison.Ordinal);

    public static int WidthFor(string collectionName)
    {
        if (string.Equals(collectionName, Image, StringComparison.Ordinal)
            || string.Equals(collectionName, Video, StringComparison.Ordinal)
            || string.Equals(collectionName, Audio, StringComparison.Ordinal))
        {
            return Width;
        }

        if (string.Equals(collectionName, ImageOld, StringComparison.Ordinal)
            || string.Equals(collectionName, VideoOld, StringComparison.Ordinal)
            || string.Equals(collectionName, AudioOld, StringComparison.Ordinal))
        {
            return OldWidth;
        }

        throw new ArgumentException("Unknown media collection name.", nameof(collectionName));
    }

    public static string EncoderHashFor(string collectionName, IEncoderFileHash hashes)
    {
        if (string.Equals(collectionName, Image, StringComparison.Ordinal)
            || string.Equals(collectionName, Video, StringComparison.Ordinal)
            || string.Equals(collectionName, ImageOld, StringComparison.Ordinal)
            || string.Equals(collectionName, VideoOld, StringComparison.Ordinal))
        {
            return hashes.VisionHash ?? string.Empty;
        }

        if (string.Equals(collectionName, Audio, StringComparison.Ordinal)
            || string.Equals(collectionName, AudioOld, StringComparison.Ordinal))
        {
            return hashes.AudioHash ?? string.Empty;
        }

        throw new ArgumentException("Unknown media collection name.", nameof(collectionName));
    }
}
