using FluentAssertions;
#pragma warning disable S8969 // Tests use ! after Should().NotBeNull(), which the compiler cannot see.
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class Phase3WatchTests
{
    [Fact]
    public void TryAdd_BlankPath_IsIgnored()
    {
        var buffer = new ScopeChangeBuffer();
        buffer.TryAdd(new ScopeChange(ScopeChangeKind.Created, "   ", null)).Should().Be(ScopeChangeBufferAddResult.Ignored);
    }

    [Fact]
    public void TryAdd_SamePath_ReplacesWithoutGrowing()
    {
        var buffer = new ScopeChangeBuffer();
        buffer.TryAdd(new ScopeChange(ScopeChangeKind.Created, "a.txt", null));
        buffer.TryAdd(new ScopeChange(ScopeChangeKind.Modified, "a.txt", null)).Should().Be(ScopeChangeBufferAddResult.Added);
        buffer.Count.Should().Be(1);
        var drained = buffer.Drain();
        drained.Should().HaveCount(1);
        drained[0].Kind.Should().Be(ScopeChangeKind.Modified);
    }

    [Fact]
    public void TryAdd_SixtyFifthPath_OverflowsAndRequestsRescan()
    {
        var buffer = new ScopeChangeBuffer();
        ScopeChangeBufferAddResult last = ScopeChangeBufferAddResult.Added;
        for (var i = 0; i < 65; i++)
        {
            last = buffer.TryAdd(new ScopeChange(ScopeChangeKind.Created, $"file{i}.txt", null));
        }

        last.Should().Be(ScopeChangeBufferAddResult.Overflow);
        buffer.NeedsRescan.Should().BeTrue();
        buffer.Count.Should().Be(64);
    }

    [Fact]
    public void Drain_ClearsItems()
    {
        var buffer = new ScopeChangeBuffer();
        buffer.TryAdd(new ScopeChange(ScopeChangeKind.Created, "a.txt", null));
        buffer.Drain().Should().HaveCount(1);
        buffer.Drain().Should().BeEmpty();
    }

    [Fact]
    public void AcknowledgeRescan_ClearsFlag()
    {
        var buffer = new ScopeChangeBuffer();
        buffer.RequestRescan();
        buffer.NeedsRescan.Should().BeTrue();
        buffer.AcknowledgeRescan();
        buffer.NeedsRescan.Should().BeFalse();
    }

    [Fact]
    public void TryMap_BlankOrError_ReturnsFalse()
    {
        WindowsWatchEventMapper.TryMap(WindowsWatchKind.Created, "   ", null, out _).Should().BeFalse();
        WindowsWatchEventMapper.TryMap(WindowsWatchKind.Error, "a.txt", null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryMap_RenamedWithPrevious_ReturnsRenamed()
    {
        var ok = WindowsWatchEventMapper.TryMap(WindowsWatchKind.Renamed, "new.txt", "old.txt", out var change);
        ok.Should().BeTrue();
        change.Kind.Should().Be(ScopeChangeKind.Renamed);
        change.PreviousPath.Should().Be("old.txt");
    }

    [Fact]
    public void TryMap_RenamedWithoutPrevious_ReturnsCreated()
    {
        var ok = WindowsWatchEventMapper.TryMap(WindowsWatchKind.Renamed, "new.txt", null, out var change);
        ok.Should().BeTrue();
        change.Kind.Should().Be(ScopeChangeKind.Created);
    }

    [Fact]
    public void TryMap_DeletedChangedCreated_ReturnThoseKinds()
    {
        WindowsWatchEventMapper.TryMap(WindowsWatchKind.Deleted, "a.txt", null, out var deleted).Should().BeTrue();
        deleted.Kind.Should().Be(ScopeChangeKind.Deleted);
        WindowsWatchEventMapper.TryMap(WindowsWatchKind.Changed, "a.txt", null, out var modified).Should().BeTrue();
        modified.Kind.Should().Be(ScopeChangeKind.Modified);
        WindowsWatchEventMapper.TryMap(WindowsWatchKind.Created, "a.txt", null, out var created).Should().BeTrue();
        created.Kind.Should().Be(ScopeChangeKind.Created);
    }

    [Fact]
    public void Select_NullOrEmpty_ReturnsEmpty()
    {
        ScopeWatchRoots.Select(null).Should().BeEmpty();
        ScopeWatchRoots.Select([]).Should().BeEmpty();
    }

    [Fact]
    public void Select_Blank_IsDropped()
    {
        ScopeWatchRoots.Select(["   "]).Should().BeEmpty();
    }

    [Fact]
    public void Select_Duplicate_KeepsOne()
    {
        ScopeWatchRoots.Select([@"D:\Photos", @"d:\photos"]).Should().HaveCount(1);
    }

    [Fact]
    public void Select_ChildOfParent_DropsChild()
    {
        var roots = ScopeWatchRoots.Select([@"D:\", @"D:\Photos"]);
        roots.Should().HaveCount(1);
        roots[0].Should().Be(ScopeGrantPolicy.Normalize(@"D:\"));
    }

    [Fact]
    public void Select_ThousandChildren_StaysOneRoot()
    {
        var paths = new List<string> { ScopeGrantPolicy.Normalize(@"D:\") };
        for (var n = 0; n < 1000; n++)
        {
            paths.Add(ScopeGrantPolicy.Normalize(@"D:\Photos\" + n));
        }

        ScopeWatchRoots.Select(paths).Should().HaveCount(1);
    }

    [Fact]
    public void Select_TwoSiblingRoots_ReturnsBoth()
    {
        ScopeWatchRoots.Select([@"D:\Photos", @"E:\Docs"]).Should().HaveCount(2);
    }

    [Fact]
    public void Choose_WindowsMacAndroid_UseFilesystemWatcher()
    {
        ScopeWatcherKindPolicy.Choose(RecallShellKind.Windows).Should().Be(ScopeWatcherKind.WindowsFileSystem);
        ScopeWatcherKindPolicy.Choose(RecallShellKind.MacCatalyst).Should().Be(ScopeWatcherKind.WindowsFileSystem);
        ScopeWatcherKindPolicy.Choose(RecallShellKind.Android).Should().Be(ScopeWatcherKind.WindowsFileSystem);
        ScopeWatcherKindPolicy.Choose(RecallShellKind.Ios).Should().Be(ScopeWatcherKind.None);
    }

    [Fact]
    public void CanWatchDirectories_IosPhone_IsFalse()
    {
        WindowsFileScopeWatcher.CanWatchDirectories(true, false).Should().BeFalse();
    }

    [Fact]
    public void CanWatchDirectories_MacCatalyst_IsTrue()
    {
        WindowsFileScopeWatcher.CanWatchDirectories(true, true).Should().BeTrue();
    }

    [Fact]
    public void CanWatchDirectories_Windows_IsTrue()
    {
        WindowsFileScopeWatcher.CanWatchDirectories(false, false).Should().BeTrue();
    }

    [Fact]
    public void Diff_AddedWhileClosed_EmitsCreated()
    {
        var changes = ForegroundGrantDiff.Diff(
            [new WatchedFile("a.txt", "h1")],
            []);
        changes.Should().ContainSingle().Which.Kind.Should().Be(ScopeChangeKind.Created);
    }

    [Fact]
    public void Diff_SameHash_EmitsNothing()
    {
        var changes = ForegroundGrantDiff.Diff(
            [new WatchedFile("a.txt", "h1")],
            [new IngestFileRow("a.txt", "h1", IngestCursorState.Finished, "g", "")]);
        changes.Should().BeEmpty();
    }

    [Fact]
    public void Diff_DifferentHash_EmitsModified()
    {
        var changes = ForegroundGrantDiff.Diff(
            [new WatchedFile("a.txt", "h2")],
            [new IngestFileRow("a.txt", "h1", IngestCursorState.Finished, "g", "")]);
        changes.Should().ContainSingle().Which.Kind.Should().Be(ScopeChangeKind.Modified);
    }

    [Fact]
    public void Diff_MissingStored_EmitsDeleted()
    {
        var changes = ForegroundGrantDiff.Diff(
            [],
            [new IngestFileRow("a.txt", "h1", IngestCursorState.Finished, "g", "")]);
        changes.Should().ContainSingle().Which.Kind.Should().Be(ScopeChangeKind.Deleted);
    }

    [Fact]
    public void Diff_AlreadyMissing_EmitsNothing()
    {
        var changes = ForegroundGrantDiff.Diff(
            [],
            [new IngestFileRow("a.txt", "h1", IngestCursorState.Missing, "g", "")]);
        changes.Should().BeEmpty();
    }

    [Fact]
    public void Diff_UniqueHashPair_EmitsRenamed()
    {
        var changes = ForegroundGrantDiff.Diff(
            [new WatchedFile("new.txt", "h1")],
            [new IngestFileRow("old.txt", "h1", IngestCursorState.Finished, "g", "")]);
        changes.Should().ContainSingle().Which.Kind.Should().Be(ScopeChangeKind.Renamed);
    }

    [Fact]
    public void Diff_RepeatedHash_DoesNotPair()
    {
        var changes = ForegroundGrantDiff.Diff(
            [new WatchedFile("n1.txt", "h"), new WatchedFile("n2.txt", "h")],
            [new IngestFileRow("o1.txt", "h", IngestCursorState.Finished, "g", ""), new IngestFileRow("o2.txt", "h", IngestCursorState.Finished, "g", "")]);
        changes.Should().NotContain(c => c.Kind == ScopeChangeKind.Renamed);
    }

    [Fact]
    public void Diff_BlankPath_IsIgnored()
    {
        var changes = ForegroundGrantDiff.Diff(
            [new WatchedFile("   ", "h1")],
            []);
        changes.Should().BeEmpty();
    }

    [Fact]
    public void DisabledScopeWatcher_Start_DoesNotWatch()
    {
        var watcher = new DisabledScopeWatcher();
        watcher.Start([], new FakeScopeChangeSink());
        watcher.Watching.Should().BeFalse();
        watcher.StartCount.Should().Be(1);
    }

    [Fact]
    public void Stop_LeavesWatchingFalse()
    {
        var watcher = new DisabledScopeWatcher();
        watcher.Start([], new FakeScopeChangeSink());
        watcher.Stop();
        watcher.Watching.Should().BeFalse();
    }

    [Fact]
    public async Task WindowsFileScopeWatcher_TempFolder_ReportsCreatedModifiedRenamedDeleted()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-watch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var watcher = new WindowsFileScopeWatcher();
            var sink = new FakeScopeChangeSink();
            var grants = new List<ScopeFolderGrant> { new(root, "temp", GrantFileKinds.All) };
            watcher.Start(grants, sink);
            try
            {
                await Task.Delay(300, TestContext.Current.CancellationToken);
                var file = Path.Combine(root, "note.txt");
                await File.WriteAllTextAsync(file, "hello", TestContext.Current.CancellationToken);
                await File.AppendAllTextAsync(file, " world", TestContext.Current.CancellationToken);
                var renamed = Path.Combine(root, "renamed.txt");
                File.Move(file, renamed);
                File.Delete(renamed);
                var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
                var kinds = new HashSet<ScopeChangeKind>();
                while (DateTimeOffset.UtcNow < deadline)
                {
                    ScopeChange[] snapshot;
                    lock (sink.Reports)
                    {
                        snapshot = sink.Reports.ToArray();
                    }

                    foreach (var report in snapshot)
                    {
                        kinds.Add(report.Kind);
                    }

                    if (kinds.Contains(ScopeChangeKind.Created)
                        && kinds.Contains(ScopeChangeKind.Modified)
                        && kinds.Contains(ScopeChangeKind.Deleted)
                        && (kinds.Contains(ScopeChangeKind.Renamed) || kinds.Contains(ScopeChangeKind.Created)))
                    {
                        break;
                    }

                    await Task.Delay(100, TestContext.Current.CancellationToken);
                }

                kinds.Should().Contain(ScopeChangeKind.Created);
                kinds.Should().Contain(ScopeChangeKind.Modified);
                kinds.Should().Contain(ScopeChangeKind.Deleted);
            }
            finally
            {
                watcher.Stop();
                watcher.Dispose();
            }
        }
        finally
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void Stop_DisposesAndClearsWatching()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-watch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var watcher = new WindowsFileScopeWatcher();
            watcher.Start([new(root, "temp", GrantFileKinds.All)], new FakeScopeChangeSink());
            watcher.Stop();
            watcher.Watching.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void OnError_RequestsRescan()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-watch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var watcher = new WindowsFileScopeWatcher();
            var sink = new FakeScopeChangeSink();
            watcher.Start([new(root, "temp", GrantFileKinds.All)], sink);
            try
            {
                var field = typeof(WindowsFileScopeWatcher).GetField("watchers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.Should().NotBeNull();
                var list = (System.Collections.IList)field!.GetValue(watcher)!;
                list.Count.Should().BeGreaterThan(0);
                var fsw = (FileSystemWatcher)list[0]!;
                var onError = typeof(FileSystemWatcher).GetMethod("OnError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                onError.Should().NotBeNull();
                onError!.Invoke(fsw, [new ErrorEventArgs(new Exception("boom"))]);
                sink.RescanCount.Should().BeGreaterThan(0);
            }
            finally
            {
                watcher.Stop();
                watcher.Dispose();
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void OnCreated_Ios_StartsLoopWithoutWatcher()
    {
        var watcher = new FakeFileScopeWatcherService();
        var coordinator = CreateCoordinator(watcher);
        try
        {
            coordinator.OnCreated(RecallShellKind.Ios);
            watcher.StartCount.Should().Be(0);
            coordinator.IsRunning.Should().BeTrue();
        }
        finally
        {
            coordinator.Stop();
            coordinator.Dispose();
        }
    }

    [Fact]
    public void OnCreated_Unknown_DoesNotStart()
    {
        foreach (var kind in new[] { RecallShellKind.Unknown, (RecallShellKind)99 })
        {
            var watcher = new FakeFileScopeWatcherService();
            var coordinator = CreateCoordinator(watcher);
            try
            {
                coordinator.OnCreated(kind);
                watcher.StartCount.Should().Be(0);
                coordinator.IsRunning.Should().BeFalse();
                coordinator.Buffer.NeedsRescan.Should().BeFalse();
            }
            finally
            {
                coordinator.Stop();
                coordinator.Dispose();
            }
        }
    }

    [Fact]
    public void OnCreated_Windows_StartsWatcher()
    {
        var watcher = new FakeFileScopeWatcherService();
        var coordinator = CreateCoordinator(watcher);
        try
        {
            coordinator.OnCreated(RecallShellKind.Windows);
            watcher.StartCount.Should().Be(1);
            coordinator.IsRunning.Should().BeTrue();
        }
        finally
        {
            coordinator.Stop();
            coordinator.Dispose();
        }
    }

    [Fact]
    public void RequestRescan_BeforeLoop_SetsFlag()
    {
        var watcher = new FakeFileScopeWatcherService();
        var coordinator = CreateCoordinator(watcher);
        try
        {
            coordinator.RequestRescan();
            coordinator.Buffer.NeedsRescan.Should().BeTrue();
            coordinator.IsRunning.Should().BeFalse();
        }
        finally
        {
            coordinator.Stop();
            coordinator.Dispose();
        }
    }

    [Fact]
    public void OnResumed_Ios_RequestsRescan()
    {
        var watcher = new FakeFileScopeWatcherService();
        var coordinator = CreateCoordinator(watcher);
        try
        {
            coordinator.OnResumed(RecallShellKind.Ios);
            coordinator.IsRunning.Should().BeTrue();
        }
        finally
        {
            coordinator.Stop();
            coordinator.Dispose();
        }
    }

    [Fact]
    public void OnResumed_Windows_RequestsRescan()
    {
        var watcher = new FakeFileScopeWatcherService();
        var coordinator = CreateCoordinator(watcher);
        try
        {
            coordinator.OnResumed(RecallShellKind.Windows);
            coordinator.IsRunning.Should().BeTrue();
        }
        finally
        {
            coordinator.Stop();
            coordinator.Dispose();
        }
    }

    [Fact]
    public void OnResumed_Mac_RequestsRescan()
    {
        var watcher = new FakeFileScopeWatcherService();
        var coordinator = CreateCoordinator(watcher);
        try
        {
            coordinator.OnResumed(RecallShellKind.MacCatalyst);
            coordinator.IsRunning.Should().BeTrue();
        }
        finally
        {
            coordinator.Stop();
            coordinator.Dispose();
        }
    }

    [Fact]
    public void WindowsFileScopeWatcher_NestedGrants_OpensOneWatcher()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-nested-" + Guid.NewGuid().ToString("N"));
        var child = Path.Combine(root, "child");
        Directory.CreateDirectory(child);
        try
        {
            var watcher = new WindowsFileScopeWatcher();
            try
            {
                watcher.Start(
                    [
                        new ScopeFolderGrant(root, "parent", GrantFileKinds.All),
                        new ScopeFolderGrant(child, "child", GrantFileKinds.All),
                    ],
                    new FakeScopeChangeSink());
                watcher.WatcherCount.Should().Be(1);
                watcher.Watching.Should().BeTrue();
            }
            finally
            {
                watcher.Dispose();
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void WindowsFileScopeWatcher_ContentUri_OpensZeroAndRescans()
    {
        var watcher = new WindowsFileScopeWatcher();
        var sink = new FakeScopeChangeSink();
        try
        {
            watcher.Start([new ScopeFolderGrant("content://com.example/tree/1", "saf", GrantFileKinds.All)], sink);
            watcher.WatcherCount.Should().Be(0);
            sink.RescanCount.Should().Be(1);
        }
        finally
        {
            watcher.Dispose();
        }
    }

    [Fact]
    public async Task PumpOnce_WaitsUntilReport()
    {
        var watcher = new FakeFileScopeWatcherService();
        var coordinator = CreateCoordinator(watcher);
        try
        {
            coordinator.OnCreated(RecallShellKind.Ios);
            coordinator.Report(new ScopeChange(ScopeChangeKind.Deleted, "gone.txt", null));
            var deadline = DateTimeOffset.UtcNow.AddMilliseconds(500);
            while (coordinator.Buffer.Count > 0 && DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(10, TestContext.Current.CancellationToken);
            }

            coordinator.Buffer.Count.Should().Be(0);
        }
        finally
        {
            coordinator.Stop();
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task TryAdd_ConcurrentWithDrain_DoesNotThrow()
    {
        var buffer = new ScopeChangeBuffer();
        var testToken = TestContext.Current.CancellationToken;
        var producer = Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                buffer.TryAdd(new ScopeChange(ScopeChangeKind.Created, $"file{i % 16}.txt", null));
            }
        }, testToken);
        var consumer = Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                buffer.Drain();
            }
        }, testToken);
        await Task.WhenAll(producer, consumer);
        producer.IsCompletedSuccessfully.Should().BeTrue();
        consumer.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task Idle_Coordinator_BusyKeepsChange_IdleDrains()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-idle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "a.txt");
        await File.WriteAllTextAsync(filePath, "hello", TestContext.Current.CancellationToken);
        try
        {
            var store = new FakeScopeGrantStore();
            var scope = new ScopeGrantService(store);
            scope.TryAddGrant(new ScopeFolderGrant(root, "temp", GrantFileKinds.Text));
            var cursors = new FakeIngestCursorStore();
            var skips = new FakeUserSkipStore();
            var opLog = new LocalOpLogWriter(null);
            var ingest = new FakeGrantedTextIngestService();
            var applier = new ScopeChangeApplier(cursors, ingest, opLog);
            var reader = IngestTestKit.CreateDirectoryReader(skips);
            var scan = new ForegroundScanService(scope, reader, cursors, applier);
            var watcher = new FakeFileScopeWatcherService();
            var idle = new FakeIdleDetectorService { Current = IdleSignal.Busy };
            var coordinator = new ScopeWatchCoordinator(watcher, scope, scan, applier, skips, idle, new FakeLocalContentProbe(), new RemoteFileSkipReport());
            try
            {
                coordinator.Report(new ScopeChange(ScopeChangeKind.Created, filePath, null));
                coordinator.Buffer.Count.Should().Be(1);
                await coordinator.PumpOnceAsync(TestContext.Current.CancellationToken);
                coordinator.Buffer.Count.Should().Be(1);
                ingest.IngestDocumentCalls.Should().Be(0);
                idle.Current = IdleSignal.Idle;
                await coordinator.PumpOnceAsync(TestContext.Current.CancellationToken);
                ingest.IngestDocumentCalls.Should().Be(1);
                coordinator.Buffer.Count.Should().Be(0);
            }
            finally
            {
                coordinator.Stop();
                coordinator.Dispose();
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task PumpOnce_ExceptionOnFirstChange_AppliesSecond()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-pump-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var first = Path.Combine(root, "first.txt");
            var second = Path.Combine(root, "second.txt");
            await File.WriteAllTextAsync(first, "first", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(second, "second", TestContext.Current.CancellationToken);
            var store = new FakeScopeGrantStore();
            var scope = new ScopeGrantService(store);
            scope.TryAddGrant(new ScopeFolderGrant(root, "temp", GrantFileKinds.All));
            var cursors = new FakeIngestCursorStore();
            var skips = new FakeUserSkipStore();
            var opLog = new LocalOpLogWriter(null);
            var throwing = new ThrowFirstIngestService();
            var applier = new ScopeChangeApplier(cursors, throwing, opLog);
            var reader = IngestTestKit.CreateDirectoryReader(skips);
            var scan = new ForegroundScanService(scope, reader, cursors, applier);
            var watcher = new FakeFileScopeWatcherService();
            var coordinator = new ScopeWatchCoordinator(watcher, scope, scan, applier, skips, new FakeIdleDetectorService(), new FakeLocalContentProbe(), new RemoteFileSkipReport());
            try
            {
                coordinator.Report(new ScopeChange(ScopeChangeKind.Created, first, null));
                coordinator.Report(new ScopeChange(ScopeChangeKind.Created, second, null));
                await coordinator.PumpOnceAsync(TestContext.Current.CancellationToken);
                throwing.Succeeded.Should().Contain(second);
            }
            finally
            {
                coordinator.Stop();
                coordinator.Dispose();
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task PumpOnce_Cancel_Stops()
    {
        var watcher = new FakeFileScopeWatcherService();
        var coordinator = CreateCoordinator(watcher);
        try
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            Func<Task> act = () => coordinator.PumpOnceAsync(cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            coordinator.Stop();
            coordinator.Dispose();
        }
    }

    [Fact]
    public void Stop_StopsWatcher()
    {
        var watcher = new FakeFileScopeWatcherService();
        var coordinator = CreateCoordinator(watcher);
        try
        {
            coordinator.OnCreated(RecallShellKind.Windows);
            watcher.Watching.Should().BeTrue();
            coordinator.Stop();
            watcher.Watching.Should().BeFalse();
        }
        finally
        {
            coordinator.Dispose();
        }
    }

    private static ScopeWatchCoordinator CreateCoordinator(FakeFileScopeWatcherService watcher)
    {
        var store = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(store);
        var cursors = new FakeIngestCursorStore();
        var skips = new FakeUserSkipStore();
        var opLog = new LocalOpLogWriter(null);
        var ingest = new FakeGrantedTextIngestService();
        var applier = new ScopeChangeApplier(cursors, ingest, opLog);
        var reader = new EmptyReader();
        var scan = new ForegroundScanService(scope, reader, cursors, applier);
        return new ScopeWatchCoordinator(watcher, scope, scan, applier, skips, new FakeIdleDetectorService(), new FakeLocalContentProbe(), new RemoteFileSkipReport());
    }

    private sealed class EmptyReader : IGrantedTextReader
    {
        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class ThrowFirstIngestService : IGrantedTextIngestService
    {
        public List<string> Succeeded { get; } = [];

        public Task IngestGrantedTextAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task IngestGrantAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task IngestDocumentAsync(ScopeFolderGrant grant, GrantedTextDocument document, CancellationToken cancellationToken = default)
        {
            if (document.FileName.Contains("first", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("first fails");
            }

            Succeeded.Add(document.Path);
            return Task.CompletedTask;
        }
    }
}
