using FluentAssertions;
using Recall.Core.Locale;
using Recall.UnitTests.Fakes;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Theme;
using Recall.UI.State;

namespace Recall.UnitTests;

public sealed class ShellUiStoreTests
{
    [Fact]
    public void SetThemePreference_WhenCalled_UpdatesSnapshot()
    {
        // Arrange
        var themeStore = new FakeThemePreferenceStore();
        var localeStore = new FakeUiLocaleStore();
        var grantStore = new FakeScopeGrantStore();
        var store = new ShellUiStore(
            new ThemeService(themeStore, new FakeSystemThemeProvider()),
            new LocaleService(localeStore),
            new ScopeGrantService(grantStore));

        // Act
        store.SetThemePreference(ThemePreference.Dark);

        // Assert
        store.Current.ThemePreference.Should().Be(ThemePreference.Dark);
        store.Current.EffectiveTheme.Should().Be("dark");
    }

    [Fact]
    public void TryAddGrant_WhenFolderAdded_ReflectsInSnapshot()
    {
        // Arrange
        var store = CreateStore();

        // Act
        store.TryAddGrant(new ScopeFolderGrant(@"C:\docs", "docs"));

        // Assert
        store.Current.Grants.Should().ContainSingle(g => g.Path == @"C:\docs");
        store.Current.CanContinueFolders.Should().BeTrue();
    }

    [Fact]
    public void Dispose_WhenCalled_DoesNotThrow()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var act = () => store.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void SetRoute_WhenPathChanges_UpdatesSnapshotRoute()
    {
        // Arrange
        var store = CreateStore();

        // Act
        store.SetRoute("/preferences");

        // Assert
        store.Current.RoutePath.Should().Be("/preferences");
    }

    private static ShellUiStore CreateStore()
    {
        return new ShellUiStore(
            new ThemeService(new FakeThemePreferenceStore(), new FakeSystemThemeProvider()),
            new LocaleService(new FakeUiLocaleStore()),
            new ScopeGrantService(new FakeScopeGrantStore()));
    }
}
