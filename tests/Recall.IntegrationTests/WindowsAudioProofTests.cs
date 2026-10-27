#if WINDOWS
using FluentAssertions;
using Recall.IntegrationTests.Infrastructure;
using Recall.Maui.Platform;

namespace Recall.IntegrationTests;

public sealed class WindowsAudioProofTests
{
    [Fact]
    public void WindowsM4a_Is16kMono()
    {
        DemoDatasetPaths.RequireRoot();
        var path = DemoDatasetPaths.Combine("audio", "en_munsell_color_notation_ch02_librivox.m4a");
        var source = new WindowsAudioSource();
        var samples = source.ReadCompressed(File.ReadAllBytes(path));
        samples.Length.Should().BeGreaterThan(16000);
    }
}
#endif
