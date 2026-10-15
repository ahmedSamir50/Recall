using Recall.Core.Checkpoints;
using Recall.Core.Device;
using Recall.Core.Download;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Vocabulary;

namespace Recall.UnitTests.Fakes;

public sealed class FakeFileScopeWatcherService : IFileScopeWatcherService
{
    public int StartCount { get; private set; }

    public bool Watching { get; private set; }

    public void Start(IReadOnlyList<ScopeFolderGrant> grants, IScopeChangeSink sink)
    {
        StartCount++;
        Watching = true;
    }

    public void Stop()
    {
        Watching = false;
    }
}

public sealed class FakeScopeChangeSink : IScopeChangeSink
{
    public List<ScopeChange> Reports { get; } = [];

    public int RescanCount { get; private set; }

    public bool RescanRequested => RescanCount > 0;

    public void Report(ScopeChange change)
    {
        lock (Reports)
        {
            Reports.Add(change);
        }
    }

    public void RequestRescan()
    {
        RescanCount++;
    }
}

public sealed class FakeIngestCursorStore : IIngestCursorStore
{
    private readonly Dictionary<string, IngestFileRow> files = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IngestChunkRow> chunks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IngestCursorState> grants = new(StringComparer.OrdinalIgnoreCase);

    public IngestFileRow? GetFile(string path)
    {
        return files.TryGetValue(path, out var row) ? row : null;
    }

