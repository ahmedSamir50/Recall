using FluentAssertions;
using Recall.Core.Faults;
using Recall.Core.Privacy;

namespace Recall.UnitTests;

public sealed class EgressProbeTests
{
    [Fact]
    public void EgressProxyMissing_Fault_IsTwentyTwo()
    {
        // Arrange / Act / Assert
        ((int)RecallFault.EgressProxyMissing).Should().Be(22);
    }

    [Fact]
    public void EgressWatchIncomplete_Fault_IsTwentyThree()
    {
        // Arrange / Act / Assert
        ((int)RecallFault.EgressWatchIncomplete).Should().Be(23);
    }

    [Fact]
    public async Task HasNonLocalByteAsync_Cancelled_Throws()
    {
        // Arrange
        var probe = new ProxyLogEgressProbe(null);
        var token = new CancellationToken(true);

        // Act
        var act = () => probe.HasNonLocalByteAsync(token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task HasNonLocalByteAsync_NullPath_ThrowsEgressProxyMissing()
    {
        // Arrange
        var probe = new ProxyLogEgressProbe(null);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<RecallFaultException>().Where(ex => ex.Fault == RecallFault.EgressProxyMissing);
    }

    [Fact]
    public async Task HasNonLocalByteAsync_MissingFile_ThrowsEgressProxyMissing()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "recall_egress_missing_" + Guid.NewGuid().ToString("N") + ".log");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<RecallFaultException>().Where(ex => ex.Fault == RecallFault.EgressProxyMissing);
    }

    [Fact]
    public async Task HasNonLocalByteAsync_NoMarker_ThrowsEgressProxyMissing()
    {
        // Arrange
        var path = WriteLog("0\t127.0.0.1\t1");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<RecallFaultException>().Where(ex => ex.Fault == RecallFault.EgressProxyMissing);
    }

    [Fact]
    public async Task HasNonLocalByteAsync_TwoMarkers_ThrowsFormatException()
    {
        // Arrange
        var path = WriteLog(
            "0\tdownload-finished\t0",
            "1\tdownload-finished\t0");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<FormatException>().WithMessage("Egress log has two download-finished lines.");
    }

    [Fact]
    public async Task HasNonLocalByteAsync_BlankLine_ThrowsFormatException()
    {
        // Arrange
        var path = WriteLog("    ");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<FormatException>().WithMessage("Egress log line is not three fields.");
    }

    [Fact]
    public async Task HasNonLocalByteAsync_BadTimestamp_ThrowsFormatException()
    {
        // Arrange
        var path = WriteLog("nope\tdownload-finished\t0");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<FormatException>().WithMessage("Egress timestamp is not an integer.");
    }

    [Fact]
    public async Task HasNonLocalByteAsync_BlankAddress_ThrowsFormatException()
    {
        // Arrange
        var path = WriteLog("1\t\t0");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<FormatException>().WithMessage("Egress address is blank.");
    }

    [Fact]
    public async Task HasNonLocalByteAsync_BadByteCount_ThrowsFormatException()
    {
        // Arrange
        var path = WriteLog("1\t127.0.0.1\tnope");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<FormatException>().WithMessage("Egress byte count is not an integer.");
    }

    [Fact]
    public async Task HasNonLocalByteAsync_NegativeByteCount_ThrowsFormatException()
    {
        // Arrange
        var path = WriteLog("1\t127.0.0.1\t-1");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<FormatException>().WithMessage("Egress byte count is negative.");
    }

    [Fact]
    public async Task HasNonLocalByteAsync_MarkerByteCountNonZero_ThrowsFormatException()
    {
        // Arrange
        var path = WriteLog(
            "0\tdownload-finished\t1",
            "60000\t127.0.0.1\t1");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<FormatException>().WithMessage("Egress download-finished byte count is not zero.");
    }

    [Fact]
    public async Task HasNonLocalByteAsync_UnderSixtyMinutes_ThrowsEgressWatchIncomplete()
    {
        // Arrange
        var path = WriteLog(
            "0\tdownload-finished\t0",
            "59999\t127.0.0.1\t1");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var act = () => probe.HasNonLocalByteAsync(default);

        // Assert
        await act.Should().ThrowAsync<RecallFaultException>().Where(ex => ex.Fault == RecallFault.EgressWatchIncomplete);
    }

    [Fact]
    public async Task HasNonLocalByteAsync_LoopbackOnly_ReturnsFalse()
    {
        // Arrange
        var path = WriteLog(
            "0\tdownload-finished\t0",
            "60000\t127.0.0.1\t1",
            "60000\t::1\t1");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var result = await probe.HasNonLocalByteAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasNonLocalByteAsync_NonLocalByte_ReturnsTrue()
    {
        // Arrange
        var path = WriteLog(
            "0\tdownload-finished\t0",
            "60000\t8.8.8.8\t1");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var result = await probe.HasNonLocalByteAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasNonLocalByteAsync_ZeroByteNonLocal_ReturnsFalse()
    {
        // Arrange
        var path = WriteLog(
            "0\tdownload-finished\t0",
            "60000\t8.8.8.8\t0");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var result = await probe.HasNonLocalByteAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasNonLocalByteAsync_BeforeMarker_IgnoresNonLocal()
    {
        // Arrange
        var path = WriteLog(
            "0\t8.8.8.8\t5",
            "1000\tdownload-finished\t0",
            "61000\t127.0.0.1\t1");
        var probe = new ProxyLogEgressProbe(path);

        // Act
        var result = await probe.HasNonLocalByteAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    private static string WriteLog(params string[] lines)
    {
        var path = Path.Combine(Path.GetTempPath(), "recall_egress_" + Guid.NewGuid().ToString("N") + ".log");
        File.WriteAllLines(path, lines);
        return path;
    }
}
