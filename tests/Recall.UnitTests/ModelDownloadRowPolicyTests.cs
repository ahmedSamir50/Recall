using FluentAssertions;
using Recall.Core.Download;
using Recall.Core.Ocr;
using Recall.Core.Ingest;
using Recall.Core.Scope;
using Recall.Core.Vocabulary;
using Recall.UI.Layout;

namespace Recall.UnitTests;

public sealed class ModelDownloadRowPolicyTests
{
    [Fact]
    public void SelectSnapshot_Text_ReturnsText()
    {
        var text = new TextModelDownloadSnapshot(TextModelDownloadPhase.Ready, 0, null);
        var vision = new TextModelDownloadSnapshot(TextModelDownloadPhase.Downloading, 0, null);
        var audio = new TextModelDownloadSnapshot(TextModelDownloadPhase.NotStarted, 0, null);
        var helpers = new TextModelDownloadSnapshot(TextModelDownloadPhase.Verifying, 0, null);
        TextModelDownloadUiPolicy.SelectSnapshot(ModelDownloadRowKind.Text, text, vision, audio, helpers).Should().BeSameAs(text);
    }

    [Fact]
    public void SelectSnapshot_Vision_ReturnsVision()
    {
        var text = new TextModelDownloadSnapshot(TextModelDownloadPhase.Ready, 0, null);
        var vision = new TextModelDownloadSnapshot(TextModelDownloadPhase.Downloading, 0, null);
        var audio = new TextModelDownloadSnapshot(TextModelDownloadPhase.NotStarted, 0, null);
        var helpers = new TextModelDownloadSnapshot(TextModelDownloadPhase.Verifying, 0, null);
        TextModelDownloadUiPolicy.SelectSnapshot(ModelDownloadRowKind.Vision, text, vision, audio, helpers).Should().BeSameAs(vision);
    }

    [Fact]
    public void SelectSnapshot_Audio_ReturnsAudio()
    {
        var text = new TextModelDownloadSnapshot(TextModelDownloadPhase.Ready, 0, null);
        var vision = new TextModelDownloadSnapshot(TextModelDownloadPhase.Downloading, 0, null);
        var audio = new TextModelDownloadSnapshot(TextModelDownloadPhase.NotStarted, 0, null);
        var helpers = new TextModelDownloadSnapshot(TextModelDownloadPhase.Verifying, 0, null);
        TextModelDownloadUiPolicy.SelectSnapshot(ModelDownloadRowKind.Audio, text, vision, audio, helpers).Should().BeSameAs(audio);
    }

    [Fact]
    public void SelectSnapshot_Helpers_ReturnsHelpers()
    {
        var text = new TextModelDownloadSnapshot(TextModelDownloadPhase.Ready, 0, null);
        var vision = new TextModelDownloadSnapshot(TextModelDownloadPhase.Downloading, 0, null);
        var audio = new TextModelDownloadSnapshot(TextModelDownloadPhase.NotStarted, 0, null);
        var helpers = new TextModelDownloadSnapshot(TextModelDownloadPhase.Verifying, 0, null);
        TextModelDownloadUiPolicy.SelectSnapshot(ModelDownloadRowKind.Helpers, text, vision, audio, helpers).Should().BeSameAs(helpers);
    }

    [Fact]
    public void SelectSnapshot_Unknown_ReturnsText()
    {
        var text = new TextModelDownloadSnapshot(TextModelDownloadPhase.Ready, 0, null);
        var vision = new TextModelDownloadSnapshot(TextModelDownloadPhase.Downloading, 0, null);
        var audio = new TextModelDownloadSnapshot(TextModelDownloadPhase.NotStarted, 0, null);
        var helpers = new TextModelDownloadSnapshot(TextModelDownloadPhase.Verifying, 0, null);
        TextModelDownloadUiPolicy.SelectSnapshot((ModelDownloadRowKind)99, text, vision, audio, helpers).Should().BeSameAs(text);
    }

    [Fact]
    public void StartFor_Helpers_IsHelpers() =>
        TextModelDownloadUiPolicy.StartFor(ModelDownloadRowKind.Helpers).Should().Be(ModelDownloadStart.Helpers);

    [Fact]
    public void SlotFor_Ocr_WinsOverVision() =>
        TextModelDownloadUiPolicy.SlotFor(true, true).Should().Be(DownloadSnapshotSlot.Ocr);

    [Fact]
    public void SlotFor_Vision_IsVision() =>
        TextModelDownloadUiPolicy.SlotFor(true, false).Should().Be(DownloadSnapshotSlot.Vision);

