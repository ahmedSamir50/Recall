using FluentAssertions;
using Recall.Benchmarks;

namespace Recall.BenchTests;

public sealed class TextBenchmarkHostTests
{
    [Fact]
    public void Label_NotAndroid_IsDesktop()
    {
        TextBenchmarkHost.Label(false, "phone").Should().Be("desktop");
    }

    [Fact]
    public void Label_AndroidMissingHost_IsEmulator()
    {
        TextBenchmarkHost.Label(true, null).Should().Be("emulator");
    }

    [Fact]
    public void Label_AndroidBlankHost_IsEmulator()
    {
        TextBenchmarkHost.Label(true, " ").Should().Be("emulator");
    }

    [Fact]
    public void Label_AndroidPhoneHost_IsPhone()
    {
        TextBenchmarkHost.Label(true, "phone").Should().Be("phone");
    }

    [Fact]
    public void Label_AndroidPhoneHostMixedCase_IsPhone()
    {
        TextBenchmarkHost.Label(true, "Phone").Should().Be("phone");
    }

    [Fact]
    public void DesktopIdenticalVectors_AreNotThePhoneGate()
    {
        TextBenchmarkHost.PhoneHybridAndLoadMeetTheExit("desktop", 2.20243, 727.3).Should().BeFalse();
    }

    [Fact]
    public void PhoneHybridAndLoad_MeetTheExit()
    {
        TextBenchmarkHost.PhoneHybridAndLoadMeetTheExit("phone", 200d, 1000d).Should().BeTrue();
        TextBenchmarkHost.PhoneHybridAndLoadMeetTheExit("Phone", 200d, 1000d).Should().BeTrue();
        TextBenchmarkHost.PhoneHybridAndLoadMeetTheExit("phone", 200.1d, 1000d).Should().BeFalse();
        TextBenchmarkHost.PhoneHybridAndLoadMeetTheExit("phone", 200d, 1000.1d).Should().BeFalse();
        TextBenchmarkHost.PhoneHybridAndLoadMeetTheExit("emulator", 200d, 1000d).Should().BeTrue();
        TextBenchmarkHost.PhoneHybridAndLoadMeetTheExit("emulator", 200.1d, 1000d).Should().BeFalse();
        TextBenchmarkHost.PhoneHybridAndLoadMeetTheExit("emulator", 135.37d, 886.14d).Should().BeTrue();
        TextBenchmarkHost.PhoneHybridAndLoadMeetTheExit("emulator", 1d, 1d).Should().BeTrue();
        TextBenchmarkHost.PhoneHybridAndLoadMeetTheExit(null!, 1d, 1d).Should().BeFalse();
    }

    [Fact]
    public void Mean_TenSamples_IsArithmeticMean()
    {
        TextBenchmarkHost.Mean([10d, 20d, 30d, 40d, 50d, 60d, 70d, 80d, 90d, 100d]).Should().Be(55d);
        var act = () => TextBenchmarkHost.Mean([]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LabelThisProcess_MatchesOperatingSystem()
    {
        var expected = TextBenchmarkHost.Label(
            OperatingSystem.IsAndroid() || OperatingSystem.IsIOS(),
            Environment.GetEnvironmentVariable(TextBenchmarkHost.BenchHostVariable));
        TextBenchmarkHost.LabelThisProcess().Should().Be(expected);
    }
}
