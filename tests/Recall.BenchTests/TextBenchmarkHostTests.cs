using FluentAssertions;
using Recall.Benchmarks;

namespace Recall.BenchTests;

public sealed class TextBenchmarkHostTests
{
    [Fact]
    public void LabelThisProcess_MatchesOperatingSystem()
    {
        var expected = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() ? "phone" : "desktop";
        TextBenchmarkHost.LabelThisProcess().Should().Be(expected);
    }
}
