using FluentAssertions;
using Microsoft.Data.Sqlite;
using Recall.Core.Checkpoints;
using Recall.Core.Download;
using Recall.Core.Faults;
using Recall.Core.Ingest;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.Core.Vectors;
using Recall.Core.Vocabulary;
using Recall.UI.Pages;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class PhaseAddendumTests
{
    private static string NewTempDb() =>
        Path.Combine(Path.GetTempPath(), "recall-addendum-" + Guid.NewGuid().ToString("N"), CheckpointSql.FileName);

    [Fact]
    public void Fold_Yeh_StaysYeh()
    {
        KeywordFold.Fold("\u064A").Should().Be("\u064A");
    }

    [Fact]
    public void Fold_Null_Throws()
    {
        Action act = () => KeywordFold.Fold(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Tokens_LowercasesNonHan()
    {
        KeywordFold.Tokens(KeywordFold.Fold("Ab")).Should().Equal("ab");
    }

    [Fact]
    public void Fold_AlefAndTatweel_Match()
    {
        var a = KeywordFold.Fold("\u0623\u0644\u0640\u0641");
        var b = KeywordFold.Fold("\u0627\u0644\u0641");
        a.Should().Be(b);
    }

    [Fact]
    public void Fold_HanCharacter_IsOneToken()
    {
        KeywordFold.Tokens(KeywordFold.Fold("你好")).Should().Equal("你", "好");
    }

    [Fact]
    public void Card_ShowsRawNote()
    {
        var hit = new TextSearchHit(
            "id",
            "a.txt",
            "a.txt",
            DateTimeOffset.UtcNow,
            TextMatchReason.DenseAndFullText,
            1,
            RawNote: "raw card text");
        var card = TextFindSession.Build([hit], _ => true).Immediate[0];
        card.RawNote.Should().Be("raw card text");
    }

    [Fact]
    public void NumericSpan_DigitsOnly_Bm25Only()
    {
        NumericSpanPolicy.IsDense("12345", codeFile: false).Should().BeFalse();
    }

    [Fact]
    public void NumericSpan_CodeFile_StaysDense()
    {
        NumericSpanPolicy.IsDense("12345", codeFile: true).Should().BeTrue();
    }

    [Fact]
    public void DatasetFolder_32Hex_Skips()
    {
        var names = Enumerable.Range(0, 300).Select(i => $"{i:x32}.jpg").ToList();
        DatasetFolderSkip.ShouldSkip(@"D:\data\gen", isGrantRoot: false, names).Should().BeTrue();
    }

    [Fact]
    public void DatasetFolder_GrantRoot_NotSkipped()
    {
        var names = Enumerable.Range(0, 300).Select(i => $"{i:x32}.jpg").ToList();
        DatasetFolderSkip.ShouldSkip(@"D:\data\gen", isGrantRoot: true, names).Should().BeFalse();
    }

    [Fact]
    public void DatasetFolder_Below300_NotSkipped()
    {
        var names = Enumerable.Range(0, 299).Select(i => $"{i:x32}.jpg").ToList();
        DatasetFolderSkip.ShouldSkip(@"D:\data\gen", isGrantRoot: false, names).Should().BeFalse();
    }

    [Fact]
    public void DatasetFolder_Uuid_IsMachineMade()
    {
        DatasetFolderSkip.IsMachineMade("00112233-4455-6677-8899-aabbccddeeff").Should().BeTrue();
    }

    [Fact]
    public void DatasetFolder_TwelveDigits_IsMachineMade()
    {
        DatasetFolderSkip.IsMachineMade("123456789012").Should().BeTrue();
    }

    [Fact]
    public void DatasetFolder_Cafe1234_IsNotMachineMade()
    {
        DatasetFolderSkip.IsMachineMade("cafe1234").Should().BeFalse();
        DatasetFolderSkip.IsMachineMade("1").Should().BeFalse();
    }

    [Fact]
    public void DatasetFolder_NumberedScans_NotSkipped()
    {
        var names = Enumerable.Range(1, 300).Select(i => $"{i}.jpg").ToList();
        DatasetFolderSkip.ShouldSkip(@"D:\scans", isGrantRoot: false, names).Should().BeFalse();
    }

    [Fact]
    public void Idle_RunsIngest_BusyDefers()
    {
        IdleIngestGate.ShouldRun(IdleSignal.Idle).Should().BeTrue();
        IdleIngestGate.ShouldRun(IdleSignal.Busy).Should().BeFalse();
    }

    [Fact]
    public void TwentyChunks_OneFile_ReturnsOneCard()
    {
        var hits = Enumerable.Range(0, 20)
            .Select(i => new TextSearchHit("id", @"D:\a.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, i * 0.01, i))
            .ToList();
        var collapsed = hits.GroupBy(h => h.SourcePath, StringComparer.OrdinalIgnoreCase).Select(g => Collapse.MaxRrf(g.ToList())).ToList();
        collapsed.Should().HaveCount(1);
    }

    [Fact]
    public void TwentyChunks_OneFile_KeepsMaxRrfScore()
    {
        var hits = new[]
        {
            new TextSearchHit("id", @"D:\a.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 0.2, 0),
            new TextSearchHit("id", @"D:\a.txt", "a.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 0.9, 1),
        };
        Collapse.MaxRrf(hits).Score.Should().Be(0.9);
    }

    [Fact]
    public void Search_GrantFilter_DoesNotPromiseTwenty()
    {
        var filter = TextSearchFilter.Build(new HashSet<string> { @"D:\In" }, "hash");
        filter.Should().Contain("grant_path");
        filter.Should().Contain(@"D:\In");
    }

    [Fact]
    public void ContentUri_EnumeratedDocument_IsInsideGrant()
    {
        var grants = new List<ScopeFolderGrant> { new("content://photos", "photos", GrantFileKinds.Text) };
        var enumerated = new HashSet<string>(StringComparer.Ordinal) { "content://photos/doc/1" };
        GrantMembership.GrantPathFor("content://photos/doc/1", grants, enumerated).Should().Be("content://photos");
    }

    [Fact]
    public void ContentUri_NotEnumerated_IsOutside()
    {
        var grants = new List<ScopeFolderGrant> { new("content://photos", "photos", GrantFileKinds.Text) };
        var enumerated = new HashSet<string>(StringComparer.Ordinal);
        GrantMembership.GrantPathFor("content://photos/doc/1", grants, enumerated).Should().BeNull();
    }

    [Fact]
    public void ContentUri_PhotosExtra_IsOutside()
    {
        var grants = new List<ScopeFolderGrant> { new("content://photos", "photos", GrantFileKinds.Text) };
        var enumerated = new HashSet<string>(StringComparer.Ordinal) { "content://photosExtra/doc/1" };
        GrantMembership.GrantPathFor("content://photosExtra", grants, enumerated).Should().BeNull();
    }

    [Fact]
    public void GrantPathFor_NullDocument_Throws()
    {
        var grants = new List<ScopeFolderGrant> { new("content://photos", "photos", GrantFileKinds.Text) };
        var enumerated = new HashSet<string>(StringComparer.Ordinal);
        Action act = () => GrantMembership.GrantPathFor(null!, grants, enumerated);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DeleteOnePath_SharedId_OtherPathStaysHit()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.MarkReading("a.txt", "h1", "g");
            store.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
            store.MarkReading("b.txt", "h2", "g");
            store.FinishPath("b.txt", "h2", "g", "enc", [new ChunkWrite("id1", 0)]);
            store.MarkMissing("a.txt");
            store.AnyLivePath("id1").Should().BeTrue();
            store.ListLiveAliases("id1").Should().ContainSingle(a => a.Path == "b.txt");
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
    public async Task Shrink_LastPath_DeletesVector()
    {
        var parts = CreateIngestParts();
        try
        {
            var grant = parts.Grant;
            var longDoc = new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", TwoChunkText, DateTimeOffset.UtcNow);
            await parts.Service.IngestDocumentAsync(grant, longDoc, CancellationToken.None);
            if (parts.Index.Inserts.Count > 1)
            {
                parts.Index.Inserts.RemoveAt(1);
            }

            parts.Index.Inserts.Should().HaveCount(1);
            var shortDoc = longDoc with { Text = "short" };
            await parts.Service.IngestDocumentAsync(grant, shortDoc, CancellationToken.None);
            parts.Index.DeleteCalls.Should().Be(1);
            parts.OpLog.Entries.Should().ContainSingle(e => e.Operation == OpLogOperation.Delete);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task Shrink_OtherPathLive_KeepsVector()
    {
        var parts = CreateIngestParts();
        try
        {
            var grant = parts.Grant;
            var longDoc = new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", TwoChunkText, DateTimeOffset.UtcNow);
            var otherPath = new GrantedTextDocument(@"D:\Known\b.txt", "b.txt", TwoChunkText, DateTimeOffset.UtcNow);
            await parts.Service.IngestDocumentAsync(grant, otherPath, CancellationToken.None);
            var deletesBefore = parts.Index.DeleteCalls;
            await parts.Service.IngestDocumentAsync(grant, longDoc with { Text = "short" }, CancellationToken.None);
            parts.Index.DeleteCalls.Should().Be(deletesBefore);
            parts.OpLog.Entries.Should().NotContain(e => e.Operation == OpLogOperation.Delete);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task SecondHash_DeletesOldRow_ReembedsSameId()
    {
        var encoderHash = new FakeEncoderFileHash { TextHash = "h1" };
        var parts = CreateIngestParts(encoderHash);
        try
        {
            var grant = parts.Grant;
            var document = new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", "hello", DateTimeOffset.UtcNow);
            await parts.Service.IngestDocumentAsync(grant, document, CancellationToken.None);
            var firstId = parts.Index.Inserts[0].ContentId;
            encoderHash.TextHash = "h2";
            await parts.Service.IngestDocumentAsync(grant, document, CancellationToken.None);
            parts.Index.DeleteCalls.Should().Be(1);
            parts.Index.Inserts.Should().ContainSingle(i => i.ContentId == firstId);
            parts.Index.Inserts[0].EncoderHash.Should().Be("h2");
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task Repair_KillAfterDelete_ResumesFromReading()
    {
        var encoderHash = new FakeEncoderFileHash { TextHash = "h1" };
        var parts = CreateIngestParts(encoderHash);
        try
        {
            var grant = parts.Grant;
            var document = new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", "hello", DateTimeOffset.UtcNow);
            await parts.Service.IngestDocumentAsync(grant, document, CancellationToken.None);
            encoderHash.TextHash = "h2";
            var hash = ContentIdBuilder.HashSourceContent(document.Text);
            parts.Cursors.MarkReading(document.Path, hash, grant.Path);
            parts.Cursors.RetirePath(document.Path);
            foreach (var chunk in parts.Cursors.ListChunks(document.Path))
            {
                parts.Index.TryDelete(chunk.ContentId);
            }

            parts.Cursors.GetFile(document.Path)!.State.Should().Be(IngestCursorState.Reading);
            await parts.Service.IngestDocumentAsync(grant, document, CancellationToken.None);
            parts.Cursors.GetFile(document.Path)!.State.Should().Be(IngestCursorState.Finished);
            parts.Index.Inserts.Should().ContainSingle(i => i.ContentId == parts.Cursors.ListChunks(document.Path)[0].ContentId);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task Repair_OtherPathLive_DoesNotDelete()
    {
        var encoderHash = new FakeEncoderFileHash { TextHash = "h1" };
        var parts = CreateIngestParts(encoderHash);
        try
        {
            var grant = parts.Grant;
            var text = "shared bytes";
            await parts.Service.IngestDocumentAsync(grant, new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", text, DateTimeOffset.UtcNow), CancellationToken.None);
            await parts.Service.IngestDocumentAsync(grant, new GrantedTextDocument(@"D:\Known\b.txt", "b.txt", text, DateTimeOffset.UtcNow), CancellationToken.None);
            var deletesBefore = parts.Index.DeleteCalls;
            var pendingBefore = parts.Cursors.PendingCount();
            encoderHash.TextHash = "h2";
            await parts.Service.IngestDocumentAsync(grant, new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", text, DateTimeOffset.UtcNow), CancellationToken.None);
            parts.Index.DeleteCalls.Should().Be(deletesBefore);
            parts.Cursors.PendingCount().Should().BeGreaterThan(pendingBefore);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task SecondPath_SameBytes_DoesNotAppendInsert()
    {
        var parts = CreateIngestParts();
        try
        {
            var grant = parts.Grant;
            var text = "same bytes";
            await parts.Service.IngestDocumentAsync(grant, new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", text, DateTimeOffset.UtcNow), CancellationToken.None);
            await parts.Service.IngestDocumentAsync(grant, new GrantedTextDocument(@"D:\Known\b.txt", "b.txt", text, DateTimeOffset.UtcNow), CancellationToken.None);
            parts.Index.Inserts.Should().HaveCount(1);
            parts.OpLog.Entries.Should().ContainSingle(e => e.Operation == OpLogOperation.Insert);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public void Open_CopiesChunkPairIntoAlias()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.FinishPath("a.txt", "h1", "g", "enc", [new ChunkWrite("id1", 0)]);
            using (var connection = new SqliteConnection($"Data Source={db}"))
            {
                connection.Open();
                using var delete = connection.CreateCommand();
                delete.CommandText = "DELETE FROM ingest_chunk_path";
                delete.ExecuteNonQuery();
            }

            var reopened = new SqliteIngestCursorStore(db);
            reopened.AnyLivePath("id1").Should().BeTrue();
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
    public async Task Find_PendingOver64_ReconcilesSixtyFour()
    {
        var cursors = new FakeIngestCursorStore { AllowWrite = true };
        for (var i = 0; i < 65; i++)
        {
            cursors.EnqueuePending($"pending-{i:D3}");
        }

        var index = new FakeTextVectorIndex();
        var service = IngestTestKit.CreateSearchService(new FakeTextEmbeddingEncoder(), index, cursors);
        await service.FindAsync("hello", [new ScopeFolderGrant("g", "g", GrantFileKinds.Text)], CancellationToken.None);
        index.ReconcileCalls.Should().Be(1);
        index.LastReconcileStateCount.Should().Be(64);
        cursors.PendingCount().Should().Be(1);
    }

    [Fact]
    public async Task Find_NoPending_DoesNotReconcile()
    {
        var cursors = new FakeIngestCursorStore();
        var index = new FakeTextVectorIndex();
        var service = IngestTestKit.CreateSearchService(new FakeTextEmbeddingEncoder(), index, cursors);
        await service.FindAsync("hello", [new ScopeFolderGrant("g", "g", GrantFileKinds.Text)], CancellationToken.None);
        index.ReconcileCalls.Should().Be(0);
    }

    [Fact]
    public async Task Find_TwentyChunks_ReturnsOneCard()
    {
        var index = new TwentyHitVectorIndex();
        var cursors = new FakeIngestCursorStore();
        cursors.AddAlias("id", @"D:\In\a.txt");
        var service = IngestTestKit.CreateSearchService(new FakeTextEmbeddingEncoder(), index, cursors);
        var hits = await service.FindAsync("hello", [new ScopeFolderGrant(@"D:\In", @"D:\In", GrantFileKinds.Text)], CancellationToken.None);
        hits.Should().HaveCount(1);
        hits[0].Score.Should().Be(0.9);
    }

    [Fact]
    public async Task ChangedBytes_DeletesOldZvecRow()
    {
        var parts = CreateIngestParts();
        try
        {
            var grant = parts.Grant;
            var path = @"D:\Known\a.txt";
            await parts.Service.IngestDocumentAsync(grant, new GrantedTextDocument(path, "a.txt", "old text", DateTimeOffset.UtcNow), CancellationToken.None);
            var oldId = parts.Index.Inserts[0].ContentId;
            await parts.Service.IngestDocumentAsync(grant, new GrantedTextDocument(path, "a.txt", "new text", DateTimeOffset.UtcNow), CancellationToken.None);
            parts.Index.DeleteCalls.Should().Be(1);
            parts.Index.Inserts.Should().NotContain(i => i.ContentId == oldId);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task ChangedBytes_SharedId_KeepsVector()
    {
        var parts = CreateIngestParts();
        try
        {
            var grant = parts.Grant;
            var text = "shared content";
            await parts.Service.IngestDocumentAsync(grant, new GrantedTextDocument(@"D:\Known\b.txt", "b.txt", text, DateTimeOffset.UtcNow), CancellationToken.None);
            var sharedId = parts.Index.Inserts[0].ContentId;
            var deletesBefore = parts.Index.DeleteCalls;
            await parts.Service.IngestDocumentAsync(grant, new GrantedTextDocument(@"D:\Known\a.txt", "a.txt", "different", DateTimeOffset.UtcNow), CancellationToken.None);
            parts.Index.DeleteCalls.Should().Be(deletesBefore);
            parts.Index.Inserts.Should().Contain(i => i.ContentId == sharedId);
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public void ShortRow_Edit_StaysShortUnlessPinned()
    {
        WidthOnEdit.Choose(Modality.Text, StoredWidth.W512, pinned: true, pastCutoff: true).Should().Be(StoredWidth.W512);
        WidthOnEdit.Choose(Modality.Image, StoredWidth.W512, pinned: false, pastCutoff: false).Should().Be(StoredWidth.W512);
        WidthOnEdit.Choose(Modality.Image, StoredWidth.W512, pinned: true, pastCutoff: false).Should().Be(StoredWidth.W768);
        WidthOnEdit.Choose(Modality.Image, StoredWidth.W768, pinned: false, pastCutoff: false).Should().Be(StoredWidth.W768);
        WidthOnEdit.Choose(Modality.Image, StoredWidth.W768, pinned: false, pastCutoff: true).Should().Be(StoredWidth.W512);
    }

    [Fact]
    public async Task Find_WriteBusy_SkipsReconcile()
    {
        var cursors = new FakeIngestCursorStore { AllowWrite = false };
        cursors.EnqueuePending("id1");
        var index = new FakeTextVectorIndex();
        var service = IngestTestKit.CreateSearchService(new FakeTextEmbeddingEncoder(), index, cursors);
        await service.FindAsync("hello", [new ScopeFolderGrant("g", "g", GrantFileKinds.Text)], CancellationToken.None);
        index.ReconcileCalls.Should().Be(0);
        cursors.PendingCount().Should().Be(1);
    }

    [Fact]
    public async Task TextModelReadyIngest_ReadGrantsAsync_CallsIngest()
    {
        var ingest = new FakeGrantedTextIngestService();
        await TextModelReadyIngest.ReadGrantsAsync(TextModelDownloadPhase.Ready, ingest, CancellationToken.None);
        ingest.IngestGrantedTextCalls.Should().Be(1);
        await TextModelReadyIngest.ReadGrantsAsync(TextModelDownloadPhase.Downloading, ingest, CancellationToken.None);
        ingest.IngestGrantedTextCalls.Should().Be(1);
    }

    [Fact]
    public void CloudFile_NotLocal_NotIndexed()
    {
        var probe = new FakeLocalContentProbe { Next = LocalContentState.Remote };
        LocalContentPolicy.MayReadBytes(probe.Probe("file.txt")).Should().BeFalse();
    }

    [Fact]
    public void VectorSchemaStale_Fault_IsTwenty()
    {
        ((int)RecallFault.VectorSchemaStale).Should().Be(20);
    }

    private static string TwoChunkText => new string('a', 40000);

    private static (ScopeFolderGrant Grant, GrantedTextIngestService Service, FakeTextVectorIndex Index, FakeIngestCursorStore Cursors, LocalOpLogWriter OpLog, string OplogDir) CreateIngestParts(FakeEncoderFileHash? encoderHash = null)
    {
        var store = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(store);
        scope.TryAddGrant(new ScopeFolderGrant(@"D:\Known", "grant", GrantFileKinds.All));
        var grant = scope.ListGrants()[0];
        var reader = new EmptyGrantedTextReader();
        var encoder = new FakeTextEmbeddingEncoder();
        var index = new FakeTextVectorIndex();
        var oplogDir = Path.Combine(Path.GetTempPath(), "recall-oplog-" + Guid.NewGuid().ToString("N"));
        var (engine, opLog, cursors) = IngestTestKit.CreateEngineParts(encoder, index, oplogDir);
        var service = IngestTestKit.CreateGrantedService(scope, reader, engine, cursors, index, opLog, encoderHash);
        return (grant, service, index, cursors, opLog, oplogDir);
    }

    private sealed class EmptyGrantedTextReader : IGrantedTextReader
    {
        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class TwentyHitVectorIndex : ITextVectorIndex
    {
        public int ReconcileCalls { get; private set; }

        public bool TryInsert(TextIndexInsert insert) => true;

        public bool TryDelete(string contentId) => false;

        public void Reconcile(IReadOnlyList<VectorLiveState> states) => ReconcileCalls++;

        public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default)
        {
            return Enumerable.Range(0, 20)
                .Select(i => new TextSearchHit(
                    "id",
                    @"D:\In\a.txt",
                    "a.txt",
                    DateTimeOffset.UtcNow,
                    TextMatchReason.DenseAndFullText,
                    i == 19 ? 0.9 : i * 0.01,
                    i,
                    GrantPath: @"D:\In"))
                .ToList();
        }
    }
}
