#if WINDOWS
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Recall.Maui.Platform.Ocr;

namespace Recall.IntegrationTests;

internal readonly record struct OcrGoldenPage(byte[] Png, string GroundTruth, bool IsPhone, string Language);

internal static class OcrGoldenCatalog
{
    public const string ManifestName = "manifest.csv";

    public const string RootVariable = "RECALL_OCR_GOLDEN";

    public const string FetchCommand = "tools/fetch-ocr-golden.ps1";

    public static string Root
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable(RootVariable);
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            return Path.GetFullPath(Path.Combine(FindRepoRoot(), "artifacts", "ocr-golden"));
        }
    }

    public static IReadOnlyList<OcrGoldenPage> Build()
    {
        var root = Root;
        var manifest = Path.Combine(root, ManifestName);
        if (!File.Exists(manifest))
        {
            throw new InvalidOperationException($"OCR golden corpus missing at {root}; run {FetchCommand}");
        }

        var pages = new List<OcrGoldenPage>();
        var header = true;
        foreach (var line in File.ReadLines(manifest))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (header)
            {
                header = false;
                continue;
            }

            var fields = SplitCsv(line);
            if (fields.Count < 6)
            {
                throw new InvalidOperationException($"OCR golden manifest row is short: {line}");
            }

            var relative = fields[0];
            var language = fields[1];
            var isPhone = bool.Parse(fields[2]);
            var sourceUrl = fields[4];
            var expectedHash = fields[5];
            var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"OCR golden page missing at {path}; run {FetchCommand}");
            }

            var groundTruthPath = Path.ChangeExtension(path, ".gt.txt");
            if (!File.Exists(groundTruthPath))
            {
                throw new InvalidOperationException($"OCR golden ground truth missing at {groundTruthPath}; run {FetchCommand}");
            }

            var bytes = File.ReadAllBytes(path);
            var actualHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"OCR golden page {relative} sha256 {actualHash} does not match the manifest {expectedHash} from {sourceUrl}; run {FetchCommand}");
            }

            var png = path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                ? WindowsPdfPageRenderer.Render(path, 1)
                : bytes;
            pages.Add(new OcrGoldenPage(png, File.ReadAllText(groundTruthPath).Trim(), isPhone, language));
        }

        return pages;
    }

    private static List<string> SplitCsv(string line)
    {
        var fields = new List<string>();
        var builder = new StringBuilder();
        var quoted = false;
        foreach (var character in line)
        {
            if (character == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (character == ',' && !quoted)
            {
                fields.Add(builder.ToString());
                builder.Clear();
                continue;
            }

            builder.Append(character);
        }

        fields.Add(builder.ToString());
        return fields;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Recall.slnx not found above {AppContext.BaseDirectory}; set {RootVariable} to the corpus root.");
    }
}
#endif
