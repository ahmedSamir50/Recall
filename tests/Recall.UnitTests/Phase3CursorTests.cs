using FluentAssertions;
#pragma warning disable S8969 // Tests use ! after Should().NotBeNull(), which the compiler cannot see.
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class Phase3CursorTests
{
    private static string NewTempDb()
    {
        return Path.Combine(Path.GetTempPath(), "recall-checkpoints-" + Guid.NewGuid().ToString("N"), CheckpointSql.FileName);
    }

    [Fact]
    public void Decide_NullReadingMissingAndChangedHash_Read()
    {
        IngestCursorPolicy.Decide(null, "h1").Should().Be(IngestCursorDecision.Read);
        IngestCursorPolicy.Decide(new IngestFileRow("a", "h1", IngestCursorState.Reading, "g", ""), "h1").Should().Be(IngestCursorDecision.Read);
        IngestCursorPolicy.Decide(new IngestFileRow("a", "h1", IngestCursorState.Missing, "g", ""), "h1").Should().Be(IngestCursorDecision.Read);
        IngestCursorPolicy.Decide(new IngestFileRow("a", "h1", IngestCursorState.Finished, "g", ""), "h2").Should().Be(IngestCursorDecision.Read);
    }

    [Fact]
    public void Decide_FinishedSameHash_Skip()
    {
        IngestCursorPolicy.Decide(new IngestFileRow("a", "h1", IngestCursorState.Finished, "g", ""), "h1").Should().Be(IngestCursorDecision.Skip);
    }

    [Fact]
    public void Decide_BlankHash_Throws()
    {
        Action act = () => IngestCursorPolicy.Decide(null, "   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResetTextIngest_ClearsFilesChunksAliasesBm25PendingAndGrants()
    {
        // Arrange
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.MarkReading("a.txt", "h1", "g");
            store.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
            store.PutBm25Only("bm25", "a.txt", "123");
            store.EnqueuePending("id1");
            store.MarkGrantFinished("g");

            // Act
            store.ResetTextIngest();

            // Assert
            store.GetFile("a.txt").Should().BeNull();
            store.ListChunks("a.txt").Should().BeEmpty();
            store.AnyLivePath("id1").Should().BeFalse();
            store.ListBm25Only().Should().BeEmpty();
            store.PendingCount().Should().Be(0);
            store.IsGrantFinished("g").Should().BeFalse();
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void MarkReading_Committed_SurvivesNewStore()
    {
        var db = NewTempDb();
        try
        {
            var first = new SqliteIngestCursorStore(db);
            first.MarkReading("a.txt", "h1", "g");
            var second = new SqliteIngestCursorStore(db);
            second.GetFile("a.txt")!.State.Should().Be(IngestCursorState.Reading);
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void SecondPath_SameBytes_FinishesAndBothPathsHit()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.MarkReading("a.txt", "h1", "g");
            store.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
            store.MarkReading("b.txt", "h2", "g");
            store.FinishPath("b.txt", "h2", "g", "enc", [new ChunkWrite("id1", 0)]);
            store.GetFile("b.txt")!.State.Should().Be(IngestCursorState.Finished);
            store.ListLiveAliases("id1").Select(a => a.Path).Should().BeEquivalentTo("a.txt", "b.txt");
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void MarkFinished_ThenDecide_SkipsSameHash()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.MarkReading("a.txt", "h1", "g");
            store.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
            var row = store.GetFile("a.txt");
            IngestCursorPolicy.Decide(row, "h1").Should().Be(IngestCursorDecision.Skip);
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void MarkFinished_SameContentId_ReplacesLiveChunk()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.MarkReading("a.txt", "h1", "g");
            store.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
            store.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 2)]);
            var chunks = store.ListChunks("a.txt");
            chunks.Should().HaveCount(1);
            chunks[0].Retired.Should().BeFalse();
            chunks[0].Ordinal.Should().Be(2);
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void RoundTrip_QuoteInPath_UsesParameter()
    {
        var path = @"D:\O'Brien\note.txt";
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.MarkReading(path, "h1", "g");
            store.GetFile(path)!.Path.Should().Be(path);
            var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Recall.Core", "Checkpoints", "SqliteIngestCursorStore.cs"));
            source.Should().Contain("@path");
            source.Should().NotContain(path);
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void RenamePath_MovesFileAndLiveChunks()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.MarkReading("old.txt", "h1", "g");
            store.FinishPath("old.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0), new ChunkWrite("id2", 1)]);
            store.RenamePath("old.txt", "new.txt");
            store.GetFile("old.txt").Should().BeNull();
            store.GetFile("new.txt").Should().NotBeNull();
            store.ListChunks("new.txt").Should().HaveCount(2);
            store.ListChunks("old.txt").Should().BeEmpty();
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void MarkMissing_KeepsChunkRowAndSetsMissing()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.MarkReading("a.txt", "h1", "g");
            store.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
            store.MarkMissing("a.txt");
            store.GetFile("a.txt")!.State.Should().Be(IngestCursorState.Missing);
            var chunks = store.ListChunks("a.txt");
            chunks.Should().HaveCount(1);
            chunks[0].Missing.Should().BeTrue();
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void RetirePath_SetsRetired()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.MarkReading("a.txt", "h1", "g");
            store.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
            store.RetirePath("a.txt");
            store.ListChunks("a.txt")[0].Retired.Should().BeTrue();
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void IsGrantFinished_OnlyWhenFinished()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.IsGrantFinished("g").Should().BeFalse();
            store.MarkGrantReading("g");
            store.IsGrantFinished("g").Should().BeFalse();
            store.MarkGrantFinished("g");
            store.IsGrantFinished("g").Should().BeTrue();
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void ReplaceDocumentPatches_SamePath_ListsOrdinals()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.ReplaceDocumentPatches("doc.pdf", [
                new ChunkWrite("pid0", 0, 1, 1, 0, 500),
                new ChunkWrite("pid1", 1, 1, 2, 480, 1200)]);
            var chunks = store.ListChunks("doc.pdf");
            chunks.Select(c => c.Ordinal).Should().Equal(0, 1);
            chunks[0].ContentId.Should().Be("pid0");
            chunks[0].PageStart.Should().Be(1);
            chunks[0].PageEnd.Should().Be(1);
            chunks[0].CharStart.Should().Be(0);
            chunks[0].CharEnd.Should().Be(500);
            chunks[1].ContentId.Should().Be("pid1");
            chunks[1].PageStart.Should().Be(1);
            chunks[1].PageEnd.Should().Be(2);
            chunks[1].CharStart.Should().Be(480);
            chunks[1].CharEnd.Should().Be(1200);
            chunks[0].Retired.Should().BeFalse();
            store.ReplaceDocumentPatches("doc.pdf", []);
            store.ListChunks("doc.pdf").Should().BeEmpty();
        }
        finally
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(db)!, true);
            }
            catch (IOException ex)
            {
                GC.KeepAlive(ex);
            }
        }
    }

    [Fact]
    public void CheckpointSql_UsesParametersAndFileName()
    {
        CheckpointSql.FileName.Should().Be("checkpoints.db");
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Recall.Core", "Checkpoints", "SqliteIngestCursorStore.cs"));
        source.Should().Contain("@path");
        source.Should().Contain("@hash");
        source.Should().Contain("@state");
        source.Should().Contain("@grant");
        source.Should().Contain("@id");
        source.Should().Contain("@ordinal");
        source.Should().Contain("@missing");
        source.Should().Contain("@retired");
    }

    private sealed class CountingReader : IGrantedTextReader
    {
        public int Calls { get; private set; }

        public List<GrantedTextDocument> Documents { get; } = [];

        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Calls++;
            foreach (var document in Documents)
            {
                yield return document;
            }

            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<string> ListAudioAndVideoAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }

        public async IAsyncEnumerable<string> ListImagesAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }
    }

    private static (ScopeGrantService Scope, CountingReader Reader, FakeTextEmbeddingEncoder Encoder, FakeTextVectorIndex Index, FakeIngestCursorStore Cursors, GrantedTextIngestService Service, string OplogDir) CreateIngest(string grantPath, GrantFileKinds kinds)
    {
        var store = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(store);
        scope.TryAddGrant(new ScopeFolderGrant(grantPath, "grant", kinds));
        var reader = new CountingReader();
        var encoder = new FakeTextEmbeddingEncoder();
        var index = new FakeTextVectorIndex();
        var oplogDir = Path.Combine(Path.GetTempPath(), "recall-oplog-" + Guid.NewGuid().ToString("N"));
        var (engine, opLog, cursors) = IngestTestKit.CreateEngineParts(encoder, index, oplogDir);
        var service = IngestTestKit.CreateGrantedService(scope, reader, engine, cursors, index, opLog);
        return (scope, reader, encoder, index, cursors, service, oplogDir);
    }

    [Fact]
    public async Task IngestGrant_UnknownPath_DoesNotRead()
    {
        var parts = CreateIngest(@"D:\Known", GrantFileKinds.All);
        try
        {
            await parts.Service.IngestGrantAsync(@"D:\Unknown", TestContext.Current.CancellationToken);
            parts.Reader.Calls.Should().Be(0);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task IngestGrant_TextAndCodeOff_DoesNotRead()
    {
        var parts = CreateIngest(@"D:\Known", GrantFileKinds.Image);
        try
        {
            await parts.Service.IngestGrantAsync(@"D:\Known", TestContext.Current.CancellationToken);
            parts.Reader.Calls.Should().Be(0);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task IngestDocument_FinishedSameHash_DoesNotEmbed()
    {
        var parts = CreateIngest(@"D:\Known", GrantFileKinds.All);
        try
        {
            var grant = new ScopeFolderGrant(@"D:\Known", "grant", GrantFileKinds.All);
            var document = new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", "hello", DateTimeOffset.UtcNow);
            await parts.Service.IngestDocumentAsync(grant, document, TestContext.Current.CancellationToken);
            var calls = parts.Encoder.Calls;
            await parts.Service.IngestDocumentAsync(grant, document, TestContext.Current.CancellationToken);
            parts.Encoder.Calls.Should().Be(calls);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task IngestDocument_Reading_Embeds()
    {
        var parts = CreateIngest(@"D:\Known", GrantFileKinds.All);
        try
        {
            var grant = new ScopeFolderGrant(@"D:\Known", "grant", GrantFileKinds.All);
            var hash = ContentIdBuilder.HashSourceContent("hello");
            parts.Cursors.MarkReading(@"D:\Known\a.txt", hash, @"D:\Known");
            var document = new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", "hello", DateTimeOffset.UtcNow);
            await parts.Service.IngestDocumentAsync(grant, document, TestContext.Current.CancellationToken);
            parts.Encoder.Calls.Should().BeGreaterThan(0);
            parts.Cursors.GetFile(@"D:\Known\a.txt")!.State.Should().Be(IngestCursorState.Finished);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task IngestDocument_ThrowAfterMarkReading_StaysReading()
    {
        var store = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(store);
        scope.TryAddGrant(new ScopeFolderGrant(@"D:\Known", "grant", GrantFileKinds.All));
        var reader = new CountingReader();
        var throwingEncoder = new ThrowingEncoder();
        var index = new FakeTextVectorIndex();
        var oplogDir = Path.Combine(Path.GetTempPath(), "recall-oplog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(oplogDir);
        try
        {
            var (engine, opLog, cursors) = IngestTestKit.CreateEngineParts(throwingEncoder, index, oplogDir);
            var service = IngestTestKit.CreateGrantedService(scope, reader, engine, cursors, index, opLog);
            var grant = new ScopeFolderGrant(@"D:\Known", "grant", GrantFileKinds.All);
            var document = new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", "hello", DateTimeOffset.UtcNow);
            Func<Task> act = () => service.IngestDocumentAsync(grant, document, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<InvalidOperationException>();
            cursors.GetFile(@"D:\Known\a.txt")!.State.Should().Be(IngestCursorState.Reading);
        }
        finally
        {
            Directory.Delete(oplogDir, true);
        }
    }

    [Fact]
    public async Task Resume_ReadingRow_EmbedsAgain_FinishedRow_DoesNotEmbed()
    {
        var store = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(store);
        scope.TryAddGrant(new ScopeFolderGrant(@"D:\Known", "grant", GrantFileKinds.All));
        var reader = new CountingReader();
        var failOnce = new FailOnceEncoder();
        var index = new FakeTextVectorIndex();
        var oplogDir = Path.Combine(Path.GetTempPath(), "recall-oplog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(oplogDir);
        try
        {
            var (engine, opLog, cursors) = IngestTestKit.CreateEngineParts(failOnce, index, oplogDir);
            var service = IngestTestKit.CreateGrantedService(scope, reader, engine, cursors, index, opLog);
            var grant = new ScopeFolderGrant(@"D:\Known", "grant", GrantFileKinds.All);
            var document = new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", "hello", DateTimeOffset.UtcNow);
            Func<Task> first = () => service.IngestDocumentAsync(grant, document, TestContext.Current.CancellationToken);
            await first.Should().ThrowAsync<InvalidOperationException>();
            await service.IngestDocumentAsync(grant, document, TestContext.Current.CancellationToken);
            failOnce.Calls.Should().Be(2);
            await service.IngestDocumentAsync(grant, document, TestContext.Current.CancellationToken);
            failOnce.Calls.Should().Be(2);
        }
        finally
        {
            Directory.Delete(oplogDir, true);
        }
    }

    private sealed class ThrowingEncoder : ITextEmbeddingEncoder
    {
        public ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("encoder down");
        }
    }

    private sealed class FailOnceEncoder : ITextEmbeddingEncoder
    {
        public int Calls { get; private set; }

        public bool FailNext { get; set; } = true;

        public ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (FailNext)
            {
                FailNext = false;
                throw new InvalidOperationException("first fails");
            }

            var vector = new float[768];
            vector[0] = prefixedText.Length;
            return ValueTask.FromResult<ReadOnlyMemory<float>>(vector);
        }
    }

    private static (FakeIngestCursorStore Cursors, LocalOpLogWriter OpLog, FakeGrantedTextIngestService Ingest, ScopeChangeApplier Applier) CreateApplier()
    {
        var cursors = new FakeIngestCursorStore();
        var opLog = new LocalOpLogWriter(null);
        var ingest = new FakeGrantedTextIngestService();
        var applier = new ScopeChangeApplier(cursors, ingest, opLog);
        return (cursors, opLog, ingest, applier);
    }

    [Fact]
    public async Task Apply_Deleted_AppendsDeleteAndDoesNotEmbed()
    {
        var parts = CreateApplier();
        parts.Cursors.MarkReading("a.txt", "h1", "g");
        parts.Cursors.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Deleted, "a.txt", null), new ScopeFolderGrant("g", "g", GrantFileKinds.All), null, TestContext.Current.CancellationToken);
        parts.OpLog.Entries.Should().Contain(e => e.Operation == Recall.Core.Vocabulary.OpLogOperation.Delete && e.ContentId == "id1");
        parts.Ingest.IngestDocumentCalls.Should().Be(0);
        parts.Cursors.GetFile("a.txt")!.State.Should().Be(IngestCursorState.Missing);
    }

    [Fact]
    public async Task Delete_OnePath_SharedId_DoesNotAppendDelete()
    {
        var parts = CreateApplier();
        parts.Cursors.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
        parts.Cursors.FinishPath("b.txt", "h2", "g", "enc", [new ChunkWrite("id1", 0)]);
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Deleted, "a.txt", null), new ScopeFolderGrant("g", "g", GrantFileKinds.All), null, TestContext.Current.CancellationToken);
        parts.OpLog.Entries.Should().NotContain(e => e.Operation == Recall.Core.Vocabulary.OpLogOperation.Delete);
    }

    [Fact]
    public async Task Delete_LastPath_AppendsDeleteOnce()
    {
        var parts = CreateApplier();
        parts.Cursors.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Deleted, "a.txt", null), new ScopeFolderGrant("g", "g", GrantFileKinds.All), null, TestContext.Current.CancellationToken);
        parts.OpLog.Entries.Should().ContainSingle(e => e.Operation == Recall.Core.Vocabulary.OpLogOperation.Delete && e.ContentId == "id1");
    }

    [Fact]
    public async Task Apply_DeletedWhenAlreadyMissing_DoesNotAppendAgain()
    {
        var parts = CreateApplier();
        parts.Cursors.MarkReading("a.txt", "h1", "g");
        parts.Cursors.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
        parts.Cursors.MarkMissing("a.txt");
        var before = parts.OpLog.Entries.Count;
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Deleted, "a.txt", null), new ScopeFolderGrant("g", "g", GrantFileKinds.All), null, TestContext.Current.CancellationToken);
        parts.OpLog.Entries.Should().HaveCount(before);
    }

    [Fact]
    public async Task Apply_DeletedWithNoChunks_MarksMissingOnly()
    {
        var parts = CreateApplier();
        parts.Cursors.MarkReading("a.txt", "h1", "g");
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Deleted, "a.txt", null), new ScopeFolderGrant("g", "g", GrantFileKinds.All), null, TestContext.Current.CancellationToken);
        parts.Cursors.GetFile("a.txt")!.State.Should().Be(IngestCursorState.Missing);
        parts.OpLog.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_RenamedSameHash_DoesNotEmbed()
    {
        var parts = CreateApplier();
        var text = "hello";
        var hash = ContentIdBuilder.HashSourceContent(text);
        parts.Cursors.MarkReading("old.txt", hash, "g");
        parts.Cursors.FinishPath("old.txt", hash, "g", "enc", [new ChunkWrite("id1", 0)]);
        var grant = new ScopeFolderGrant("g", "g", GrantFileKinds.All);
        var document = new GrantedTextDocument("new.txt", "new.txt", text, DateTimeOffset.UtcNow);
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Renamed, "new.txt", "old.txt"), grant, document, TestContext.Current.CancellationToken);
        parts.Ingest.IngestDocumentCalls.Should().Be(0);
        parts.Cursors.GetFile("new.txt").Should().NotBeNull();
    }

    [Fact]
    public async Task Apply_RenamedChangedHash_RetiresAndEmbedsOnce()
    {
        var parts = CreateApplier();
        var oldHash = ContentIdBuilder.HashSourceContent("old");
        parts.Cursors.MarkReading("old.txt", oldHash, "g");
        parts.Cursors.FinishPath("old.txt", oldHash, "g", "enc", [new ChunkWrite("id1", 0)]);
        var grant = new ScopeFolderGrant("g", "g", GrantFileKinds.All);
        var document = new GrantedTextDocument("new.txt", "new.txt", "new text", DateTimeOffset.UtcNow);
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Renamed, "new.txt", "old.txt"), grant, document, TestContext.Current.CancellationToken);
        parts.Cursors.ListChunks("old.txt")[0].Retired.Should().BeTrue();
        parts.Ingest.IngestDocumentCalls.Should().Be(1);
    }

    [Fact]
    public async Task Apply_RenamedUnknownPrevious_IngestsNewPath()
    {
        var parts = CreateApplier();
        var grant = new ScopeFolderGrant("g", "g", GrantFileKinds.All);
        var document = new GrantedTextDocument("new.txt", "new.txt", "hello", DateTimeOffset.UtcNow);
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Renamed, "new.txt", "missing.txt"), grant, document, TestContext.Current.CancellationToken);
        parts.Ingest.IngestDocumentCalls.Should().Be(1);
    }

    [Fact]
    public async Task Apply_RenamedMissingDocument_MarksPreviousMissing()
    {
        var parts = CreateApplier();
        parts.Cursors.MarkReading("old.txt", "h1", "g");
        parts.Cursors.FinishPath("old.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Renamed, "new.txt", "old.txt"), new ScopeFolderGrant("g", "g", GrantFileKinds.All), null, TestContext.Current.CancellationToken);
        parts.Cursors.GetFile("old.txt")!.State.Should().Be(IngestCursorState.Missing);
    }

    [Fact]
    public async Task Apply_CreatedNullDocument_DoesNotWrite()
    {
        var parts = CreateApplier();
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Created, "a.txt", null), new ScopeFolderGrant("g", "g", GrantFileKinds.All), null, TestContext.Current.CancellationToken);
        parts.Cursors.GetFile("a.txt").Should().BeNull();
        parts.Ingest.IngestDocumentCalls.Should().Be(0);
    }

    [Fact]
    public async Task Apply_BlankPath_DoesNotWrite()
    {
        var parts = CreateApplier();
        await parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Created, "   ", null), new ScopeFolderGrant("g", "g", GrantFileKinds.All), new GrantedTextDocument("a", "a", "x", DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);
        parts.Ingest.IngestDocumentCalls.Should().Be(0);
    }

    [Fact]
    public async Task Apply_Cancelled_Throws()
    {
        var parts = CreateApplier();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        Func<Task> act = () => parts.Applier.ApplyAsync(new ScopeChange(ScopeChangeKind.Created, "a.txt", null), null, null, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task TryRead_MissingBlockedIgnoredOrUnsafe_ReturnsNull()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-tryread-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);
            var skips = new FakeUserSkipStore();
            var missing = await FilesystemScopeFileReader.TryReadAsync(grant, Path.Combine(root, "missing.txt"), skips, TestContext.Current.CancellationToken);
            missing.Should().BeNull();
            var blockedDir = Path.Combine(root, "node_modules");
            Directory.CreateDirectory(blockedDir);
            var blockedFile = Path.Combine(blockedDir, "a.txt");
            await File.WriteAllTextAsync(blockedFile, "hello", TestContext.Current.CancellationToken);
            var blocked = await FilesystemScopeFileReader.TryReadAsync(grant, blockedFile, skips, TestContext.Current.CancellationToken);
            blocked.Should().BeNull();
            await File.WriteAllTextAsync(Path.Combine(root, ".gitignore"), "*.txt\n", TestContext.Current.CancellationToken);
            var ignoredFile = Path.Combine(root, "skip.txt");
            await File.WriteAllTextAsync(ignoredFile, "hello", TestContext.Current.CancellationToken);
            var ignored = await FilesystemScopeFileReader.TryReadAsync(grant, ignoredFile, skips, TestContext.Current.CancellationToken);
            ignored.Should().BeNull();
            File.Delete(Path.Combine(root, ".gitignore"));
            var exeFile = Path.Combine(root, "run.exe");
            await File.WriteAllBytesAsync(exeFile, [0x4D, 0x5A, 0x90, 0x00, 0x01], TestContext.Current.CancellationToken);
            var unsafeRead = await FilesystemScopeFileReader.TryReadAsync(grant, exeFile, skips, TestContext.Current.CancellationToken);
            unsafeRead.Should().BeNull();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task TryRead_AdmittedText_ReturnsDocument()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-tryread-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "notes.txt");
            await File.WriteAllTextAsync(file, "hello", TestContext.Current.CancellationToken);
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);
            var document = await FilesystemScopeFileReader.TryReadAsync(grant, file, new FakeUserSkipStore(), TestContext.Current.CancellationToken);
            document.Should().NotBeNull();
            document!.Text.Should().Be("hello");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static HashSet<string> GrantPathSet(params string[] paths) =>
        paths.ToHashSet(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Apply_RetiredChunk_DropsHit()
    {
        var hit = new TextSearchHit("id1", "old.txt", "old.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1.0, GrantPath: "g");
        var chunk = new IngestChunkRow("id1", "old.txt", 0, false, true);
        SearchHitMarks.Apply(hit, chunk, GrantPathSet("g")).Should().BeNull();
    }

    [Fact]
    public void Apply_MissingChunk_SetsSourceMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-hit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "a.txt");
            File.WriteAllText(file, "x");
            var hit = new TextSearchHit("id1", "stored.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1.0, GrantPath: root);
            var chunk = new IngestChunkRow("id1", file, 0, true, false);
            var applied = SearchHitMarks.Apply(hit, chunk, GrantPathSet(root));
            applied.Should().NotBeNull();
            applied!.SourceMissing.Should().BeTrue();
            applied.SourcePath.Should().Be(file);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Apply_ChunkPath_ReplacesStoredPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-hit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "new.txt");
            File.WriteAllText(file, "x");
            var hit = new TextSearchHit("id1", "old.txt", "old.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1.0, GrantPath: root);
            var chunk = new IngestChunkRow("id1", file, 0, false, false);
            SearchHitMarks.Apply(hit, chunk, GrantPathSet(root))!.SourcePath.Should().Be(file);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Apply_PathOutsideGrant_DropsHit()
    {
        var hit = new TextSearchHit("id1", "old.txt", "old.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1.0, GrantPath: "other");
        var chunk = new IngestChunkRow("id1", @"D:\Other\a.txt", 0, false, false);
        SearchHitMarks.Apply(hit, chunk, GrantPathSet(@"D:\Known")).Should().BeNull();
    }

    [Fact]
    public void Apply_NoLiveAlias_DropsHit()
    {
        var hit = new TextSearchHit("id1", "a.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1.0, GrantPath: "g");
        SearchHitMarks.Apply(hit, null, GrantPathSet("g")).Should().BeNull();
    }

    [Fact]
    public void Build_SourceMissing_IsUnavailableEvenWhenFileExists()
    {
        var hit = new TextSearchHit("id1", "a.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1.0, SourceMissing: true);
        var view = Recall.UI.Pages.TextFindSession.Build([hit], _ => true);
        view.Immediate.Should().HaveCount(1);
        view.Immediate[0].Unavailable.Should().BeTrue();
    }

    [Fact]
    public void Build_MissingFile_IsUnavailable()
    {
        var hit = new TextSearchHit("id1", "a.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1.0);
        var view = Recall.UI.Pages.TextFindSession.Build([hit], _ => false);
        view.Immediate[0].Unavailable.Should().BeTrue();
    }

    [Fact]
    public void Build_PresentFile_IsAvailable()
    {
        var hit = new TextSearchHit("id1", "a.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1.0);
        var view = Recall.UI.Pages.TextFindSession.Build([hit], _ => true);
        view.Immediate[0].Unavailable.Should().BeFalse();
    }

    [Fact]
    public void AppendDelete_Blank_Throws()
    {
        var writer = new LocalOpLogWriter(null);
        Action act = () => writer.AppendDelete("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AppendDelete_Memory_IsDeleteAndUnsent()
    {
        var writer = new LocalOpLogWriter(null);
        writer.AppendDelete("id1");
        writer.Entries.Should().ContainSingle().Which.Operation.Should().Be(Recall.Core.Vocabulary.OpLogOperation.Delete);
        writer.Entries[0].Sent.Should().BeFalse();
    }

    [Fact]
    public void AppendDelete_NoDirectory_WritesNoFile()
    {
        var writer = new LocalOpLogWriter("   ");
        writer.AppendDelete("id1");
        writer.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void AppendDelete_Directory_WritesDeleteLine()
    {
        var dir = Path.Combine(Path.GetTempPath(), "recall-oplog-" + Guid.NewGuid().ToString("N"));
        try
        {
            var writer = new LocalOpLogWriter(dir);
            writer.AppendDelete("id1");
            Directory.GetFiles(dir, "*.log").Should().HaveCount(1);
            File.ReadAllText(Directory.GetFiles(dir, "*.log")[0]).Should().Contain("id1").And.Contain("Delete");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Recall.slnx was not found above the test output directory.");
    }
}