    public IReadOnlyList<IngestFileRow> ListFiles(string grantPath)
    {
        return files.Values
            .Where(f => string.Equals(
                ScopeGrantPolicy.Normalize(f.GrantPath),
                ScopeGrantPolicy.Normalize(grantPath),
                StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IReadOnlyList<IngestChunkRow> ListChunks(string path)
    {
        return chunks.Values
            .Where(c => string.Equals(c.Path, path, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IngestChunkRow? FindChunk(string contentId)
    {
        return chunks.TryGetValue(contentId, out var row) ? row : null;
    }

    public void MarkReading(string path, string contentHash, string grantPath)
    {
        files[path] = new IngestFileRow(path, contentHash, IngestCursorState.Reading, grantPath);
    }

    public void MarkFinished(string path, string contentHash, string grantPath, IReadOnlyList<(string ContentId, int Ordinal)> chunks)
    {
        if (chunks is not null)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var chunk in chunks)
            {
                if (!seen.Add(chunk.ContentId))
                {
                    throw new InvalidOperationException("Duplicate chunk id.");
                }

                if (this.chunks.TryGetValue(chunk.ContentId, out var existing)
                    && !string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Duplicate chunk id.");
                }
            }
        }

        foreach (var id in this.chunks.Keys.ToList())
        {
            if (string.Equals(this.chunks[id].Path, path, StringComparison.OrdinalIgnoreCase))
            {
                this.chunks.Remove(id);
            }
        }

        files[path] = new IngestFileRow(path, contentHash, IngestCursorState.Finished, grantPath);
        if (chunks is not null)
        {
            foreach (var chunk in chunks)
            {
                this.chunks[chunk.ContentId] = new IngestChunkRow(chunk.ContentId, path, chunk.Ordinal, false, false);
            }
        }
    }

    public void RenamePath(string previousPath, string nextPath)
    {
        if (!files.TryGetValue(previousPath, out var row))
        {
            return;
        }

        files.Remove(previousPath);
        files[nextPath] = row with { Path = nextPath };
        foreach (var id in chunks.Keys.ToList())
        {
            var chunk = chunks[id];
            if (string.Equals(chunk.Path, previousPath, StringComparison.OrdinalIgnoreCase) && !chunk.Retired)
            {
                chunks[id] = chunk with { Path = nextPath };
            }
        }
    }

    public void MarkMissing(string path)
    {
        if (files.TryGetValue(path, out var row))
        {
            files[path] = row with { State = IngestCursorState.Missing };
        }
        else
        {
            files[path] = new IngestFileRow(path, string.Empty, IngestCursorState.Missing, string.Empty);
        }

        foreach (var id in chunks.Keys.ToList())
        {
            var chunk = chunks[id];
            if (string.Equals(chunk.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                chunks[id] = chunk with { Missing = true };
            }
        }
    }

    public void RetirePath(string path)
    {
        foreach (var id in chunks.Keys.ToList())
        {
            var chunk = chunks[id];
            if (string.Equals(chunk.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                chunks[id] = chunk with { Retired = true };
            }
        }
    }

    public void MarkGrantReading(string grantPath)
    {
        grants[ScopeGrantPolicy.Normalize(grantPath)] = IngestCursorState.Reading;
        grants[grantPath] = IngestCursorState.Reading;
    }

    public void MarkGrantFinished(string grantPath)
    {
        grants[ScopeGrantPolicy.Normalize(grantPath)] = IngestCursorState.Finished;
        grants[grantPath] = IngestCursorState.Finished;
    }

    public bool IsGrantFinished(string grantPath)
    {
        if (grants.TryGetValue(grantPath, out var state))
        {
            return state == IngestCursorState.Finished;
        }

        return grants.TryGetValue(ScopeGrantPolicy.Normalize(grantPath), out var normalized) && normalized == IngestCursorState.Finished;
    }
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

    public void SetGrantFileKinds(string path, GrantFileKinds kinds) => inner.SetGrantFileKinds(path, kinds);

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

public sealed class FakeOnboardingCompletionStore : IOnboardingCompletionStore
{
    public bool Complete { get; set; }

    public bool IsComplete() => Complete;

    public void MarkComplete() => Complete = true;

    public void ResetToOnboarding() => Complete = false;
}

public sealed class FakeDebugAppReset : IDebugAppReset
{
    public bool IsEnabled { get; set; }

    public Task ResetAllLocalDataAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class FakeModelDownloadService : IModelDownloadService
{
    public TextModelDownloadSnapshot Snapshot { get; set; } = new(TextModelDownloadPhase.NotStarted, 0, null);

    public int DownloadCalls { get; private set; }

    public TextModelDownloadSnapshot GetTextDownloadSnapshot() => Snapshot;

    public Task DownloadTextModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    {
        DownloadCalls++;
        if (Snapshot.Phase == TextModelDownloadPhase.NotStarted)
        {
            Snapshot = Snapshot with { Phase = TextModelDownloadPhase.Downloading };
        }

        return Task.CompletedTask;
    }

    public void CancelTextDownload()
    {
    }

    public Task RetryTextDownloadAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public sealed class FakeIndexPolicy : IIndexPolicy
{
    public StoredWidth TextStoredWidth => StoredWidth.W512;

    public void EnsureStorableWidth(Modality modality, int width)
    {
    }
}

public sealed class FakeTextTokenCounter : ITextTokenCounter
{
    public int CountTokens(string text) => Math.Max(1, text.Length / 4);
}

public sealed class FakeTextEmbeddingEncoder : ITextEmbeddingEncoder
{
    public int Calls { get; private set; }

    public string LastPrefixed { get; private set; } = string.Empty;

    public ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
    {
        Calls++;
        LastPrefixed = prefixedText;
        var vector = new float[768];
        vector[0] = prefixedText.Length;
        return ValueTask.FromResult<ReadOnlyMemory<float>>(vector);
    }
}

public sealed class FakeGrantedTextIngestService : IGrantedTextIngestService
{
    public int IngestGrantCalls { get; private set; }

    public int IngestDocumentCalls { get; private set; }

    public Task IngestGrantedTextAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task IngestGrantAsync(string path, CancellationToken cancellationToken = default)
    {
        IngestGrantCalls++;
        return Task.CompletedTask;
    }

    public Task IngestDocumentAsync(ScopeFolderGrant grant, GrantedTextDocument document, CancellationToken cancellationToken = default)
    {
        IngestDocumentCalls++;
        return Task.CompletedTask;
    }
}

public sealed class FakeTextSearchService : ITextSearchService
{
    public Task<IReadOnlyList<TextSearchHit>> FindAsync(string query, IReadOnlyList<ScopeFolderGrant> grants, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TextSearchHit>>([]);
}

public sealed class FakeDeviceIdCandidateProviderPort : IDeviceIdCandidateProvider
{
    public DeviceIdCandidate GetCandidate() => new("fake", true);
}

public sealed class FakeRandomDeviceIdSourcePort : IRandomDeviceIdSource
{
    public string NewDeviceId() => "random";
}

public sealed class FakeTextVectorIndex : ITextVectorIndex
{
    public List<TextIndexInsert> Inserts { get; } = [];

    public TextSearchRequest? LastRequest { get; private set; }

    public bool TryInsert(TextIndexInsert insert)
    {
        if (Inserts.Any(i => i.ContentId == insert.ContentId))
        {
            return false;
        }

        Inserts.Add(insert);
        return true;
    }

    public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return [];
    }
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
    public string GetDeviceId() => "fake-device-id";

    public string GetOriginalOsName() => "Recall device";

    public string GetDisplayName() => "Recall device";

    public DisplayNameSetResult TrySetDisplayName(string displayName) => DisplayNameSetResult.Saved;

    public string GetInitials() => "RD";
}

public sealed class FakeDeviceRecordStore : IDeviceRecordStore
{
    public Recall.Core.Device.DeviceRecordState? Load() => null;

    public void Save(Recall.Core.Device.DeviceRecordState state)
    {
    }
}

public sealed class FakeDeviceOsNameProvider : IDeviceOsNameProvider
{
    public string OsName { get; set; } = "Test phone";

    public string GetOsDeviceName() => OsName;
}

public sealed class FakeAgeCutoffPreferenceStore : IAgeCutoffPreferenceStore
{
    public Recall.Core.Vocabulary.AgeCutoffYears Value { get; set; } = Recall.Core.Vocabulary.AgeCutoffYears.Two;

    public Recall.Core.Vocabulary.AgeCutoffYears Get() => Value;

    public void Set(Recall.Core.Vocabulary.AgeCutoffYears years) => Value = years;
}

public sealed class FakeRecallShellKind : IRecallShellKind
{
    public RecallShellKind Kind { get; set; } = RecallShellKind.Unknown;
}

public sealed class FakeGrantedFolderAccessService : IGrantedFolderAccessService
{
    public int RestoreCount { get; private set; }

    public void RestorePersistedAccess() => RestoreCount++;
}

public sealed class FakeUserSkipStore : IUserSkipStore
{
    public UserSkipAdditions Stored { get; private set; } = UserSkipAdditions.Empty;

    public UserSkipAdditions Load() => Stored;

    public void Save(UserSkipAdditions additions) => Stored = additions;
}
