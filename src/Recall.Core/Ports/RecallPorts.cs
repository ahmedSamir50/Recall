using Recall.Core.Device;
using Recall.Core.Download;
using Recall.Core.Indexing;
using Recall.Core.Search;
using Recall.Core.Text;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Vocabulary;

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

    void SetGrantFileKinds(string path, GrantFileKinds kinds);

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

public sealed record FolderPickerResult(FolderPickerOutcome Outcome, string? Path, string? ReopenToken = null);

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

/// <summary>The only network path for ONNX weights.</summary>
public interface IModelDownloadService
{
    TextModelDownloadSnapshot GetTextDownloadSnapshot();

    Task DownloadTextModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default);

    void CancelTextDownload();

    Task RetryTextDownloadAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default);
}

/// <summary>Stored width, age cutoff, and enabled modalities. Rules stay in Core.</summary>
public interface IIndexPolicy
{
    StoredWidth TextStoredWidth { get; }

    void EnsureStorableWidth(Modality modality, int width);
}

/// <summary>Mean-pooled 768-d text embedding from ONNX. Implemented in the MAUI head.</summary>
public interface ITextEmbeddingEncoder
{
    ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default);
}

/// <summary>512-d text index backed by ZVec. Implemented outside Core.</summary>
public interface ITextVectorIndex
{
    bool TryInsert(TextIndexInsert insert);

    IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default);
}

/// <summary>One-shot read of granted text files for indexing.</summary>
public interface IGrantedTextIngestService
{
    Task IngestGrantedTextAsync(CancellationToken cancellationToken = default);
}

/// <summary>Hybrid text search orchestration.</summary>
public interface ITextSearchService
{
    Task<IReadOnlyList<TextSearchHit>> FindAsync(string query, IReadOnlyList<ScopeFolderGrant> grants, CancellationToken cancellationToken = default);
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

/// <summary>OS device name for first-launch minting only.</summary>
public interface IDeviceOsNameProvider
{
    string GetOsDeviceName();
}

/// <summary>Persists the local device record.</summary>
public interface IDeviceRecordStore
{
    DeviceRecordState? Load();

    void Save(DeviceRecordState state);
}

/// <summary>Local device id and display name for this install.</summary>
public interface IDeviceRecordService
{
    string GetDeviceId();

    string GetOriginalOsName();

    string GetDisplayName();

    DisplayNameSetResult TrySetDisplayName(string displayName);

    string GetInitials();
}

/// <summary>Person's age cutoff for media shrink (stored only in phase 1).</summary>
public interface IAgeCutoffPreferenceStore
{
    AgeCutoffYears Get();

    void Set(AgeCutoffYears years);
}

public enum RecallShellKind
{
    Unknown = 0,
    Windows = 1,
    Android = 2,
    Ios = 3,
    MacCatalyst = 4,
}

/// <summary>Which MAUI shell is running (for copy such as the iPhone grant note).</summary>
public interface IRecallShellKind
{
    RecallShellKind Kind { get; }
}

/// <summary>Reopens security-scoped or SAF grants after restart.</summary>
public interface IGrantedFolderAccessService
{
    void RestorePersistedAccess();
}
