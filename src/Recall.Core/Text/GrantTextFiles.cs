namespace Recall.Core.Text;

public static class GrantTextFiles
{
    private static readonly string[] Extensions =
    [
        ".txt", ".md", ".markdown", ".csv", ".json", ".xml", ".html", ".htm", ".log",
    ];

    public static bool IsTextFile(string path)
    {
        var extension = Path.GetExtension(path);
        return Extensions.Any(candidate => string.Equals(extension, candidate, StringComparison.OrdinalIgnoreCase));
    }
}