    [Fact]
    public void SlotFor_Text_IsText() =>
        TextModelDownloadUiPolicy.SlotFor(false, false).Should().Be(DownloadSnapshotSlot.Text);

    [Fact]
    public void MirrorAudio_KeepsPhase()
    {
        var vision = new TextModelDownloadSnapshot(TextModelDownloadPhase.Ready, 0, null);
        TextModelDownloadUiPolicy.MirrorAudio(vision).Phase.Should().Be(TextModelDownloadPhase.Ready);
    }

    [Fact]
    public void FromModality_Image_IsVision() =>
        ModelDownloadRowKindExtensions.FromModality(Modality.Image).Should().Be(ModelDownloadRowKind.Vision);

    [Fact]
    public void FromModality_Video_Throws() =>
        FluentActions.Invoking(() => ModelDownloadRowKindExtensions.FromModality(Modality.Video)).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void OcrPack_Length_Is5() => OcrModelFiles.Pack.Length.Should().Be(5);

    [Fact]
    public void ShouldStart_Ready_IsTrue() =>
        WindowIngestPolicy.ShouldStart(TextModelDownloadPhase.Ready).Should().BeTrue();

    [Fact]
    public void ShouldStart_Downloading_IsFalse() =>
        WindowIngestPolicy.ShouldStart(TextModelDownloadPhase.Downloading).Should().BeFalse();

    [Fact]
    public void ChromeStatus_PassRunning_IsRunning() =>
        IngestChromePolicy.Status(true, TextModelDownloadPhase.Ready, TextModelDownloadPhase.Ready, TextModelDownloadPhase.Ready, false)
            .Should().Be(IngestChromeStatus.Running);

    [Fact]
    public void ChromeStatus_VisionDownloading_IsRunning() =>
        IngestChromePolicy.Status(false, TextModelDownloadPhase.Ready, TextModelDownloadPhase.Downloading, TextModelDownloadPhase.Ready, false)
            .Should().Be(IngestChromeStatus.Running);

    [Fact]
    public void ChromeStatus_Unfinished_IsStopped() =>
        IngestChromePolicy.Status(false, TextModelDownloadPhase.Ready, TextModelDownloadPhase.Ready, TextModelDownloadPhase.Ready, true)
            .Should().Be(IngestChromeStatus.Stopped);

    [Fact]
    public void ChromeStatus_Idle_IsDone() =>
        IngestChromePolicy.Status(false, TextModelDownloadPhase.Ready, TextModelDownloadPhase.Ready, TextModelDownloadPhase.Ready, false)
            .Should().Be(IngestChromeStatus.Done);

    [Fact]
    public void IngestKindLabels_Image_IsImageKey() =>
        IngestKindLabels.Key(GrantFileKinds.Image).Should().Be("Ingest_Image");

    [Fact]
    public void IngestKindLabels_None_Throws() =>
        FluentActions.Invoking(() => IngestKindLabels.Key(GrantFileKinds.None)).Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void RamMegabytes_OneMegabyte_Is1Mb() =>
        ResourceSampleFormat.RamMegabytes(1048576, "MB").Should().Be("1 MB");

    [Fact]
    public void CpuPercent_Truncates() =>
        ResourceSampleFormat.CpuPercent(12.9).Should().Be("12%");

    [Fact]
    public void GpuValue_Null_IsNull() =>
        ResourceSampleFormat.GpuValue(null).Should().BeNull();

    [Fact]
    public void GpuValue_Twelve_Is12Percent() =>
        ResourceSampleFormat.GpuValue(12.2).Should().Be("12%");

    [Fact]
    public void ModelRow_Source_HasNoDeferred()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Recall.UI", "Components", "Models", "ModelRow.razor"));
        razor.Should().NotContain("Models_Entry_Deferred");
    }

    [Fact]
    public void Preferences_AddedRowsInsideScroll()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Recall.UI", "Pages", "Library", "Preferences.razor"));
        razor.Split("max-h-[16rem] overflow-y-auto overflow-x-hidden", StringSplitOptions.None).Length.Should().Be(4);
    }

    [Fact]
    public void AppXaml_StartsIngestWhenReady()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Recall.Maui", "App.xaml.cs"));
        source.Should().Contain("WindowIngestPolicy.ShouldStart");
        source.Should().Contain("IngestGrantedTextAsync");
    }

    [Fact]
    public void SetSnapshot_MirrorsAudio()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Recall.Maui", "Platform", "Download", "MauiModelDownloadService.cs"));
        source.Should().Contain("audioSnapshot = TextModelDownloadUiPolicy.MirrorAudio(visionSnapshot)");
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
