namespace Recall.Core.Age;

public static class MediaWords
{
    public static string Join(string? ocr, string? transcript, string? keywords)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(ocr))
        {
            parts.Add(ocr);
        }

        if (!string.IsNullOrWhiteSpace(transcript))
        {
            parts.Add(transcript);
        }

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            parts.Add(keywords);
        }

        return parts.Count == 0 ? string.Empty : string.Join('\n', parts);
    }
}
