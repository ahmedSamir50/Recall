using FluentAssertions;
using Recall.Core.Locale;

namespace Recall.UnitTests;

public sealed class ArabicDigitShapingTests
{
    [Fact]
    public void ShapeDigits_KeepsLatinDigits()
    {
        LocaleService.ShapeDigits("123").Should().Be("123");
        LocaleService.ShapeDigits("98%").Should().Be("98%");
        LocaleService.ShapeDigits(string.Empty).Should().BeEmpty();
    }
}
