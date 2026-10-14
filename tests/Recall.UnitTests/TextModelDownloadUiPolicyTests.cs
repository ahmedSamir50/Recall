using FluentAssertions;
using Recall.Core.Download;

namespace Recall.UnitTests;

public sealed class TextModelDownloadUiPolicyTests
{
    [Theory]
    [InlineData(TextModelDownloadPhase.NotStarted, true)]
    [InlineData(TextModelDownloadPhase.Downloading, false)]
    [InlineData(TextModelDownloadPhase.Verifying, false)]
    [InlineData(TextModelDownloadPhase.Ready, false)]
    [InlineData(TextModelDownloadPhase.HashFailed, false)]
    [InlineData(TextModelDownloadPhase.Cancelled, false)]
    [InlineData(TextModelDownloadPhase.Interrupted, false)]
    public void ShouldAutoStartDownload_OnlyWhenNotStarted(TextModelDownloadPhase phase, bool expected) =>
        TextModelDownloadUiPolicy.ShouldAutoStartDownload(phase).Should().Be(expected);

    [Theory]
    [InlineData(TextModelDownloadPhase.NotStarted, false)]
    [InlineData(TextModelDownloadPhase.Downloading, false)]
    [InlineData(TextModelDownloadPhase.Verifying, false)]
    [InlineData(TextModelDownloadPhase.Ready, false)]
    [InlineData(TextModelDownloadPhase.HashFailed, true)]
    [InlineData(TextModelDownloadPhase.Cancelled, true)]
    [InlineData(TextModelDownloadPhase.Interrupted, true)]
    public void ShouldOfferRetry_ForRecoverableFailures(TextModelDownloadPhase phase, bool expected) =>
        TextModelDownloadUiPolicy.ShouldOfferRetry(phase).Should().Be(expected);

    [Theory]
    [InlineData(-1, 0, true)]
    [InlineData(0, 0, false)]
    [InlineData(16, 17, true)]
    [InlineData(33, 33, false)]
    public void ShouldPublishProgress_OnlyWhenTheShownPercentChanges(int last, int next, bool expected) =>
        TextModelDownloadUiPolicy.ShouldPublishProgress(last, next).Should().Be(expected);

    [Fact]
    public void ComputePercent_UsesFloorAndRejectsEmptyOrNegativeTotals()
    {
        TextModelDownloadUiPolicy.ComputePercent(0, 100).Should().Be(0);
        TextModelDownloadUiPolicy.ComputePercent(-1, 100).Should().Be(0);
        TextModelDownloadUiPolicy.ComputePercent(2, 3).Should().Be(66);
        TextModelDownloadUiPolicy.ComputePercent(100, 100).Should().Be(100);
        TextModelDownloadUiPolicy.ComputePercent(200, 100).Should().Be(100);
        TextModelDownloadUiPolicy.ComputePercent(10, null).Should().Be(0);
        TextModelDownloadUiPolicy.ComputePercent(10, 0).Should().Be(0);
        TextModelDownloadUiPolicy.ComputePercent(10, -5).Should().Be(0);
    }

    [Fact]
    public void ComputeOverallPercent_DoesNotResetWhenTheNextFileStarts()
    {
        TextModelDownloadUiPolicy.ComputeOverallPercent(0, 3, 1, 2).Should().Be(16);
        TextModelDownloadUiPolicy.ComputeOverallPercent(1, 3, 0, 100).Should().Be(33);
        TextModelDownloadUiPolicy.ComputeOverallPercent(1, 3, 0, null).Should().Be(33);
        TextModelDownloadUiPolicy.ComputeOverallPercent(3, 3, 0, null).Should().Be(100);
        TextModelDownloadUiPolicy.ComputeOverallPercent(0, 0, 50, 100).Should().Be(0);
        TextModelDownloadUiPolicy.ComputeOverallPercent(-1, 3, 0, 100).Should().Be(0);
        TextModelDownloadUiPolicy.ComputeOverallPercent(4, 3, 0, null).Should().Be(100);
    }
}
