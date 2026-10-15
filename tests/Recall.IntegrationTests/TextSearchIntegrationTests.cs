using FluentAssertions;
using Recall.Adapters.Indexing;
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
        var inserted = index.TryInsert(new TextIndexInsert(
            ContentIdBuilder.Build(
                Recall.Core.Vocabulary.Modality.Text,
                ContentIdBuilder.HashSourceContent("pinned note"),
                Recall.Core.Vocabulary.StoredWidth.W512,
                0),
            Path.Combine(root, "note.txt"),
            "note.txt",
            "pinned note about mars rovers",
            vector,
            DateTimeOffset.UtcNow));
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
        index.TryInsert(new TextIndexInsert(
            ContentIdBuilder.Build(
                Recall.Core.Vocabulary.Modality.Text,
                ContentIdBuilder.HashSourceContent("outside note"),
                Recall.Core.Vocabulary.StoredWidth.W512,
                0),
            outsidePath,
            "note.txt",
            "outside note about mars",
            outsideVector,
            DateTimeOffset.UtcNow));
        index.TryInsert(new TextIndexInsert(
            ContentIdBuilder.Build(
                Recall.Core.Vocabulary.Modality.Text,
                ContentIdBuilder.HashSourceContent("inside note"),
                Recall.Core.Vocabulary.StoredWidth.W512,
                0),
            insidePath,
            "note.txt",
            "inside note about mars",
            insideVector,
            DateTimeOffset.UtcNow));
        var query = new float[512];
        query[0] = 1f;
        var hits = index.Search(new TextSearchRequest(
            "mars",
            query,
            new HashSet<string>([insideDir], StringComparer.OrdinalIgnoreCase),
            TopK: 20), TestContext.Current.CancellationToken);
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
