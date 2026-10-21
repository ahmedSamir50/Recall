namespace Recall.Core.Indexing;

public static class MediaCollections
{
    public const string Image = "items_image";

    public const string Video = "items_video";

    public const int Width = 768;

    public static bool IsMedia(string collectionName) =>
        string.Equals(collectionName, Image, StringComparison.Ordinal)
        || string.Equals(collectionName, Video, StringComparison.Ordinal);
}
