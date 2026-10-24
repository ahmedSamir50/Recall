namespace Recall.Core.Scope;

public static class MediaFileKinds
{
    public const string AudioAccept = ".mp3,.m4a,.wav,.flac,.ogg,.aac";

    private static readonly string[] AudioExtensions =
    [
        ".mp3", ".m4a", ".wav", ".flac", ".ogg", ".aac",
    ];

    private static readonly string[] VideoExtensions =
    [
        ".mp4", ".mov", ".mkv", ".webm", ".avi",
    ];

    private static readonly string[] ImageExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic", ".tif", ".tiff", ".bmp",
    ];

    public static bool IsAudio(string fileName) => HasExtension(fileName, AudioExtensions);

    public static bool IsVideo(string fileName) => HasExtension(fileName, VideoExtensions);

    public static bool IsImage(string fileName) => HasExtension(fileName, ImageExtensions);

    private static bool HasExtension(string? fileName, string[] extensions)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (extension.Length == 0)
        {
            return false;
        }

        return extensions.Any(candidate => string.Equals(candidate, extension, StringComparison.OrdinalIgnoreCase));
    }
}
