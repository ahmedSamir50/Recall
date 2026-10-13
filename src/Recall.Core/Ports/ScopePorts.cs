using Recall.Core.Scope;

namespace Recall.Core.Ports;

public interface IFileScopeWatcherService
{
}

public interface IScopeManagerService
{
    IReadOnlyList<ScopeFolderGrant> ListGrants();

    ScopeGrantAddResult TryAddGrant(ScopeFolderGrant grant);

    void RemoveGrant(string path);

    void SetGrantFileKinds(string path, GrantFileKinds kinds);

    bool CanContinue();
}

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

public interface IFolderPickerService
{
    Task<FolderPickerResult> PickFolderAsync(CancellationToken cancellationToken = default);
}

public interface IGrantedFolderAccessService
{
    void RestorePersistedAccess();
}

public interface IVideoFrameSamplerService
{
}

public interface IIdleDetectorService
{
}
