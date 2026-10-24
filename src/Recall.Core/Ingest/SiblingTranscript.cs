using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Recall.Core.Ingest;

public static class SiblingTranscript
{
    public const bool ReadsEmbeddedTrack = false;

    private static readonly string[] CaptionExtensions =
    [
        ".srt", ".vtt", ".sbv", ".ass", ".ssa", ".ttml",
    ];

    private static readonly Regex LanguageTag = new(
        @"^[A-Za-z]{2,3}(-[A-Za-z]{2,8})?$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex SbvTimestamp = new(
        @"^\d+:\d\d:\d\d\.\d\d\d,\d+:\d\d:\d\d\.\d\d\d$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex OverrideBlock = new(
        @"\{[^{}]*\}",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Comparer<string> Utf8ByteComparer = Comparer<string>.Create(CompareUtf8Bytes);

    public static bool IsCaptionFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        return MatchesCaption(Path.GetExtension(fileName));
    }

    public static bool Matches(string mediaFileName, string candidateFileName)
    {
        if (string.IsNullOrEmpty(mediaFileName) || string.IsNullOrEmpty(candidateFileName))
        {
            return false;
        }

        if (!IsCaptionFile(candidateFileName))
        {
            return false;
        }

        var mediaStem = Path.GetFileNameWithoutExtension(mediaFileName);
        var candidateStem = Path.GetFileNameWithoutExtension(candidateFileName);
        if (string.Equals(candidateStem, mediaStem, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var tagDot = candidateStem.LastIndexOf('.');
        if (tagDot < 0)
        {
            return false;
        }

        var baseStem = candidateStem[..tagDot];
        if (!string.Equals(baseStem, mediaStem, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return LanguageTag.IsMatch(candidateStem[(tagDot + 1)..]);
    }

    public static IReadOnlyList<string> Order(IReadOnlyList<string> fileNames)
    {
        if (fileNames is null)
        {
            throw new ArgumentNullException(nameof(fileNames));
        }

        return fileNames.OrderBy(static name => name, Utf8ByteComparer).ToArray();
    }

    public static string Join(IReadOnlyList<string> orderedTexts)
    {
        if (orderedTexts is null)
        {
            throw new ArgumentNullException(nameof(orderedTexts));
        }

        return string.Join("\n", orderedTexts);
    }

    public static string ExtractCues(string extension, string raw)
    {
        if (raw is null)
        {
            throw new ArgumentNullException(nameof(raw));
        }

        var normalized = extension is null ? string.Empty : extension.ToLowerInvariant();
        return normalized switch
        {
            ".srt" => FromSrt(raw),
            ".vtt" => FromVtt(raw),
            ".sbv" => FromSbv(raw),
            ".ass" => FromAss(raw),
            ".ssa" => FromAss(raw),
            ".ttml" => FromTtml(raw),
            _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, "Unknown transcript extension."),
        };
    }

    private static bool MatchesCaption(string extension) =>
        CaptionExtensions.Any(candidate => string.Equals(candidate, extension, StringComparison.OrdinalIgnoreCase));

    private static string FromSrt(string raw)
    {
        var kept = new List<string>();
        foreach (var line in Lines(raw))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || IsAllDigits(trimmed) || trimmed.Contains("-->", StringComparison.Ordinal))
            {
                continue;
            }

            kept.Add(line);
        }

        return string.Join("\n", kept);
    }

    private static string FromVtt(string raw)
    {
        var kept = new List<string>();
        var skipBlock = false;
        foreach (var line in Lines(raw))
        {
            if (line.TrimStart().StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.TrimStart().StartsWith("NOTE", StringComparison.OrdinalIgnoreCase)
                || line.TrimStart().StartsWith("STYLE", StringComparison.OrdinalIgnoreCase)
                || line.TrimStart().StartsWith("REGION", StringComparison.OrdinalIgnoreCase))
            {
                skipBlock = true;
                continue;
            }

            if (skipBlock)
            {
                if (line.Trim().Length == 0)
                {
                    skipBlock = false;
                }

                continue;
            }

            if (line.Trim().Length == 0 || line.Contains("-->", StringComparison.Ordinal))
            {
                continue;
            }

            kept.Add(line);
        }

        return string.Join("\n", kept);
    }

    private static string FromSbv(string raw)
    {
        var kept = new List<string>();
        foreach (var line in Lines(raw))
        {
            if (line.Trim().Length == 0 || SbvTimestamp.IsMatch(line.Trim()))
            {
                continue;
            }

            kept.Add(line);
        }

        return string.Join("\n", kept);
    }

    private static string FromAss(string raw)
    {
        var kept = new List<string>();
        foreach (var line in Lines(raw))
        {
            if (!line.TrimStart().StartsWith("Dialogue:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fields = line.TrimStart().Split(',');
            if (fields.Length <= 9)
            {
                continue;
            }

            var text = OverrideBlock.Replace(string.Join(',', fields[9..]), string.Empty);
            kept.Add(text);
        }

        return string.Join("\n", kept);
    }

    private static string FromTtml(string raw)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(raw);
        }
        catch (XmlException)
        {
            return string.Empty;
        }

        var paragraphs = document.Descendants()
            .Where(static element => element.Name.LocalName == "p")
            .Select(static element => element.Value.Trim());
        return string.Join("\n", paragraphs);
    }

    private static string[] Lines(string raw) =>
        raw.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static bool IsAllDigits(string value) =>
        value.All(static c => c is >= '0' and <= '9');

    private static int CompareUtf8Bytes(string? left, string? right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var first = Encoding.UTF8.GetBytes(left);
        var second = Encoding.UTF8.GetBytes(right);
        var shared = Math.Min(first.Length, second.Length);
        for (var index = 0; index < shared; index++)
        {
            if (first[index] != second[index])
            {
                return first[index] - second[index];
            }
        }

        return first.Length - second.Length;
    }
}
