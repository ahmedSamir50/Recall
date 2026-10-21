using Recall.Core.Checkpoints;
using Recall.Core.Device;
using Recall.Core.Download;
using Recall.Core.Indexing;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Text;
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
    private readonly Dictionary<string, IngestChunkRow> chunkRows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IngestCursorState> grants = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IngestAliasRow> aliases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Bm25OnlyRow> bm25Only = new(StringComparer.Ordinal);
    private readonly HashSet<string> pending = new(StringComparer.Ordinal);

    public bool AllowWrite { get; set; } = true;

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
        return chunkRows.Values
            .Where(c => string.Equals(c.Path, path, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IngestChunkRow? FindChunk(string contentId)
    {
        return chunkRows.TryGetValue(contentId, out var row) ? row : null;
    }

    public void MarkReading(string path, string contentHash, string grantPath)
    {
        files[path] = new IngestFileRow(path, contentHash, IngestCursorState.Reading, grantPath, string.Empty);
    }

    public void MarkFinished(string path, string contentHash, string grantPath, string encoderHash)
    {
        FinishPath(path, contentHash, grantPath, encoderHash, []);
    }

    public void RenamePath(string previousPath, string nextPath)
    {
        if (!files.TryGetValue(previousPath, out var row))
        {
            return;
        }

        files.Remove(previousPath);
        files[nextPath] = row with { Path = nextPath };
        foreach (var id in chunkRows.Keys.ToList())
        {
            var chunk = chunkRows[id];
            if (string.Equals(chunk.Path, previousPath, StringComparison.OrdinalIgnoreCase) && !chunk.Retired)
            {
                chunkRows[id] = chunk with { Path = nextPath };
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
            files[path] = new IngestFileRow(path, string.Empty, IngestCursorState.Missing, string.Empty, string.Empty);
        }

        foreach (var id in chunkRows.Keys.ToList())
        {
            var chunk = chunkRows[id];
            if (string.Equals(chunk.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                chunkRows[id] = chunk with { Missing = true };
            }
        }

        foreach (var key in aliases.Keys.ToList())
        {
            var alias = aliases[key];
            if (string.Equals(alias.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                aliases[key] = alias with { Missing = true };
            }
        }
    }

    public void RetirePath(string path)
    {
        foreach (var id in chunkRows.Keys.ToList())
        {
            var chunk = chunkRows[id];
            if (string.Equals(chunk.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                chunkRows[id] = chunk with { Retired = true };
            }
        }

        foreach (var key in aliases.Keys.ToList())
        {
            var alias = aliases[key];
            if (string.Equals(alias.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                aliases[key] = alias with { Retired = true };
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

    public bool AnyLivePath(string contentId)
    {
        return aliases.Values.Any(a => string.Equals(a.ContentId, contentId, StringComparison.Ordinal)
            && !a.Missing && !a.Retired);
    }

    public bool AnyOtherLivePath(string contentId, string path)
    {
        if (string.IsNullOrWhiteSpace(contentId) || string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Content id and path must not be blank.");
        }

        return aliases.Values.Any(a => string.Equals(a.ContentId, contentId, StringComparison.Ordinal)
            && !string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase)
            && !a.Missing && !a.Retired);
    }

    public IReadOnlyList<string> ListRetiredIds(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        return aliases.Values
            .Where(a => string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase) && a.Retired)
            .Select(a => a.ContentId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public void AddAlias(string contentId, string path)
    {
        aliases[contentId + "|" + path] = new IngestAliasRow(contentId, path, false, false);
    }

    public void MoveLiveAliases(string fromContentId, string toContentId)
    {
        var keysToMove = aliases.Keys.Where(k => k.StartsWith(fromContentId + "|", StringComparison.Ordinal)).ToList();
        foreach (var key in keysToMove)
        {
            var alias = aliases[key];
            aliases.Remove(key);
            var newKey = toContentId + "|" + alias.Path;
            aliases[newKey] = alias with { ContentId = toContentId };
        }
    }

    public void PutBm25Only(string contentId, string path, string rawText)
    {
        bm25Only[contentId] = new Bm25OnlyRow(contentId, path, rawText);
    }

    public IReadOnlyList<Bm25OnlyRow> ListBm25Only()
    {
        return bm25Only.Values.ToList();
    }

    public IReadOnlyList<IngestAliasRow> ListLiveAliases(string contentId)
    {
        return aliases.Values
            .Where(a => string.Equals(a.ContentId, contentId, StringComparison.Ordinal)
                && !a.Missing && !a.Retired)
            .ToList();
    }

    public IReadOnlyList<string> ListPending(int limit)
    {
        return pending.OrderBy(id => id, StringComparer.Ordinal).Take(limit).ToList();
    }

    public int PendingCount()
    {
        return pending.Count;
    }

    public void RemovePending(IReadOnlyList<string> contentIds)
    {
        foreach (var id in contentIds)
        {
            pending.Remove(id);
        }
    }

    public void EnqueuePending(string contentId)
    {
        pending.Add(contentId);
    }

    public bool TryEnterWrite(out CheckpointLease? lease)
    {
        if (!AllowWrite)
        {
            lease = null;
            return false;
        }

        lease = EnterWrite();
        return true;
    }

    public CheckpointLease EnterWrite()
    {
        return new CheckpointLease(static () => { }, static () => { });
    }

    public void FinishPath(string path, string contentHash, string grantPath, string encoderHash, IReadOnlyList<ChunkWrite> chunks)
    {
        files[path] = new IngestFileRow(path, contentHash, IngestCursorState.Finished, grantPath, encoderHash);
        var newIds = chunks.Select(c => c.ContentId).ToHashSet(StringComparer.Ordinal);
        foreach (var id in chunkRows.Keys.ToList())
        {
            var chunk = chunkRows[id];
            if (string.Equals(chunk.Path, path, StringComparison.OrdinalIgnoreCase) && !chunk.Retired && !newIds.Contains(id))
            {
                chunkRows[id] = chunk with { Retired = true };
            }
        }

        foreach (var key in aliases.Keys.ToList())
        {
            var alias = aliases[key];
            if (string.Equals(alias.Path, path, StringComparison.OrdinalIgnoreCase) && !alias.Retired && !newIds.Contains(alias.ContentId))
            {
                aliases[key] = alias with { Retired = true };
            }
        }

        foreach (var chunk in chunks)
        {
            if (chunkRows.TryGetValue(chunk.ContentId, out var existing)
                && string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase)
                && !existing.Retired)
            {
                chunkRows[chunk.ContentId] = existing with { Ordinal = chunk.Ordinal, Missing = false };
            }
            else if (!chunkRows.ContainsKey(chunk.ContentId))
            {
                chunkRows[chunk.ContentId] = new IngestChunkRow(chunk.ContentId, path, chunk.Ordinal, false, false);
            }

            AddAlias(chunk.ContentId, path);
        }
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
    public int Calls { get; private set; }

    public Func<Stream, IReadOnlyList<VideoFrame>>? Handler { get; set; }

    public IReadOnlyList<VideoFrame> SampleEveryTwoSeconds(Stream media)
    {
        ArgumentNullException.ThrowIfNull(media);
        Calls++;
        return Handler?.Invoke(media) ?? [];
    }
}

public sealed class FakeSecureStorageService : ISecureStorageService
{
}

public sealed class FakeIdleDetectorService : IIdleDetectorService
{
    public IdleSignal Current { get; set; } = IdleSignal.Idle;
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

    public TextModelDownloadSnapshot VisionSnapshot { get; set; } = new(TextModelDownloadPhase.NotStarted, 0, null);

    public int VisionDownloadCalls { get; private set; }

    public TextModelDownloadSnapshot GetVisionDownloadSnapshot() => VisionSnapshot;

    public Task DownloadVisionModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    {
        VisionDownloadCalls++;
        if (VisionSnapshot.Phase == TextModelDownloadPhase.NotStarted)
        {
            VisionSnapshot = VisionSnapshot with { Phase = TextModelDownloadPhase.Downloading };
        }

        return Task.CompletedTask;
    }
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

    public int IngestGrantedTextCalls { get; private set; }

    public Task IngestGrantedTextAsync(CancellationToken cancellationToken = default)
    {
        IngestGrantedTextCalls++;
        return Task.CompletedTask;
    }

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

    public int DeleteCalls { get; private set; }

    public int ReconcileCalls { get; private set; }

    public string? LastFilter { get; private set; }

    public bool TryInsert(TextIndexInsert insert)
    {
        if (Inserts.Any(i => i.ContentId == insert.ContentId))
        {
            return false;
        }

        Inserts.Add(insert);
        return true;
    }

    public bool TryDelete(string contentId)
    {
        var removed = Inserts.RemoveAll(i => i.ContentId == contentId) > 0;
        if (removed)
        {
            DeleteCalls++;
        }

        return removed;
    }

    public int LastReconcileStateCount { get; private set; }

    public void Reconcile(IReadOnlyList<VectorLiveState> states)
    {
        ReconcileCalls++;
        LastReconcileStateCount = states.Count;
        LastFilter = states.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        LastFilter = request.Filter;
        return [];
    }
}

public sealed class FakeEncoderFileHash : IEncoderFileHash
{
    public string? TextHash { get; set; } = "test-hash";
}

public static class IngestTestKit
{
    public static DirectoryGrantedTextReader CreateDirectoryReader(IUserSkipStore? skips = null) =>
        new(
            skips ?? new FakeUserSkipStore(),
            new FakeLocalContentProbe(),
            new RemoteFileSkipReport(),
            new DatasetSkipReport());

    public static (TextIngestEngine Engine, LocalOpLogWriter OpLog, FakeIngestCursorStore Cursors) CreateEngineParts(
        ITextEmbeddingEncoder encoder,
        FakeTextVectorIndex index,
        string? oplogDir = null)
    {
        var dir = oplogDir ?? Path.Combine(Path.GetTempPath(), "recall-oplog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var cursors = new FakeIngestCursorStore();
        var opLog = new LocalOpLogWriter(dir);
        var engine = new TextIngestEngine(encoder, index, new FakeIndexPolicy(), new FakeTextTokenCounter(), opLog, cursors);
        return (engine, opLog, cursors);
    }

    public static GrantedTextIngestService CreateGrantedService(
        IScopeManagerService scope,
        IGrantedTextReader reader,
        TextIngestEngine engine,
        FakeIngestCursorStore cursors,
        FakeTextVectorIndex index,
        LocalOpLogWriter opLog,
        IEncoderFileHash? encoderFileHash = null) =>
        new(scope, reader, engine, cursors, index, opLog, encoderFileHash ?? new FakeEncoderFileHash());

    public static TextSearchService CreateSearchService(
        FakeTextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        FakeIngestCursorStore? cursors = null,
        IVisionEmbeddingEncoder? vision = null) =>
        new(encoder, index, new FakeIndexPolicy(), cursors ?? new FakeIngestCursorStore(), new FakeEncoderFileHash(), vision);
}

public sealed class FakeLocalContentProbe : ILocalContentProbe
{
    public LocalContentState Next { get; set; } = LocalContentState.Local;

    public LocalContentState Probe(string path) => Next;
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

public sealed class FakeVisionEmbeddingEncoder : IVisionEmbeddingEncoder
{
    public int Calls { get; private set; }

    public ReadOnlyMemory<byte> LastJpeg { get; private set; }

    public Action<ReadOnlyMemory<byte>, float[]>? OnEmbed { get; set; }

    public void EmbedJpeg(ReadOnlyMemory<byte> jpeg, float[] destination768)
    {
        Calls++;
        LastJpeg = jpeg;
        if (OnEmbed is not null)
        {
            OnEmbed(jpeg, destination768);
            return;
        }

        destination768[0] = 1;
    }
}

public sealed class FakeMediaVectorIndex : IMediaVectorIndex
{
    public List<string> CreatedCollections { get; } = [];

    public List<MediaIndexInsert> Inserts { get; } = [];

    public List<string> QueriedCollections { get; } = [];

    public List<MediaSearchRequest> Requests { get; } = [];

    public Func<MediaSearchRequest, IReadOnlyList<MediaSearchHit>>? OnSearch { get; set; }

    public void Create(string collectionName, int width)
    {
        CreatedCollections.Add(collectionName);
    }

    public bool TryInsert(string collectionName, MediaIndexInsert insert)
    {
        if (Inserts.Any(i => string.Equals(i.ContentId, insert.ContentId, StringComparison.Ordinal)))
        {
            return false;
        }

        Inserts.Add(insert);
        return true;
    }

    public bool TryDelete(string contentId) =>
        Inserts.RemoveAll(i => string.Equals(i.ContentId, contentId, StringComparison.Ordinal)) > 0;

    public IReadOnlyList<MediaSearchHit> Search(MediaSearchRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        QueriedCollections.Add(request.CollectionName);
        return OnSearch?.Invoke(request) ?? [];
    }
}

public sealed class FakePictureSearchService : IPictureSearchService
{
    public IReadOnlyList<MediaSearchHit> Result { get; set; } = [];

    public int Calls { get; private set; }

    public IReadOnlyList<ScopeFolderGrant> LastGrants { get; private set; } = [];

    public Task<IReadOnlyList<MediaSearchHit>> FindAsync(
        ReadOnlyMemory<byte> jpeg,
        IReadOnlyList<ScopeFolderGrant> grants,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastGrants = grants;
        return Task.FromResult(Result);
    }
}
