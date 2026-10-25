using System.Text;
using System.Text.RegularExpressions;

namespace Recall.Maui.Platform.Ocr;

internal static class OcrInferenceYaml
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    public static IReadOnlyList<string> ReadCharacterDict(string yamlPath)
    {
        var text = File.ReadAllText(yamlPath, Encoding.UTF8);
        var match = Regex.Match(
            text,
            @"character_dict\s*:\s*\[(.*?)\]",
            RegexOptions.Singleline,
            RegexTimeout);
        if (!match.Success)
        {
            return [];
        }

        var dict = new List<string>();
        var tokens = Regex.Matches(match.Groups[1].Value, @"'((?:\\'|[^'])*)'|""((?:\\""|[^""])*)""", RegexOptions.None, RegexTimeout);
        for (var index = 0; index < tokens.Count; index++)
        {
            var item = tokens[index];
            if (item.Groups[1].Success)
            {
                dict.Add(item.Groups[1].Value.Replace("\\'", "'", StringComparison.Ordinal));
            }
            else if (item.Groups[2].Success)
            {
                dict.Add(item.Groups[2].Value.Replace("\\\"", "\"", StringComparison.Ordinal));
            }
        }

        return dict;
    }
}
