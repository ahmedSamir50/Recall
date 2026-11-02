using FluentAssertions;
using Recall.Core.Download;
using Recall.Core.Ingest;
using Recall.Core.Runtime;
using Recall.UI.Components;
using Recall.UI.Layout;

namespace Recall.UnitTests;

public sealed class ShellChromeTests
{
    private static string RepoRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void ShellStatus_VisionDownloading_IsRunning()
    {
        // Arrange
        var status = IngestChromePolicy.Status(
            false,
            TextModelDownloadPhase.Ready,
            TextModelDownloadPhase.Downloading,
            TextModelDownloadPhase.Ready,
            false, DeviceVerdict.Ok);

        // Act
        var key = ShellStatusLabel.Key(status);

        // Assert
        status.Should().Be(IngestChromeStatus.Running);
        key.Should().Be("Ingest_Running");
    }

    [Theory]
    [InlineData(TextModelDownloadPhase.Downloading)]
    [InlineData(TextModelDownloadPhase.Verifying)]
    public void ShellStatus_TextBusy_IsRunning(TextModelDownloadPhase phase)
    {
        // Act
        var key = ShellStatusLabel.Key(IngestChromePolicy.Status(false, phase, TextModelDownloadPhase.Ready, TextModelDownloadPhase.Ready, false, DeviceVerdict.Ok));

        // Assert
        key.Should().Be("Ingest_Running");
    }

    [Theory]
    [InlineData(TextModelDownloadPhase.Downloading)]
    [InlineData(TextModelDownloadPhase.Verifying)]
    public void ShellStatus_AudioBusy_IsRunning(TextModelDownloadPhase phase)
    {
        // Act
        var key = ShellStatusLabel.Key(IngestChromePolicy.Status(false, TextModelDownloadPhase.Ready, TextModelDownloadPhase.Ready, phase, false, DeviceVerdict.Ok));

        // Assert
        key.Should().Be("Ingest_Running");
    }

    [Fact]
    public void ShellStatus_VisionVerifying_IsRunning()
    {
        // Act
        var key = ShellStatusLabel.Key(IngestChromePolicy.Status(
            false,
            TextModelDownloadPhase.Ready,
            TextModelDownloadPhase.Verifying,
            TextModelDownloadPhase.Ready,
            false, DeviceVerdict.Ok));

        // Assert
        key.Should().Be("Ingest_Running");
    }

    [Fact]
    public void ShellStatus_PassRunning_IsRunning()
    {
        // Act
        var key = ShellStatusLabel.Key(IngestChromePolicy.Status(
            true,
            TextModelDownloadPhase.Ready,
            TextModelDownloadPhase.Ready,
            TextModelDownloadPhase.Ready,
            false, DeviceVerdict.Ok));

        // Assert
        key.Should().Be("Ingest_Running");
    }

    [Theory]
    [InlineData(TextModelDownloadPhase.NotStarted)]
    [InlineData(TextModelDownloadPhase.Ready)]
    [InlineData(TextModelDownloadPhase.HashFailed)]
    [InlineData(TextModelDownloadPhase.Cancelled)]
    [InlineData(TextModelDownloadPhase.Interrupted)]
    public void ShellStatus_AllIdle_IsReady(TextModelDownloadPhase phase)
    {
        // Arrange
        var status = IngestChromePolicy.Status(false, phase, phase, phase, false, DeviceVerdict.Ok);

        // Act
        var key = ShellStatusLabel.Key(status);

        // Assert
        status.Should().Be(IngestChromeStatus.Done);
        key.Should().Be("Status_Ready");
    }

    [Fact]
    public void ShellStatus_Unfinished_IsNotReady()
    {
        // Arrange
        var status = IngestChromePolicy.Status(
            false,
            TextModelDownloadPhase.Ready,
            TextModelDownloadPhase.Ready,
            TextModelDownloadPhase.Ready,
            true, DeviceVerdict.Ok);

        // Act
        var key = ShellStatusLabel.Key(status);

        // Assert
        status.Should().Be(IngestChromeStatus.Stopped);
        key.Should().BeNull();
    }

    [Fact]
    public void ShellStatus_Unknown_IsNotReady()
    {
        // Act
        var key = ShellStatusLabel.Key((IngestChromeStatus)99);

        // Assert
        key.Should().BeNull();
    }

    [Fact]
    public void ShellStatus_RunningAndDone_UseTheExistingKeys()
    {
        // Assert
        ShellStatusLabel.Key(IngestChromeStatus.Running).Should().Be("Ingest_Running");
        ShellStatusLabel.Key(IngestChromeStatus.Done).Should().Be("Status_Ready");
        ShellStatusLabel.Key(IngestChromeStatus.Stopped).Should().BeNull();
    }

    [Fact]
    public void RouteMatch_ExactPath_IsActive()
    {
        // Assert
        ShellRouteMatch.IsActive("/", "/").Should().BeTrue();
        ShellRouteMatch.IsActive("/preferences", "/preferences").Should().BeTrue();
        ShellRouteMatch.IsActive("/folders", "/preferences").Should().BeFalse();
        ShellRouteMatch.IsActive("/device", "/").Should().BeFalse();
        ShellRouteMatch.IsActive(null, "/preferences").Should().BeFalse();
    }

    [Fact]
    public void RouteMatch_DeviceAndPin_CountAsPreferences()
    {
        // Assert
        ShellRouteMatch.IsActive("/device", "/preferences").Should().BeTrue();
        ShellRouteMatch.IsActive("/pin", "/preferences").Should().BeTrue();
        ShellRouteMatch.IsActive("/device", "/pin").Should().BeFalse();
        ShellRouteMatch.IsActive("/pin", "/device").Should().BeFalse();
    }

    [Fact]
    public void AppShell_ReadyLabel_FollowsChromeStatus()
    {
        // Arrange
        var shell = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Layout", "AppShell.razor"));

        // Assert
        shell.Should().Contain("ShellStatusKey");
        shell.Should().NotContain("Get(\"Status_Ready\"");
    }

    [Fact]
    public void HomeRazor_CardNote_ClampsHeight()
    {
        // Arrange
        var home = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Pages", "Find", "Home.razor"));

        // Assert
        home.Split("max-h-[8rem] overflow-hidden\">@card.RawNote", StringSplitOptions.None).Length.Should().Be(3);
    }

    [Fact]
    public void DeviceAndPin_Back_GoesToPreferences()
    {
        // Arrange
        var device = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Pages", "Library", "Device.razor"));
        var pin = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Pages", "Library", "Pin.razor"));
        var deviceCode = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Pages", "Library", "Device.razor.cs"));
        var pinCode = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Pages", "Library", "Pin.razor.cs"));
        var english = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Resources", "Strings.resx"));
        var arabic = File.ReadAllText(Path.Combine(RepoRoot, "src", "Recall.UI", "Resources", "Strings.ar.resx"));

        // Assert
        device.Should().Contain("Btn_Back");
        pin.Should().Contain("Btn_Back");
        device.IndexOf("Btn_Back", StringComparison.Ordinal).Should().BeLessThan(device.IndexOf("Device_Title", StringComparison.Ordinal));
        pin.IndexOf("Btn_Back", StringComparison.Ordinal).Should().BeLessThan(pin.IndexOf("Pin_EmptyTitle", StringComparison.Ordinal));
        deviceCode.Should().Contain("NavigateTo(\"/preferences\")");
        pinCode.Should().Contain("NavigateTo(\"/preferences\")");
        english.Should().Contain("<value>Back</value>");
        arabic.Should().Contain("<value>رجوع</value>");
    }
}
