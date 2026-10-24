using FluentAssertions;
using Recall.Core.Download;
using Recall.Core.Preferences;
using Recall.Core.Text;
using Recall.UI.Pages;

namespace Recall.UnitTests;

public sealed class Phase2CloseTests
{
    private static string RepoRoot => FindRepoRoot();

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

    [Fact]
    public void MarkVerifying_SetsPhaseAndClearsFault()
    {
        var input = new TextModelDownloadSnapshot(TextModelDownloadPhase.Downloading, 10, TextModelDownloadFault.Transport);
        var result = TextModelDownloadUiPolicy.MarkVerifying(input);
        result.Phase.Should().Be(TextModelDownloadPhase.Verifying);
        result.Fault.Should().BeNull();
    }

    [Fact]
    public void Plan_UnderAim_IsOnePatch()
    {
        var patches = DocumentPatchPlanner.Plan("Notes", "mars", null, 0, Enumerable.Range(1, 5).ToList());
        patches.Should().HaveCount(1);
        patches[0].Ordinal.Should().Be(0);
        patches[0].Raw.Should().Be("mars");
    }

    [Fact]
    public void Plan_BlankContent_Throws()
    {
        var act = () => DocumentPatchPlanner.Plan("Notes", " ", null, 0, Enumerable.Range(1, 5).ToList());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Plan_NullEnds_Throws()
    {
        var act = () => DocumentPatchPlanner.Plan("Notes", "mars", null, 0, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MauiProject_ContainsNoOnnxWeights()
    {
        var mauiRoot = Path.Combine(RepoRoot, "src", "Recall.Maui");
        var files = Directory.EnumerateFiles(mauiRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .ToList();
        files.Should().OnlyContain(path =>
            !path.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase)
            && !path.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase));
        var csproj = File.ReadAllText(Path.Combine(mauiRoot, "Recall.Maui.csproj"));
        csproj.Should().NotContain(".onnx");
        csproj.Should().NotContain(".gguf");
    }

    [Fact]
    public void HomeRazor_OmitsDemoCards()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Pages", "Find", "Home.razor"));
        text.Should().NotContain("Home_Card1Title");
        text.Should().NotContain("Home_QueryDemo");
        text.Should().NotContain("Home_LatencyDemo");
        text.Should().NotContain("Home_ResultsSub");
        text.Should().NotContain("Home_Card2Translated");
        text.Should().NotContain("Home_MatchDemo");
    }

    [Fact]
    public void AppCss_DefinesFillHeight()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "wwwroot", "css", "app.css"));
        text.Should().Contain(".h-full{height:100%}");
        text.Should().Contain(".transition-bar{height:100%");
    }

    [Fact]
    public void AppCss_BottomSafeUsesPublishedInset()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "wwwroot", "css", "app.css"));
        text.Should().Contain(".pb-safe{padding-bottom:max(env(safe-area-inset-bottom, 0px), var(--safe-bottom, 0px))}");
    }

    [Fact]
    public void Solution_DoesNotReferenceMudBlazor()
    {
        var paths = new[]
        {
            Path.Combine(RepoRoot, "src", "Recall.UI", "Recall.UI.csproj"),
            Path.Combine(RepoRoot, "src", "Recall.Maui", "Recall.Maui.csproj"),
            Path.Combine(RepoRoot, "Directory.Packages.props"),
            Path.Combine(RepoRoot, "src", "Recall.Maui", "MauiProgram.cs"),
            Path.Combine(RepoRoot, "src", "Recall.Maui", "wwwroot", "index.html"),
            Path.Combine(RepoRoot, "src", "Recall.UI", "_Imports.razor"),
        };
        foreach (var path in paths)
        {
            File.ReadAllText(path).Should().NotContain("MudBlazor");
        }

        File.Exists(Path.Combine(RepoRoot, "src", "Recall.UI", "Theme", "RecallSynapticMudTheme.cs")).Should().BeFalse();
    }

    [Fact]
    public void ThemeChoiceClass_SystemSelected_IsPrimary() =>
        Preferences.ThemeChoiceClass(ThemePreference.System, ThemePreference.System).Should().Be("btn-p");

    [Fact]
    public void ThemeChoiceClass_SystemSelected_DarkIsGhost() =>
        Preferences.ThemeChoiceClass(ThemePreference.System, ThemePreference.Dark).Should().Be("btn-g");

    [Fact]
    public void ThemeChoiceClass_DarkSelected_IsPrimary() =>
        Preferences.ThemeChoiceClass(ThemePreference.Dark, ThemePreference.Dark).Should().Be("btn-p");

    [Fact]
    public void ThemeChoiceClass_DarkSelected_LightIsGhost() =>
        Preferences.ThemeChoiceClass(ThemePreference.Dark, ThemePreference.Light).Should().Be("btn-g");

    [Fact]
    public void ThemeChoiceClass_LightSelected_IsPrimary() =>
        Preferences.ThemeChoiceClass(ThemePreference.Light, ThemePreference.Light).Should().Be("btn-p");

    [Fact]
    public void ThemeChoiceClass_LightSelected_SystemIsGhost() =>
        Preferences.ThemeChoiceClass(ThemePreference.Light, ThemePreference.System).Should().Be("btn-g");
}
