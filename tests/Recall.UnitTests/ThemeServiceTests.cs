using FluentAssertions;
using Recall.Core.Preferences;
using Recall.Core.Theme;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class ThemeServiceTests
{
    [Fact]
    public void GetEffectiveTheme_SystemAndOsDark_ReturnsDark()
    {
        // Arrange
        var store = new FakeThemePreferenceStore { Value = ThemePreference.System };
        var system = new FakeSystemThemeProvider { OsIsDark = true };
        var service = new ThemeService(store, system);

        // Act
        var theme = service.GetEffectiveTheme();

        // Assert
        theme.Should().Be("dark");
    }

    [Fact]
    public void GetEffectiveTheme_SystemAndOsLight_ReturnsLight()
    {
        // Arrange
        var store = new FakeThemePreferenceStore { Value = ThemePreference.System };
        var system = new FakeSystemThemeProvider { OsIsDark = false };
        var service = new ThemeService(store, system);

        // Act
        var theme = service.GetEffectiveTheme();

        // Assert
        theme.Should().Be("light");
    }

    [Fact]
    public void GetEffectiveTheme_ForcedDark_ReturnsDark()
    {
        // Arrange
        var store = new FakeThemePreferenceStore { Value = ThemePreference.Dark };
        var system = new FakeSystemThemeProvider { OsIsDark = false };
        var service = new ThemeService(store, system);

        // Act
        var theme = service.GetEffectiveTheme();

        // Assert
        theme.Should().Be("dark");
    }

    [Fact]
    public void GetEffectiveTheme_ForcedLight_ReturnsLight()
    {
        // Arrange
        var store = new FakeThemePreferenceStore { Value = ThemePreference.Light };
        var system = new FakeSystemThemeProvider { OsIsDark = true };
        var service = new ThemeService(store, system);

        // Act
        var theme = service.GetEffectiveTheme();

        // Assert
        theme.Should().Be("light");
    }

    [Fact]
    public void ParseStored_UnknownValue_FallsBackToSystem()
    {
        // Act
        var parsed = ThemeResolution.ParseStored("not-a-theme");

        // Assert
        parsed.Should().Be(ThemePreference.System);
    }

    [Fact]
    public void SetPreference_RoundTripsThroughFakeStore()
    {
        // Arrange
        var store = new FakeThemePreferenceStore();
        var service = new ThemeService(store, new FakeSystemThemeProvider());

        // Act
        service.SetPreference(ThemePreference.Light);

        // Assert
        store.Value.Should().Be(ThemePreference.Light);
        service.GetPreference().Should().Be(ThemePreference.Light);
    }
}
