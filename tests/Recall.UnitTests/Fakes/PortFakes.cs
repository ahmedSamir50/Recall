using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Sync;

namespace Recall.UnitTests.Fakes;

public sealed class FakeFileScopeWatcherService : IFileScopeWatcherService
{
}

public sealed class FakeScopeManagerService : IScopeManagerService
{
    private readonly FakeScopeGrantStore store = new();
    private readonly ScopeGrantService inner;

    public FakeScopeManagerService()
    {
        inner = new ScopeGrantService(store);
    }

    public IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> ListGrants() => inner.ListGrants();

    public Recall.Core.Scope.ScopeGrantAddResult TryAddGrant(Recall.Core.Scope.ScopeFolderGrant grant) =>
        inner.TryAddGrant(grant);

    public void RemoveGrant(string path) => inner.RemoveGrant(path);

    public bool CanContinue() => inner.CanContinue();
}

public sealed class FakeVideoFrameSamplerService : IVideoFrameSamplerService
{
}

public sealed class FakeSecureStorageService : ISecureStorageService
{
}

public sealed class FakeIdleDetectorService : IIdleDetectorService
{
}

public sealed class FakeModelDownloadService : IModelDownloadService
{
}

public sealed class FakeIndexPolicy : IIndexPolicy
{
}

public sealed class FakeSyncAdapter : ISyncAdapter
{
}

public sealed class FakeThemePreferenceStorePort : IThemePreferenceStore
{
    public Recall.Core.Preferences.ThemePreference Get() => Recall.Core.Preferences.ThemePreference.System;

    public void Set(Recall.Core.Preferences.ThemePreference preference)
    {
    }
}

public sealed class FakeUiLocaleStorePort : IUiLocaleStore
{
    public Recall.Core.Preferences.UiLocale Get() => Recall.Core.Preferences.UiLocale.En;

    public void Set(Recall.Core.Preferences.UiLocale locale)
    {
    }
}

public sealed class FakeSystemThemeProviderPort : ISystemThemeProvider
{
    public bool IsDark() => true;
}

public sealed class FakeScopeGrantStorePort : IScopeGrantStore
{
    public IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> List() => [];

    public void Save(IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> grants)
    {
    }
}

public sealed class FakeFolderPickerServicePort : IFolderPickerService
{
    public Task<FolderPickerResult> PickFolderAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new FolderPickerResult(FolderPickerOutcome.Cancelled, null));
}

public sealed class FakeDeviceRecordService : IDeviceRecordService
{
    public string GetDisplayName() => "Recall device";

    public string GetInitials() => "RD";
}
