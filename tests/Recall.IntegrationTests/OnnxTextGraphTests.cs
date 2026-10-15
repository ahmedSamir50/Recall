using FluentAssertions;
using Recall.Adapters.Embedding;

namespace Recall.IntegrationTests;

public sealed class OnnxTextGraphTests
{
    [Fact]
    public void EmptyFeatureDimensions_Null_Throws()
    {
        var act = () => OnnxTextGraph.EmptyFeatureDimensions(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EmptyFeatureDimensions_Empty_Throws()
    {
        var act = () => OnnxTextGraph.EmptyFeatureDimensions([]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EmptyFeatureDimensions_Symbolic_BecomesZero() =>
        OnnxTextGraph.EmptyFeatureDimensions([-1, 768]).Should().Equal(0, 768);

    [Fact]
    public void EmptyFeatureDimensions_FixedLeading_BecomesZero() =>
        OnnxTextGraph.EmptyFeatureDimensions([1, 4, 768]).Should().Equal(0, 4, 768);

    [Fact]
    public void EmptyFeatureDimensions_AlreadyEmpty_Stays() =>
        OnnxTextGraph.EmptyFeatureDimensions([0, 768]).Should().Equal(0, 768);

    [Fact]
    public void RequireTextInputs_Null_Throws()
    {
        var act = () => OnnxTextGraph.RequireTextInputs(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RequireTextInputs_AllFive_Returns()
    {
        var act = () => OnnxTextGraph.RequireTextInputs([
            OnnxTextGraph.InputIds,
            OnnxTextGraph.AttentionMask,
            OnnxTextGraph.ImageFeatures,
            OnnxTextGraph.VideoFeatures,
            OnnxTextGraph.AudioFeatures]);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireTextInputs_MissingVideoFeatures_Throws()
    {
        var act = () => OnnxTextGraph.RequireTextInputs([
            OnnxTextGraph.InputIds,
            OnnxTextGraph.AttentionMask,
            OnnxTextGraph.ImageFeatures,
            OnnxTextGraph.AudioFeatures]);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RequireFloat_Null_Throws()
    {
        var act = () => OnnxTextGraph.RequireFloat(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RequireFloat_Float_Returns()
    {
        var act = () => OnnxTextGraph.RequireFloat(typeof(float));
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireFloat_Int64_Throws()
    {
        var act = () => OnnxTextGraph.RequireFloat(typeof(long));
        act.Should().Throw<InvalidOperationException>();
    }
}
