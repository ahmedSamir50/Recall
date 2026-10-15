using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Recall.UnitTests;

public sealed class DocsSiteTests
{
    private static readonly Regex PublicTypeName = new(
        @"^\s*public\s+(?:(?:sealed|static|abstract|partial|readonly|unsafe|ref|new|file)\s+)*(?:record\s+struct|record\s+class|class|record|interface|enum|struct)\s+([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly string[] FeaturePaths =
    [
        "docs/features/screens.md",
        "docs/features/folder-grants.md",
        "docs/features/model-download.md",
        "docs/features/text-embed.md",
        "docs/features/text-chunking.md",
        "docs/features/text-ingest.md",
        "docs/features/text-search.md",
        "docs/features/open-file.md",
        "docs/features/age-cutoff.md",
        "docs/features/sync-stub.md",
        "docs/features/watchers.md",
        "docs/features/vision-and-ocr.md",
        "docs/features/audio-and-transcripts.md",
        "docs/features/code-and-skips.md",
        "docs/features/structure-chunks.md",
        "docs/features/people.md",
        "docs/features/file-details.md",
        "docs/features/local-notes.md",
        "docs/features/proof.md",
    ];

    private static readonly string[] DomainPages =
    [
        "scope.md",
        "device.md",
        "locale.md",
        "theme.md",
        "preferences.md",
        "download.md",
        "text.md",
        "search.md",
        "vector-index.md",
        "sync.md",
        "shell.md",
        "faults.md",
    ];

    private static readonly string[] UseCasePages =
    [
        "onboard-and-grant.md",
        "download-text-model.md",
        "ingest-granted-text.md",
        "find-text.md",
        "open-result.md",
        "rename-device.md",
        "switch-theme-and-language.md",
        "choose-age.md",
        "reset-local-data.md",
        "absent.md",
    ];

    [Fact]
    public void NavPaths_EveryListedMarkdownFile_Exists()
    {
        // Arrange
        var repo = FindRepoRoot();
        var yml = File.ReadAllText(Path.Combine(repo, "mkdocs.yml"));
        var paths = Regex.Matches(yml, @"[\w./-]+\.md")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Act
        var missing = paths
            .Where(path => !File.Exists(Path.Combine(repo, "docs", path.Replace('/', Path.DirectorySeparatorChar))))
            .ToArray();

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void TechnicalDetails_IsRetired_AndDoesNotClaimTheCheckpointDatabaseHoldsCursors()
    {
        // Arrange
        var text = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "technical-details.md"));

        // Act
        var retired = text.Contains("This page is retired.", StringComparison.Ordinal);
        var claimsCursors = text.Contains("That database holds ingestion cursors", StringComparison.Ordinal);

        // Assert
        retired.Should().BeTrue();
        claimsCursors.Should().BeFalse();
    }

