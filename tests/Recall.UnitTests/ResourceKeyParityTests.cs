using System.Xml.Linq;
using FluentAssertions;

namespace Recall.UnitTests;

public sealed class ResourceKeyParityTests
{
    [Fact]
    public void StringsResx_ArabicAndEnglish_HaveTheSameKeys()
    {
        // Arrange
        var root = FindRepoRoot();
        var enPath = Path.Combine(root, "src", "Recall.UI", "Resources", "Strings.resx");
        var arPath = Path.Combine(root, "src", "Recall.UI", "Resources", "Strings.ar.resx");

        // Act
        var enKeys = ReadKeys(enPath);
        var arKeys = ReadKeys(arPath);

        // Assert
        enKeys.Should().BeEquivalentTo(arKeys);
        enKeys.Should().NotBeEmpty();
    }

    private static HashSet<string> ReadKeys(string path)
    {
        var document = XDocument.Load(path);
        return document.Root!
            .Elements("data")
            .Select(element => element.Attribute("name")!.Value)
            .ToHashSet(StringComparer.Ordinal);
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

        throw new InvalidOperationException("Recall.slnx was not found.");
    }
}
