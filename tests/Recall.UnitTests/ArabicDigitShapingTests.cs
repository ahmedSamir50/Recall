using FluentAssertions;
using Recall.Core.Locale;

namespace Recall.UnitTests;

public sealed class ArabicDigitShapingTests
{
    [Fact]
    public void ShapeForArabicUi_Empty_ReturnsEmpty()
    {
        // Act
        var shaped = ArabicDigitShaping.ShapeForArabicUi(string.Empty);

        // Assert
        shaped.Should().BeEmpty();
    }

    [Fact]
    public void ShapeForArabicUi_LatinDigits_BecomesArabicIndic()
    {
        // Act
        var shaped = ArabicDigitShaping.ShapeForArabicUi("123");

        // Assert
        shaped.Should().Be("١٢٣");
    }

    [Fact]
    public void ShapeForArabicUi_AlreadyArabicDigits_Unchanged()
    {
        // Act
        var shaped = ArabicDigitShaping.ShapeForArabicUi("١٢٣");

        // Assert
        shaped.Should().Be("١٢٣");
    }

    [Fact]
    public void ShapeForArabicUi_PercentSign_Preserved()
    {
        // Act
        var shaped = ArabicDigitShaping.ShapeForArabicUi("98%");

        // Assert
        shaped.Should().Be("٩٨%");
    }
}
