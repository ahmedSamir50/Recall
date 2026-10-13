namespace Recall.Core.Text;

public static class GrantTextFiles
{
    private static string[] extensions = TextIngestOptions.DefaultExtensions;

    public static void Use(TextIngestOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        extensions = options.ExtensionList();
    }

    public static bool IsTextFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extensions.Any(candidate => string.Equals(extension, candidate, StringComparison.OrdinalIgnoreCase));
    }
}
