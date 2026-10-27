#if WINDOWS
using FluentAssertions;
using Recall.IntegrationTests.Infrastructure;

namespace Recall.IntegrationTests;

public sealed class PreviewSeekProofTests
{
    [Fact]
    public void Preview_Wav_SeeksToFour()
    {
        DemoDatasetPaths.RequireRoot();
        var path = DemoDatasetPaths.Combine("audio", "en_manhattanhenge_spoken_article_extensible_mono.wav");
        File.Exists(path).Should().BeTrue($"demo wav missing: {path}");

        var plan = MediaPreviewSeekProof.Plan(4, TimeSpan.FromSeconds(10));
        plan.SeekCalled.Should().BeTrue();
        plan.Position.Should().Be(TimeSpan.FromSeconds(4));
        plan.Token.Should().Be(CancellationToken.None);

        var sourcePath = Path.Combine(
            FindRepoRoot(),
            "src",
            "Recall.Maui",
            "Platform",
            "Media",
            "MediaPreviewPage.xaml.cs");
        var source = File.ReadAllText(sourcePath);
        source.Should().Contain("await Player.SeekTo(TimeSpan.FromSeconds(startSeconds), CancellationToken.None)");
        source.Should().Contain("Player.Play()");
        source.Should().NotContain("Player.Position");
        source.Should().Contain("Player.Duration");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Recall.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Recall.slnx not found.");
    }
}
#endif
