using FluentAssertions;

namespace Recall.Maui.UnitTests;

public sealed class OcrWordErrorRateTests
{
    [Fact]
    public void Measure_IdenticalTokens_IsZero()
    {
        var error = OcrWordErrorRate.Measure("Alpha beta", "alpha beta");
        error.Rate.Should().Be(0d);
        error.Edits.Should().Be(0);
        error.ReferenceCount.Should().Be(2);
    }

    [Fact]
    public void Measure_OneSubstitution_IsOneThird()
    {
        var error = OcrWordErrorRate.Measure("alpha beta gamma", "alpha other gamma");
        error.Edits.Should().Be(1);
        error.Rate.Should().BeApproximately(1d / 3d, 1e-9);
    }

    [Fact]
    public void Measure_EmptyHypothesis_IsOne()
    {
        var error = OcrWordErrorRate.Measure("alpha beta", string.Empty);
        error.Rate.Should().Be(1d);
        error.Edits.Should().Be(2);
    }

    [Fact]
    public void MeasureScript_KeepsOnlyThatScript()
    {
        var arabic = OcrWordErrorRate.MeasureScript("مياه water غازية", "مياه غازية", OcrScript.Arabic);
        arabic.ReferenceCount.Should().Be(2);
        arabic.Rate.Should().Be(0d);

        var latin = OcrWordErrorRate.MeasureScript("مياه water غازية", "water", OcrScript.Latin);
        latin.ReferenceCount.Should().Be(1);
        latin.Rate.Should().Be(0d);
    }
}
