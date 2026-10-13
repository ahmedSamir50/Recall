using Recall.Core.Ports;
using Recall.Core.Preferences;

namespace Recall.UnitTests.Fakes;

public sealed class FakeThemePreferenceStore : IThemePreferenceStore
{
    public ThemePreference Value { get; set; } = ThemePreference.System;

    public ThemePreference Get() => Value;

    public void Set(ThemePreference preference) => Value = preference;
}

public sealed class FakeUiLocaleStore : IUiLocaleStore
{
    public UiLocale Value { get; set; } = UiLocale.En;

    public UiLocale Get() => Value;

    public void Set(UiLocale locale) => Value = locale;
}

public sealed class FakeSystemThemeProvider : ISystemThemeProvider
{
    public bool OsIsDark { get; set; } = true;

    public bool IsDark() => OsIsDark;
}

public sealed class FakeScopeGrantStore : IScopeGrantStore
{
    private List<Recall.Core.Scope.ScopeFolderGrant> grants = [];

    public IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> List() => grants;

    public void Save(IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> grants) =>
        this.grants = grants.ToList();
}

public sealed class FakeFolderPickerService : IFolderPickerService
{
    public FolderPickerResult NextResult { get; set; } =
        new(FolderPickerOutcome.Cancelled, null);

    public Task<FolderPickerResult> PickFolderAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(NextResult);
}
