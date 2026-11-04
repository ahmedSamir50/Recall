using FluentAssertions;

namespace Recall.UnitTests;

public sealed class NoSkippedOcrProofsTests
{
    [Fact]
    public void OcrProofFiles_HaveNoSkip()
    {
        var root = FindRepoRoot();
        string[] files =
        [
            Path.Combine(root, "tests", "Recall.IntegrationTests", "OcrGoldenReportTests.cs"),
            Path.Combine(root, "tests", "Recall.IntegrationTests", "OcrDemoProofTests.cs"),
        ];

        var offenders = new List<string>();
        foreach (var file in files)
        {
            File.Exists(file).Should().BeTrue($"the OCR proof file must exist: {file}");
            var text = File.ReadAllText(file);
            var count = CountOccurrences(text, "Skip =") + CountOccurrences(text, "Skip=");
            if (count > 0)
            {
                offenders.Add($"{Path.GetFileName(file)}: {count}");
            }
        }

        offenders.Should().BeEmpty("an OCR or golden proof never skips; a missing input fails with the fetch command named");
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
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

        throw new InvalidOperationException($"Recall.slnx not found above {AppContext.BaseDirectory}.");
    }
}
