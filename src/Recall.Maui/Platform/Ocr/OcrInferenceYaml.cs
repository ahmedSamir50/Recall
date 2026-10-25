using System.Text;

namespace Recall.Maui.Platform.Ocr;

internal static class OcrInferenceYaml
{
    private const string DictHeader = "character_dict:";

    private const string EntryPrefix = "  - ";

    public static IReadOnlyList<string> ReadCharacterDict(string yamlPath)
    {
        var dict = new List<string>();
        var inDict = false;
        foreach (var line in File.ReadLines(yamlPath, Encoding.UTF8))
        {
            if (!inDict)
            {
                if (line.Trim() == DictHeader)
                {
                    inDict = true;
                }

                continue;
            }

            if (!line.StartsWith(EntryPrefix, StringComparison.Ordinal))
            {
                break;
            }

            dict.Add(StripQuotes(line[EntryPrefix.Length..]));
        }

        if (dict.Count == 0)
        {
            throw new InvalidOperationException("OCR character dict is empty.");
        }

        return dict;
    }

    private static string StripQuotes(string entry)
    {
        if (entry.Length >= 2
            && ((entry[0] == '\'' && entry[^1] == '\'') || (entry[0] == '"' && entry[^1] == '"')))
        {
            return entry[1..^1];
        }

        return entry;
    }
}
