using FluentAssertions;
using Recall.Adapters.Indexing;
using Recall.Core.Checkpoints;
using Recall.Core.Indexing;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Text;
using Recall.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ZVec.NET;

namespace Recall.IntegrationTests;

public sealed class TextSearchIntegrationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "recall_integration_" + Guid.NewGuid().ToString("N"));

    private static string ModelDir => IntegrationTextModelPaths.ResolveModelDirectory();

    private ServiceProvider BuildProvider()
    {
        Directory.CreateDirectory(root);
        return IntegrationTestServiceCollection.Build(ModelDir, Path.Combine(root, "zvec"));
    }

    [Fact]
    public void QuantizeTypeUndefined_ProducesFloatIndex()
    {
        var databaseRoot = Path.Combine(root, "zvec_quant");
        using var index = new ZVecTextVectorIndex(databaseRoot);
        var vector = new float[512];
        vector[0] = 1f;
        var inserted = index.TryInsert(SampleInsert(
            ContentIdBuilder.Build(
                Recall.Core.Vocabulary.Modality.Text,
                ContentIdBuilder.HashSourceContent("pinned note"),
                Recall.Core.Vocabulary.StoredWidth.W512,
                0),
            Path.Combine(root, "note.txt"),
            "pinned note about mars rovers",
            vector,
            Path.Combine(root, "note.txt")));
        inserted.Should().BeTrue();
        Directory.GetDirectories(databaseRoot).Select(Path.GetFileName).Should().Equal("items_text");
    }

    [Fact]
    public void Search_DropsPathOutsideGrant()
    {
        var databaseRoot = Path.Combine(root, "zvec_grant");
        using var index = new ZVecTextVectorIndex(databaseRoot);
        var outsideDir = Path.Combine(root, "outside");
        var insideDir = Path.Combine(root, "inside");
        Directory.CreateDirectory(outsideDir);
        Directory.CreateDirectory(insideDir);
        var outsidePath = Path.Combine(outsideDir, "note.txt");
        var insidePath = Path.Combine(insideDir, "note.txt");
        var outsideVector = new float[512];
        outsideVector[0] = 1f;
        var insideVector = new float[512];
        insideVector[0] = 1f;
        index.TryInsert(SampleInsert(
            ContentIdBuilder.Build(
                Recall.Core.Vocabulary.Modality.Text,
                ContentIdBuilder.HashSourceContent("outside note"),
                Recall.Core.Vocabulary.StoredWidth.W512,
                0),
            outsidePath,
            "outside note about mars",
            outsideVector,
            outsideDir));
        index.TryInsert(SampleInsert(
            ContentIdBuilder.Build(
                Recall.Core.Vocabulary.Modality.Text,
                ContentIdBuilder.HashSourceContent("inside note"),
                Recall.Core.Vocabulary.StoredWidth.W512,
                0),
            insidePath,
            "inside note about mars",
            insideVector,
            insideDir));
        var query = new float[512];
        query[0] = 1f;
        var grants = new HashSet<string>([insideDir], StringComparer.OrdinalIgnoreCase);
        const string hash = "integration-test-hash";
        var filter = TextSearchFilter.Build(grants, hash);
        var hits = index.Search(new TextSearchRequest(
            "mars",
            query,
            grants,
            hash,
            filter,
            true,
            20), TestContext.Current.CancellationToken);
        hits.Should().HaveCount(1);
        hits[0].SourcePath.Should().Contain("inside");
        hits.Should().OnlyContain(h => !h.SourcePath.Contains("outside", StringComparison.Ordinal));
    }

    [Fact]
    public void RrfRankConstant_Is60()
    {
        new ZVecRrfReranker().RankConstant.Should().Be(ZVecDefaults.Rerank.RankConstant);
    }

    [Fact]
    public void IndexPolicy_Rejects128WideStore()
    {
        var policy = new Recall.Core.Indexing.RecallIndexPolicy();
        var act = () => policy.EnsureStorableWidth(Recall.Core.Vocabulary.Modality.Text, 128);
        act.Should().Throw<Recall.Core.Faults.RecallFaultException>().Which.Fault.Should().Be(Recall.Core.Faults.RecallFault.StoredWidthBlocked);
    }

    [Fact]
    public async Task HybridQuery_ReturnsGrantedHit_FromPinnedNote()
    {
        await using var provider = BuildProvider();
        var search = provider.GetRequiredService<ITextSearchService>();
        var grantPath = Path.Combine(root, "grant");
        var grant = new ScopeFolderGrant(grantPath, "grant", GrantFileKinds.Text);
        provider.GetRequiredService<IScopeManagerService>().TryAddGrant(grant);
        var grants = new List<ScopeFolderGrant> { grant };
        Directory.CreateDirectory(grantPath);
        var notePath = Path.Combine(grants[0].Path, "mars.txt");
        const string note = "The red planet is Mars.";
        await File.WriteAllTextAsync(notePath, note, TestContext.Current.CancellationToken);

        await provider.GetRequiredService<IGrantedTextIngestService>()
            .IngestGrantedTextAsync(TestContext.Current.CancellationToken);

        var hits = await search.FindAsync("red planet", grants, TestContext.Current.CancellationToken);
        hits.Should().NotBeEmpty();
        hits[0].SourcePath.Should().Contain("mars.txt");
        hits.Should().OnlyContain(h => h.SourcePath.StartsWith(grants[0].Path, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Open_StaleTextCollection_ReplacesAndClearsCursors()
    {
        var databaseRoot = Path.Combine(root, "zvec_stale");
        CreateStaleTextCollection(databaseRoot);
        var store = new SqliteIngestCursorStore(Path.Combine(root, "stale-checkpoints", CheckpointSql.FileName));
        const string grant = "g";
        store.MarkReading("a.txt", "h1", grant);
        store.FinishPath("a.txt", "h1", grant, "enc", [new ChunkWrite("id1", 0)]);
        store.PutBm25Only("bm25", "a.txt", "123");
        store.EnqueuePending("id1");
        store.MarkGrantFinished(grant);

        using var index = new ZVecTextVectorIndex(databaseRoot, cursors: store);
        var vector = new float[512];
        vector[0] = 1f;
        var inserted = index.TryInsert(SampleInsert(
            ContentIdBuilder.Build(
                Recall.Core.Vocabulary.Modality.Text,
                ContentIdBuilder.HashSourceContent("fresh note"),
                Recall.Core.Vocabulary.StoredWidth.W512,
                0),
            Path.Combine(root, "fresh.txt"),
            "fresh note about mars",
            vector,
            Path.Combine(root, "fresh.txt")));

        inserted.Should().BeTrue();
        store.GetFile("a.txt").Should().BeNull();
        store.ListChunks("a.txt").Should().BeEmpty();
        store.AnyLivePath("id1").Should().BeFalse();
        store.ListBm25Only().Should().BeEmpty();
        store.PendingCount().Should().Be(0);
        store.IsGrantFinished(grant).Should().BeFalse();
        var query = new float[512];
        query[0] = 1f;
        var hits = index.Search(new TextSearchRequest(
            "mars",
            query,
            new HashSet<string>([Path.Combine(root, "fresh.txt")], StringComparer.OrdinalIgnoreCase),
            "integration-test-hash",
            TextSearchFilter.Build(new HashSet<string>([Path.Combine(root, "fresh.txt")], StringComparer.OrdinalIgnoreCase), "integration-test-hash"),
            true,
            20), TestContext.Current.CancellationToken);
        hits.Should().ContainSingle(h => h.SourcePath.EndsWith("fresh.txt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Open_CurrentTextCollection_KeepsRowsAndCursors()
    {
        var databaseRoot = Path.Combine(root, "zvec_keep");
        var vector = new float[512];
        vector[0] = 1f;
        var contentId = ContentIdBuilder.Build(
            Recall.Core.Vocabulary.Modality.Text,
            ContentIdBuilder.HashSourceContent("kept note"),
            Recall.Core.Vocabulary.StoredWidth.W512,
            0);
        using (var created = new ZVecTextVectorIndex(databaseRoot))
        {
            created.TryInsert(SampleInsert(
                contentId,
                Path.Combine(root, "kept.txt"),
                "kept note about mars",
                vector,
                Path.Combine(root, "kept.txt"))).Should().BeTrue();
        }

        var store = new SqliteIngestCursorStore(Path.Combine(root, "keep-checkpoints", CheckpointSql.FileName));
        store.MarkReading("kept.txt", "h1", "g");
        store.FinishPath("kept.txt", "h1", "g", "enc", [new ChunkWrite(contentId, 0)]);
        using var reopened = new ZVecTextVectorIndex(databaseRoot, cursors: store);
        store.GetFile("kept.txt").Should().NotBeNull();
        var query = new float[512];
        query[0] = 1f;
        var hits = reopened.Search(new TextSearchRequest(
            "mars",
            query,
            new HashSet<string>([Path.Combine(root, "kept.txt")], StringComparer.OrdinalIgnoreCase),
            "integration-test-hash",
            TextSearchFilter.Build(new HashSet<string>([Path.Combine(root, "kept.txt")], StringComparer.OrdinalIgnoreCase), "integration-test-hash"),
            true,
            20), TestContext.Current.CancellationToken);
        hits.Should().ContainSingle(h => h.SourcePath.EndsWith("kept.txt", StringComparison.OrdinalIgnoreCase));
    }

    private static void CreateStaleTextCollection(string databaseRoot)
    {
        Directory.CreateDirectory(databaseRoot);
        var schema = new ZVecCollectionSchema
        {
            Name = ZVecTextVectorIndex.CollectionName,
            Fields =
            [
                new ZVecFieldSchema { Name = ZVecTextVectorIndex.SourcePathField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = ZVecTextVectorIndex.FileNameField, DataType = ZVecDataType.String },
                new ZVecFieldSchema { Name = ZVecTextVectorIndex.ModifiedField, DataType = ZVecDataType.String },
            ],
            Vectors =
            [
                new ZVecVectorSchema
                {
                    Name = ZVecTextVectorIndex.EmbeddingField,
                    DataType = ZVecDataType.VectorFp32,
                    Dimension = 512,
                    IndexParam = new ZVecHnswIndexParam
                    {
                        MetricType = ZVecMetricType.Cosine,
                        M = RecallIndexPolicy.TextHnswM,
                        EfConstruction = RecallIndexPolicy.TextHnswEfConstruction,
                        QuantizeType = ZVecQuantizeType.Undefined,
                    },
                },
                new ZVecVectorSchema
                {
                    Name = ZVecTextVectorIndex.RawTextField,
                    DataType = ZVecDataType.String,
                    Dimension = 0,
                    IndexParam = new ZVecFtsIndexParam
                    {
                        Tokenizer = ZVecFtsTokenizer.Standard,
                        Filters = [ZVecFtsTokenFilter.Lowercase],
                    },
                },
            ],
        };
        var factory = new ZVecFactory();
        factory.Initialize();
        var collection = factory.CreateAndOpen(Path.Combine(databaseRoot, ZVecTextVectorIndex.CollectionName), schema);
        collection.Dispose();
        factory.Dispose();
    }

    private static TextIndexInsert SampleInsert(string contentId, string path, string raw, float[] vector, string grantPath) =>
        new(
            contentId,
            path,
            Path.GetFileName(path),
            raw,
            vector,
            DateTimeOffset.UtcNow,
            "integration-test-hash",
            true,
            grantPath,
            raw.ToLowerInvariant(),
            0);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch
            {
                // best effort
            }
        }
    }
}
