using Recall.Core.Preferences;
using Recall.Core.Scope;

namespace Recall.Core.Ports;

/// <summary>Filesystem events while the process is running.</summary>
public interface IFileScopeWatcherService
{
}

/// <summary>Granted paths and the parent-wins tree check.</summary>
public interface IScopeManagerService
{
    IReadOnlyList<ScopeFolderGrant> ListGrants();

    ScopeGrantAddResult TryAddGrant(ScopeFolderGrant grant);

    void RemoveGrant(string path);

    bool CanContinue();
}

/// <summary>Persists folder grants for the scope manager.</summary>
public interface IScopeGrantStore
{
    IReadOnlyList<ScopeFolderGrant> List();

    void Save(IReadOnlyList<ScopeFolderGrant> grants);
}

public enum FolderPickerOutcome
{
    Selected = 0,
    Cancelled = 1,
    Denied = 2,
}

public sealed record FolderPickerResult(FolderPickerOutcome Outcome, string? Path);

/// <summary>Platform folder picker. Only implementation may touch native dialogs.</summary>
public interface IFolderPickerService
{
    Task<FolderPickerResult> PickFolderAsync(CancellationToken cancellationToken = default);
}

/// <summary>Platform video frame sampler.</summary>
public interface IVideoFrameSamplerService
{
}

/// <summary>Secure storage port. v1 stores no sync keys.</summary>
public interface ISecureStorageService
{
}

/// <summary>Pauses ingestion. It does not capture.</summary>
public interface IIdleDetectorService
{
}

/// <summary>The only network path. No method until the download epic.</summary>
public interface IModelDownloadService
{
}

/// <summary>Stored width, age cutoff, and enabled modalities. Rules stay in Core.</summary>
public interface IIndexPolicy
{
}

/// <summary>Persists theme preference.</summary>
public interface IThemePreferenceStore
{
    ThemePreference Get();

    void Set(ThemePreference preference);
}

/// <summary>Persists UI locale.</summary>
public interface IUiLocaleStore
{
    UiLocale Get();

    void Set(UiLocale locale);
}

/// <summary>Reports whether the OS is currently using a dark theme.</summary>
public interface ISystemThemeProvider
{
    bool IsDark();
}

/// <summary>Local device id and display name for this install.</summary>
public interface IDeviceRecordService
{
    string GetDisplayName();

    string GetInitials();
}