    [Fact]
    public void ScreenMap_IsRetired_AndDoesNotSaySearchExecutionIsUnwired()
    {
        // Arrange
        var text = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "screen-map.md"));

        // Act
        var retired = text.Contains("This page is retired.", StringComparison.Ordinal);
        var unwired = text.Contains("Search execution, latency measurement", StringComparison.Ordinal);

        // Assert
        retired.Should().BeTrue();
        unwired.Should().BeFalse();
    }

    [Fact]
    public void ForAgents_NamesEveryFeaturePage()
    {
        // Arrange
        var text = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "for-agents.md"));

        // Act
        var missing = FeaturePaths.Where(path => !text.Contains(path, StringComparison.Ordinal)).ToArray();

        // Assert
        missing.Should().BeEmpty();
        text.Should().Contain("Every file under src, tests, and tools is a heading under docs/code.");
        text.Should().Contain("A domain owns one reason to change.");
    }

    [Fact]
    public void ExtensionDecisions_StayOpen_AndDoNotClaimTheyRun()
    {
        // Arrange
        var repo = FindRepoRoot();
        var structure = File.ReadAllText(Path.Combine(repo, "docs", "features", "structure-chunks.md"));
        var people = File.ReadAllText(Path.Combine(repo, "docs", "features", "people.md"));
        var details = File.ReadAllText(Path.Combine(repo, "docs", "features", "file-details.md"));
        var notes = File.ReadAllText(Path.Combine(repo, "docs", "features", "local-notes.md"));
        var nonGoals = File.ReadAllText(Path.Combine(repo, "docs", "non-goals.md"));
        var canon = File.ReadAllText(Path.Combine(repo, ".cursor", "skills", "recall-expert-harness", "canon.md"));

        // Act
        var open = structure.Contains("Status: open", StringComparison.Ordinal)
            && people.Contains("Status: open", StringComparison.Ordinal)
            && details.Contains("Status: open", StringComparison.Ordinal)
            && notes.Contains("Status: open", StringComparison.Ordinal);

        // Assert
        open.Should().BeTrue();
        structure.Should().Contain("Chunks are not averaged.");
        people.Should().Contain("A stored face vector at 128 numbers is forbidden.");
        details.Should().Contain("Recall does not send the place to a server.");
        notes.Should().Contain("v1 has no language model.");
        nonGoals.Should().Contain("No language model in v1.");
        canon.Should().Contain("Zero LLM in v1.");
        canon.Should().NotContain("in v1 or v1.5");
    }

    [Fact]
    public void DomainIndex_NamesEveryDomainPage()
    {
        // Arrange
        var text = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "domains", "index.md"));

        // Act
        var missing = DomainPages.Where(page => !text.Contains(page, StringComparison.Ordinal)).ToArray();

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void UseCaseIndex_NamesEveryUseCasePage()
    {
        // Arrange
        var text = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "use-cases", "index.md"));

        // Act
        var missing = UseCasePages.Where(page => !text.Contains(page, StringComparison.Ordinal)).ToArray();

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void EverySourceFile_HasACatalogHeading()
    {
        // Arrange
        var repo = FindRepoRoot();
        var sections = CatalogSections(repo);

        // Act
        var missing = SourceFiles(repo).Where(path => !sections.ContainsKey(path)).ToArray();

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void CatalogSection_RepeatsPublicTypeName()
    {
        // Arrange
        var repo = FindRepoRoot();
        var sections = CatalogSections(repo);
        var missing = new List<string>();

        // Act
        foreach (var relative in SourceFiles(repo).Where(path => path.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var source = File.ReadAllText(Path.Combine(repo, relative.Replace('/', Path.DirectorySeparatorChar)));
            var names = PublicTypeName.Matches(source).Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal).ToArray();
            var section = sections[relative];
            if (names.Length == 0)
            {
                var recorded = section.Contains("No public type.", StringComparison.Ordinal);
                missing.AddRange(recorded ? [] : [relative + ": No public type."]);
                continue;
            }

            missing.AddRange(names.Where(name => !section.Contains(name, StringComparison.Ordinal)).Select(name => relative + ": " + name));
        }

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void CatalogSection_QuotesBranchLines()
    {
        // Arrange
        var repo = FindRepoRoot();
        var sections = CatalogSections(repo);
        var gaps = new List<string>();

        // Act
        foreach (var relative in SourceFiles(repo).Where(path => path.EndsWith(".cs", StringComparison.Ordinal)))
        {
            var section = sections[relative];
            foreach (var line in File.ReadLines(Path.Combine(repo, relative.Replace('/', Path.DirectorySeparatorChar))))
            {
                var trim = line.Trim();
                var branch = trim.StartsWith("else if (", StringComparison.Ordinal)
                    || trim.StartsWith("if (", StringComparison.Ordinal)
                    || trim.StartsWith("catch (", StringComparison.Ordinal);
                if (branch && !section.Contains(trim, StringComparison.Ordinal))
                {
                    gaps.Add(relative + ": " + trim);
                }
            }
        }

        // Assert
        gaps.Should().BeEmpty();
    }

    [Fact]
    public void Plan_Version15_FollowsTheV1Shape_AndStaysOutOfTheV1Total()
    {
        // Arrange
        var repo = FindRepoRoot();
        var plan = File.ReadAllText(Path.Combine(repo, "docs", "plan.md"));
        var implementation = File.ReadAllText(Path.Combine(repo, "InitDocs", "Recall_App_Implementation_Plan.md"));
        var technical = File.ReadAllText(Path.Combine(repo, "InitDocs", "Recall_App_Technical_Details.md"));

        // Act
        var versionHeading = plan.IndexOf("# Version 1.5", StringComparison.Ordinal);
        var allEpics = plan.IndexOf("**All epics**", StringComparison.Ordinal);
        var allTasks = plan.IndexOf("**All v1.5 tasks**", StringComparison.Ordinal);
        var afterTasks = allTasks < 0 ? string.Empty : plan[allTasks..];

        // Assert
        plan.Should().Contain("# Version 1.5");
        plan.Should().Contain("## Phase 1 — Sections");
        implementation.Should().Contain("# Version 1.5");
        implementation.Should().Contain("## Phase 1 — Sections");
        technical.Should().Contain("# Version 1.5");
        technical.Should().Contain("## Phase 1 — Sections");
        versionHeading.Should().BeGreaterThan(allEpics);
        afterTasks.Should().Contain("not-started");
        afterTasks.IndexOf("0%", StringComparison.Ordinal).Should().BeGreaterThan(afterTasks.IndexOf("not-started", StringComparison.Ordinal));
        implementation.Should().Contain("History. The living plan is docs/plan.md.");
        technical.Should().Contain("History. The living plan is docs/plan.md.");
    }

    private static Dictionary<string, string> CatalogSections(string repo)
    {
        var text = new StringBuilder();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(repo, "docs", "code"), "*.md"))
        {
            text.AppendLine(File.ReadAllText(file));
        }

        var catalog = text.ToString();
        var matches = Regex.Matches(catalog, @"^## ((?:src|tests|tools)/\S+)\s*$", RegexOptions.Multiline);
        var sections = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index + matches[i].Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : catalog.Length;
            sections[matches[i].Groups[1].Value] = catalog[start..end];
        }

        return sections;
    }

    private static IEnumerable<string> SourceFiles(string repo)
    {
        foreach (var top in new[] { "src", "tests", "tools" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(repo, top), "*.*", SearchOption.AllDirectories))
            {
                if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    && !file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(repo, file).Replace('\\', '/');
                if (relative.Contains("/bin/", StringComparison.Ordinal) || relative.Contains("/obj/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (relative == "tests/Recall.UnitTests/DocsSiteTests.cs")
                {
                    continue;
                }

                yield return relative;
            }
        }
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

        throw new InvalidOperationException("Recall.slnx was not found above the test output directory.");
    }
}
