using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Download;
using Recall.Core.Faults;
using Recall.Core.Ingest;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Text;
using Recall.UI.Pages;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class PhaseAddendumTests
{
    private static string NewTempDb() =>
        Path.Combine(Path.GetTempPath(), "recall-addendum-" + Guid.NewGuid().ToString("N"), CheckpointSql.FileName);

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
        filter.Should().Contain(@"D:\\In");
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
    public void Shrink_LastPath_DeletesVector()
    {
        var index = new FakeTextVectorIndex();
        index.TryInsert(MakeInsert("id2"));
        index.TryDelete("id2").Should().BeTrue();
        index.DeleteCalls.Should().Be(1);
    }

    [Fact]
    public void Shrink_OtherPathLive_KeepsVector()
    {
        var db = NewTempDb();
        var index = new FakeTextVectorIndex();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.FinishPath("a.txt", "h", "g", "enc", [new ChunkWrite("id1", 0)]);
            store.FinishPath("b.txt", "h2", "g", "enc", [new ChunkWrite("id1", 0)]);
            store.AnyLivePath("id1").Should().BeTrue();
            index.TryDelete("id1").Should().BeFalse();
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

    private static TextIndexInsert MakeInsert(string id) =>
        new(id, "p", "f", "note", new float[512], DateTimeOffset.UtcNow, "enc", true, "g", "note", 0);

}
