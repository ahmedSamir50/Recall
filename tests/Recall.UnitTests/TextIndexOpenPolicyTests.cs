using FluentAssertions;
using Recall.Core.Indexing;

namespace Recall.UnitTests;

public sealed class TextIndexOpenPolicyTests
{
    [Fact]
    public void Decide_MissingDirectory_Creates()
    {
        // Arrange
        const bool directoryExists = false;

        // Act
        var whenFlagIsTrue = TextIndexOpenPolicy.Decide(directoryExists, hasFtsKeyVector: true);
        var whenFlagIsFalse = TextIndexOpenPolicy.Decide(directoryExists, hasFtsKeyVector: false);

        // Assert
        whenFlagIsTrue.Should().Be(TextIndexOpenKind.Create);
        whenFlagIsFalse.Should().Be(TextIndexOpenKind.Create);
    }

    [Fact]
    public void Decide_MissingFtsKey_Replaces()
    {
        // Arrange
        const bool directoryExists = true;

        // Act
        var decision = TextIndexOpenPolicy.Decide(directoryExists, hasFtsKeyVector: false);

        // Assert
        decision.Should().Be(TextIndexOpenKind.Replace);
    }

    [Fact]
    public void Decide_CurrentFtsKey_Opens()
    {
        // Arrange
        const bool directoryExists = true;

        // Act
        var decision = TextIndexOpenPolicy.Decide(directoryExists, hasFtsKeyVector: true);

        // Assert
        decision.Should().Be(TextIndexOpenKind.Open);
    }
}
