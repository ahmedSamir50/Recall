using FluentAssertions;
using Recall.Adapters.Indexing;
using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Text;
using Recall.UnitTests.Fakes;
using ZVec.NET;

namespace Recall.UnitTests;

public sealed class CloseAddendumGapsTests
{
    private static string NewTempDb() =>
        Path.Combine(Path.GetTempPath(), "recall-close-" + Guid.NewGuid().ToString("N"), CheckpointSql.FileName);

    [Fact]
    public void TextSchema_FtsKey_UsesFtsIndexParam()
    {
        var schema = ZVecTextVectorIndex.CreateSchema();
        var fts = schema.Vectors.Single(v => v.Name == ZVecTextVectorIndex.FtsKeyField);
        fts.IndexParam.Should().BeOfType<ZVecFtsIndexParam>();
        var ftsParam = (ZVecFtsIndexParam)fts.IndexParam;
        ftsParam.Tokenizer.Should().Be(ZVecFtsTokenizer.Standard);
        ftsParam.Filters.Should().Equal(ZVecFtsTokenFilter.Lowercase);
        schema.Fields.Should().Contain(f => f.Name == ZVecTextVectorIndex.RawTextField && f.DataType == ZVecDataType.String);
        var embedding = schema.Vectors.Single(v => v.Name == ZVecTextVectorIndex.EmbeddingField);
        embedding.IndexParam.Should().BeOfType<ZVecHnswIndexParam>();
        ((ZVecHnswIndexParam)embedding.IndexParam).QuantizeType.Should().Be(ZVecQuantizeType.Undefined);
    }

