namespace Recall.Core.Text;

public sealed class TextIngestOptions
{
    public const string SectionName = "TextIngest";

    public static readonly string[] DefaultExtensions =
    [
        ".txt", ".md", ".markdown", ".csv", ".json", ".xml", ".html", ".htm",
    ];

    public string Extensions { get; set; } = string.Join(',', DefaultExtensions);

    public string[] ExtensionList()
    {
        var parts = Extensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? DefaultExtensions : parts;
    }
}
