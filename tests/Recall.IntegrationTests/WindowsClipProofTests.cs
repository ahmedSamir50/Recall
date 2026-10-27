#if WINDOWS
using FluentAssertions;
using Recall.Core.Ingest;
using Recall.IntegrationTests.Infrastructure;
using Recall.Maui.Platform;

namespace Recall.IntegrationTests;

public sealed class WindowsClipProofTests
{
    [Fact]
    public void WindowsClip_Anthem_FramesAtZeroTwoFour()
    {
        if (Environment.OSVersion.Version.Build < FrameCadence.WindowsMinimumBuild)
        {
            var lowBuildSampler = new WindowsVideoFrameSampler();
            var act = () => lowBuildSampler.SampleEveryTwoSeconds(Stream.Null);
            act.Should().Throw<NotSupportedException>().WithMessage(FrameCadence.WindowsMinimumBuildMessage);
            return;
        }

        DemoDatasetPaths.RequireRoot();
        var path = DemoDatasetPaths.Combine("video", "wikipedia_25_anthem_promo.mp4");
        using var stream = File.OpenRead(path);
        var sampler = new WindowsVideoFrameSampler();
        var frames = sampler.SampleEveryTwoSeconds(stream);
        frames.Select(f => f.StartSeconds).Should().Contain(new[] { 0d, 2d, 4d });
    }
}
#endif