    [Fact]
    public void NumericSpan_Null_Throws()
    {
        Action act = () => NumericSpanPolicy.IsDense(null!, false);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NumericSpan_Empty_IsNotDense()
    {
        NumericSpanPolicy.IsDense("", false).Should().BeFalse();
        NumericSpanPolicy.IsDense(" \t", false).Should().BeFalse();
    }

    [Fact]
    public void NumericSpan_LetterTimesFour_Boundary()
    {
        NumericSpanPolicy.IsDense("a1234", false).Should().BeTrue();
        NumericSpanPolicy.IsDense("a12345", false).Should().BeFalse();
    }

    [Fact]
    public void NumericSpan_SpacesIgnored()
    {
        NumericSpanPolicy.IsDense("a\t1234", false).Should().BeTrue();
    }

    [Fact]
    public async Task NumericSpan_DigitsOnly_Bm25Only()
    {
        var parts = CreateIngestParts();
        try
        {
            var grant = parts.Grant;
            await parts.Service.IngestDocumentAsync(
                grant,
                new GrantedTextDocument(@"D:\Known\n.txt", "n.txt", "12345", DateTimeOffset.UtcNow),
                CancellationToken.None);
            parts.Index.Inserts.Should().BeEmpty();
            parts.Cursors.ListBm25Only().Should().ContainSingle(row => row.RawText == "12345");
            var hits = await parts.Search.FindAsync("12345", [grant], CancellationToken.None);
            hits.Should().Contain(hit => hit.SourcePath == @"D:\Known\n.txt");
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public async Task NumericSpan_CodeFile_StaysDense()
    {
        var parts = CreateIngestParts();
        try
        {
            var grant = parts.Grant;
            await parts.Service.IngestDocumentAsync(
                grant,
                new GrantedTextDocument(@"D:\Known\n.cs", "n.cs", "12345", DateTimeOffset.UtcNow),
                CancellationToken.None);
            parts.Index.Inserts.Should().HaveCount(1);
            parts.Cursors.ListBm25Only().Should().BeEmpty();
            NumericSpanPolicy.IsDense("12345", true).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(parts.OplogDir, true);
        }
    }

    [Fact]
    public void NumericSpan_FinishThrows_LeavesNoBm25RowAndStaysReading()
    {
        var db = NewTempDb();
        try
        {
            var store = new SqliteIngestCursorStore(db);
            store.MarkReading("a.txt", "h", "g");
            var act = () => store.FinishPath(
                "a.txt",
                "h",
                "g",
                "enc",
                [new ChunkWrite("", 0)],
                [new Bm25OnlyRow("bm25", "a.txt", "12345")]);
            act.Should().Throw<ArgumentException>();
            var check = new SqliteIngestCursorStore(db);
            check.GetFile("a.txt")!.State.Should().Be(IngestCursorState.Reading);
            check.ListBm25Only().Should().BeEmpty();
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
    public void Count_DigitQuery_MatchesDigitSpan()
    {
        Bm25OnlyRank.Count("12345", "12345").Should().Be(1);
    }

    [Fact]
    public void Count_LetterQuery_MissesDigits()
    {
        Bm25OnlyRank.Count("12345", "hello").Should().Be(0);
    }

    [Fact]
    public void Count_TwoPieces()
    {
        Bm25OnlyRank.Count("12 34", "12 99").Should().Be(1);
    }

    [Fact]
    public void Count_Null_Throws()
    {
        Action raw = () => Bm25OnlyRank.Count(null!, "q");
        raw.Should().Throw<ArgumentNullException>();
        Action query = () => Bm25OnlyRank.Count("1", null!);
        query.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Merge_AppendsAfterFts_OrdersByOverlap()
    {
        var grants = new List<ScopeFolderGrant> { new(@"D:\Known", "k", GrantFileKinds.Text) };
        var fts = new[]
        {
            new TextSearchHit("f1", @"D:\Known\f1.txt", "f1.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1, GrantPath: @"D:\Known"),
            new TextSearchHit("f2", @"D:\Known\f2.txt", "f2.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1, GrantPath: @"D:\Known"),
        };
        var rows = new[]
        {
            new Bm25OnlyRow("high", @"D:\Known\high.txt", "alpha beta gamma"),
            new Bm25OnlyRow("mid", @"D:\Known\mid.txt", "alpha beta"),
            new Bm25OnlyRow("low", @"D:\Known\low.txt", "alpha"),
        };
        var merged = Bm25OnlyRank.Merge(fts, rows, "alpha beta gamma", grants);
        merged.Select(h => h.ContentId).Should().Equal("f1", "f2", "high", "mid", "low");
    }

    [Fact]
    public void Merge_Tie_KeepsLowerContentId()
    {
        var grants = new List<ScopeFolderGrant> { new(@"D:\Known", "k", GrantFileKinds.Text) };
        var fts = Array.Empty<TextSearchHit>();
        var rows = new[]
        {
            new Bm25OnlyRow("b", @"D:\Known\b.txt", "alpha beta"),
            new Bm25OnlyRow("a", @"D:\Known\a.txt", "alpha beta"),
        };
        var merged = Bm25OnlyRank.Merge(fts, rows, "alpha beta", grants);
        merged.Select(h => h.ContentId).Should().Equal("a", "b");
    }

    [Fact]
    public void Merge_ZeroOverlap_IsOmitted()
    {
        var grants = new List<ScopeFolderGrant> { new(@"D:\Known", "k", GrantFileKinds.Text) };
        var rows = new[] { new Bm25OnlyRow("x", @"D:\Known\x.txt", "12345") };
        var merged = Bm25OnlyRank.Merge([], rows, "alpha", grants);
        merged.Should().BeEmpty();
    }

    [Fact]
    public void Merge_CapsAt64()
    {
        var grants = new List<ScopeFolderGrant> { new(@"D:\Known", "k", GrantFileKinds.Text) };
        var fts = new[]
        {
            new TextSearchHit("f0", @"D:\Known\f0.txt", "f0.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1, GrantPath: @"D:\Known"),
            new TextSearchHit("f1", @"D:\Known\f1.txt", "f1.txt", DateTimeOffset.UtcNow, TextMatchReason.DenseAndFullText, 1, GrantPath: @"D:\Known"),
        };
        var rows = Enumerable.Range(0, 70)
            .Select(i => new Bm25OnlyRow($"b{i:00}", $@"D:\Known\b{i:00}.txt", "alpha"))
            .ToList();
        var merged = Bm25OnlyRank.Merge(fts, rows, "alpha", grants);
        merged.Should().HaveCount(64);
        merged[0].ContentId.Should().Be("f0");
        merged[1].ContentId.Should().Be("f1");
        merged[2].ContentId.Should().Be("b00");
        merged.Should().NotContain(h => h.ContentId == "b63");
    }

    [Fact]
    public void Merge_OutsideGrant_IsOmitted()
    {
        var grants = new List<ScopeFolderGrant> { new(@"D:\Known", "k", GrantFileKinds.Text) };
        var rows = new[] { new Bm25OnlyRow("x", @"D:\Other\x.txt", "alpha") };
        var merged = Bm25OnlyRank.Merge([], rows, "alpha", grants);
        merged.Should().BeEmpty();
    }

    [Fact]
    public void Merge_Null_Throws()
    {
        var grants = new List<ScopeFolderGrant> { new(@"D:\Known", "k", GrantFileKinds.Text) };
        Action act = () => Bm25OnlyRank.Merge(null!, [], "q", grants);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromFlags_Partial_IsRemote()
    {
        AndroidDocumentPresence.FromFlags(true, false, false, null, false, false).Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromFlags_Virtual_IsRemote()
    {
        AndroidDocumentPresence.FromFlags(false, true, false, null, false, false).Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromFlags_DownloadProgress_IsRemote()
    {
        AndroidDocumentPresence.FromFlags(false, false, false, null, true, false).Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromFlags_DownloadError_IsRemote()
    {
        AndroidDocumentPresence.FromFlags(false, false, false, null, false, true).Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromFlags_AvailableFalse_IsRemote()
    {
        AndroidDocumentPresence.FromFlags(false, false, false, false, false, false).Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromFlags_AvailableTrue_IsLocal()
    {
        AndroidDocumentPresence.FromFlags(false, false, false, true, false, false).Should().Be(LocalContentState.Local);
    }

    [Fact]
    public void FromFlags_Api36WithoutCloudFlags_IsLocal()
    {
        AndroidDocumentPresence.FromFlags(false, false, true, null, false, false).Should().Be(LocalContentState.Local);
    }

    [Fact]
    public void FromFlags_NotApi36WithoutCloudFlags_IsLocal()
    {
        AndroidDocumentPresence.FromFlags(false, false, false, null, false, false).Should().Be(LocalContentState.Local);
    }

    [Fact]
    public void MayReadBytes_Remote_IsFalse()
    {
        LocalContentPolicy.MayReadBytes(LocalContentState.Remote).Should().BeFalse();
    }

    [Fact]
    public void MayReadBytes_Unknown_IsFalse()
    {
        LocalContentPolicy.MayReadBytes(LocalContentState.Unknown).Should().BeFalse();
    }

    [Fact]
    public void MayReadBytes_Local_IsTrue()
    {
        LocalContentPolicy.MayReadBytes(LocalContentState.Local).Should().BeTrue();
    }

    [Fact]
    public void FromFindData_NotFound_IsUnknown()
    {
        WindowsCloudAttributes.FromFindData(false, 0).Should().Be(LocalContentState.Unknown);
    }

    [Fact]
    public void FromFindData_Offline_IsRemote()
    {
        WindowsCloudAttributes.FromFindData(true, WindowsCloudAttributes.Offline).Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromFindData_RecallOnOpen_IsRemote()
    {
        WindowsCloudAttributes.FromFindData(true, WindowsCloudAttributes.RecallOnOpen).Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromFindData_RecallOnDataAccess_IsRemote()
    {
        WindowsCloudAttributes.FromFindData(true, WindowsCloudAttributes.RecallOnDataAccess).Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromFindData_PinnedOnly_IsLocal()
    {
        WindowsCloudAttributes.FromFindData(true, WindowsCloudAttributes.Pinned).Should().Be(LocalContentState.Local);
    }

    [Fact]
    public void FromFindData_UnpinnedOnly_IsLocal()
    {
        WindowsCloudAttributes.FromFindData(true, WindowsCloudAttributes.Unpinned).Should().Be(LocalContentState.Local);
    }

    [Fact]
    public void FromFindData_NoBits_IsLocal()
    {
        WindowsCloudAttributes.FromFindData(true, 0).Should().Be(LocalContentState.Local);
    }

    [Fact]
    public void IsStubName_DotIcloud_IsTrue()
    {
        AppleICloudPresence.IsStubName(".file.icloud").Should().BeTrue();
        AppleICloudPresence.IsStubName("file.icloud").Should().BeFalse();
    }

    [Fact]
    public void FromResource_Stub_IsRemote()
    {
        AppleICloudPresence.FromResource(true, null, null).Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromResource_UbiquitousNull_IsUnknown()
    {
        AppleICloudPresence.FromResource(false, null, null).Should().Be(LocalContentState.Unknown);
    }

    [Fact]
    public void FromResource_NotUbiquitous_IsLocal()
    {
        AppleICloudPresence.FromResource(false, false, null).Should().Be(LocalContentState.Local);
    }

    [Fact]
    public void FromResource_Downloaded_IsLocal()
    {
        AppleICloudPresence.FromResource(false, true, "Downloaded").Should().Be(LocalContentState.Local);
    }

    [Fact]
    public void FromResource_Current_IsRemote()
    {
        AppleICloudPresence.FromResource(false, true, "Current").Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromResource_NotDownloaded_IsRemote()
    {
        AppleICloudPresence.FromResource(false, true, "NotDownloaded").Should().Be(LocalContentState.Remote);
    }

    [Fact]
    public void FromResource_OtherStatus_IsUnknown()
    {
        AppleICloudPresence.FromResource(false, true, "other").Should().Be(LocalContentState.Unknown);
    }

    [Fact]
    public async Task CloudFile_NotLocal_NotIndexed()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-cloud-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "local.txt");
        await File.WriteAllTextAsync(file, "hello", TestContext.Current.CancellationToken);
        var grant = new ScopeFolderGrant(root, "g", GrantFileKinds.Text);
        var skips = new FakeUserSkipStore();
        var remote = new RemoteFileSkipReport();

        (await FilesystemScopeFileReader.TryReadAsync(grant, file, skips, TestContext.Current.CancellationToken, new FakeLocalContentProbe { Next = LocalContentState.Remote }, remote))
            .Should().BeNull();
        remote.SkippedCount.Should().Be(1);
        File.Exists(file).Should().BeTrue();

        (await FilesystemScopeFileReader.TryReadAsync(grant, file, skips, TestContext.Current.CancellationToken, new FakeLocalContentProbe { Next = LocalContentState.Unknown }, remote))
            .Should().BeNull();

        (await FilesystemScopeFileReader.TryReadAsync(grant, file, skips, TestContext.Current.CancellationToken, new FakeLocalContentProbe { Next = LocalContentState.Local }, remote))
            .Should().NotBeNull();

        Directory.Delete(root, true);
    }

    [Fact]
    public async Task CloudFile_RemoteSibling_IsNotRead()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-cloud-sib-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "a.txt"), "a", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "b.txt"), "b", TestContext.Current.CancellationToken);
        var grant = new ScopeFolderGrant(root, "g", GrantFileKinds.Text);
        var probe = new PathStateProbe();
        var remote = new RemoteFileSkipReport();
        var reader = new DirectoryGrantedTextReader(new FakeUserSkipStore(), probe, remote, new DatasetSkipReport());
        var names = new List<string>();
        await foreach (var doc in reader.ReadAsync(grant, CancellationToken.None))
        {
            names.Add(doc.FileName);
        }

        names.Should().Equal("a.txt");
        remote.Paths.Should().Contain(p => p.EndsWith("b.txt", StringComparison.OrdinalIgnoreCase));
        Directory.Delete(root, true);
    }

    [Fact]
    public void CloudProbe_AndroidReader_ChecksFlagsBeforeOpen()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Recall.Maui", "Platform", "Folders", "MauiGrantedTextReader.Android.cs");
        var text = File.ReadAllText(path);
        text.Should().Contain("AndroidDocumentPresence.FromFlags");
        text.Should().Contain("LocalContentPolicy.MayReadBytes");
    }

    [Fact]
    public void CloudProbe_AppleProbe_DoesNotStartDownload()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Recall.Maui", "Platform", "Files", "MauiLocalContentProbe.Apple.cs");
        var text = File.ReadAllText(path);
        text.Should().Contain("AppleICloudPresence.FromResource");
        text.Should().NotContain("StartDownloading");
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

        throw new InvalidOperationException("Recall.slnx was not found.");
    }

    private static (ScopeFolderGrant Grant, GrantedTextIngestService Service, FakeTextVectorIndex Index, FakeIngestCursorStore Cursors, TextSearchService Search, string OplogDir) CreateIngestParts()
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
        var service = IngestTestKit.CreateGrantedService(scope, reader, engine, cursors, index, opLog);
        var search = IngestTestKit.CreateSearchService(encoder, index, cursors);
        return (grant, service, index, cursors, search, oplogDir);
    }

    private sealed class EmptyGrantedTextReader : IGrantedTextReader
    {
        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
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

    private sealed class PathStateProbe : ILocalContentProbe
    {
        public LocalContentState Probe(string path) =>
            path.EndsWith("b.txt", StringComparison.OrdinalIgnoreCase) ? LocalContentState.Remote : LocalContentState.Local;
    }
}
