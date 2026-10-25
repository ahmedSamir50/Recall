#if WINDOWS
using FluentAssertions;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Search;
using Recall.Maui.Platform;
using Recall.Maui.Platform.Files;
using Recall.Maui.Platform.Ocr;

namespace Recall.Maui.UnitTests;

public sealed class CloseOpenProofsTests
{
    [Fact]
    public void WindowsClip_FiveSeconds_JpegAtZeroTwoFour()
    {
        if (Environment.OSVersion.Version.Build < FrameCadence.WindowsMinimumBuild)
        {
            var lowBuildSampler = new WindowsVideoFrameSampler();
            var act = () => lowBuildSampler.SampleEveryTwoSeconds(Stream.Null);
            act.Should().Throw<NotSupportedException>().WithMessage(FrameCadence.WindowsMinimumBuildMessage);
            return;
        }

        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "five-seconds.mp4");
        using var stream = File.OpenRead(path);
        var sampler = new WindowsVideoFrameSampler();
        var frames = sampler.SampleEveryTwoSeconds(stream);
        frames.Should().HaveCount(3);
        frames.Select(f => f.StartSeconds).Should().Equal(0d, 2d, 4d);
        foreach (var frame in frames)
        {
            frame.Jpeg.Take(3).Should().Equal(255, 216, 255);
        }
    }

    [Fact]
    public void WindowsCompressed_Tone_Is16kHz()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tone.m4a");
        var source = new WindowsAudioSource();
        var samples = source.ReadCompressed(File.ReadAllBytes(path));
        samples.Length.Should().BeGreaterThan(16000);
    }

    [Fact]
    public void WindowsSoundtrack_Talk_Is16kHz()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "talk.mp4");
        var source = new WindowsAudioSource();
        var samples = source.ReadCompressed(File.ReadAllBytes(path));
        samples.Length.Should().BeGreaterThan(16000);
    }

    [Fact]
    public async Task OpenAt_AudioStartFour_CallsPreview()
    {
        var preview = new RecordingPreview();
        var opener = new MauiLocalFileOpener(preview);
        var result = await opener.OpenAtAsync("talk.m4a", 4, TestContext.Current.CancellationToken);
        preview.StartSeconds.Should().Be(4);
        result.Should().Be(LocalFileOpenResult.Opened);
    }

    [Fact]
    public async Task OpenAt_StartZero_UsesLauncher()
    {
        var preview = new RecordingPreview();
        var opener = new MauiLocalFileOpener(preview);
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "tone.m4a");
        _ = await opener.OpenAtAsync(path, 0, TestContext.Current.CancellationToken);
        preview.Calls.Should().Be(0);
    }

    [Fact]
    public async Task OpenAt_MissingFile_DoesNotPreview()
    {
        var preview = new RecordingPreview();
        var opener = new MauiLocalFileOpener(preview);
        var result = await opener.OpenAtAsync("missing-talk.m4a", 4, TestContext.Current.CancellationToken);
        result.Should().Be(LocalFileOpenResult.Missing);
        preview.Calls.Should().Be(0);
    }

    [Fact]
    public async Task OpenAt_NullPath_Throws()
    {
        var opener = new MauiLocalFileOpener(new RecordingPreview());
        var act = async () => await opener.OpenAtAsync(null!, 0, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void PdfRender_PageZero_Throws()
    {
        var act = () => WindowsPdfPageRenderer.Render("sample.pdf", 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PdfRender_Null_Throws()
    {
        var act = () => WindowsPdfPageRenderer.Render(null!, 1);
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class RecordingPreview : IMediaPreview
    {
        public int Calls { get; private set; }

        public double? StartSeconds { get; private set; }

        public Task PlayAsync(string path, double startSeconds, CancellationToken cancellationToken = default)
        {
            Calls++;
            StartSeconds = startSeconds;
            return Task.CompletedTask;
        }
    }
}
#endif
