using System.Globalization;
using FluentAssertions;
using Recall.Core.Locale;
using Recall.Core.Preferences;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class LocaleServiceTests
{
    [Fact]
    public void LangCode_Arabic_IsAr()
    {
        // Arrange
        var store = new FakeUiLocaleStore { Value = UiLocale.Ar };
        var service = new LocaleService(store);

        // Act
        var lang = service.LangCode();

        // Assert
        lang.Should().Be("ar");
        service.Dir().Should().Be("rtl");
    }

    [Fact]
    public void LangCode_English_IsEn()
    {
        // Arrange
        var store = new FakeUiLocaleStore { Value = UiLocale.En };
        var service = new LocaleService(store);

        // Act
        var lang = service.LangCode();

        // Assert
        lang.Should().Be("en");
        service.Dir().Should().Be("ltr");
    }

    [Fact]
    public void ShapeDigits_English_LeavesLatinDigits()
    {
        // Arrange
        var service = new LocaleService(new FakeUiLocaleStore { Value = UiLocale.En });

        // Act
        var shaped = service.ShapeDigits("42");

        // Assert
        shaped.Should().Be("42");
    }

    [Fact]
    public void ApplyCulture_Arabic_SetsCurrentCulture()
    {
        // Arrange
        var service = new LocaleService(new FakeUiLocaleStore());

        // Act
        service.ApplyCulture(UiLocale.Ar);

        // Assert
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Should().Be("ar");
    }
}
