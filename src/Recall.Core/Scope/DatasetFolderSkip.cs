namespace Recall.Core.Scope;

public static class DatasetFolderSkip
{
    public static IReadOnlyList<string> ImageExtensions { get; } =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic", ".tif", ".tiff", ".bmp",
    ];

    private static readonly HashSet<string> MediaExtensions = CreateMediaExtensions();

    private static HashSet<string> CreateMediaExtensions()
    {
        var extensions = new HashSet<string>(ImageExtensions, StringComparer.OrdinalIgnoreCase);
        extensions.UnionWith([".mp4", ".mov", ".mkv", ".webm", ".avi", ".mp3", ".m4a", ".wav", ".flac", ".ogg", ".aac"]);
        return extensions;
    }

    public static bool ShouldSkip(string folderPath, bool isGrantRoot, IReadOnlyList<string> directChildNames)
    {
        ArgumentNullException.ThrowIfNull(folderPath);
        ArgumentNullException.ThrowIfNull(directChildNames);
        if (isGrantRoot)
        {
            return false;
        }

        var media = 0;
        var machineMade = 0;
        foreach (var name in directChildNames)
        {
            ArgumentNullException.ThrowIfNull(name);
            var extension = Path.GetExtension(name);
            if (!MediaExtensions.Contains(extension))
            {
                continue;
            }

            media++;
            if (IsMachineMade(Path.GetFileNameWithoutExtension(name)))
            {
                machineMade++;
            }
        }

        return media >= 300 && machineMade * 100 >= media * 70;
    }

    public static bool IsMachineMade(string nameWithoutExtension)
    {
        ArgumentNullException.ThrowIfNull(nameWithoutExtension);
        if (nameWithoutExtension.Length == 32 && nameWithoutExtension.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F') && !nameWithoutExtension.Contains('-', StringComparison.Ordinal))
        {
            return true;
        }

        if (Guid.TryParse(nameWithoutExtension, out _))
        {
            return true;
        }

        if (nameWithoutExtension.Length >= 12 && nameWithoutExtension.All(static c => c is >= '0' and <= '9'))
        {
            return true;
        }

        return false;
    }
}
