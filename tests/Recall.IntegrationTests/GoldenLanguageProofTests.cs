using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.IntegrationTests.Infrastructure;

namespace Recall.IntegrationTests;

public sealed class GoldenLanguageProofTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "recall_golden_lang_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (IOException)
        {
            // Native ZVec may release checkpoints.db shortly after the provider disposes.
        }
    }

    [Fact]
    public async Task Golden_SevenLanguages_SpreadWithinFive()
    {
        Environment.SetEnvironmentVariable("RECALL_EMBED_CPU_ONLY", "1");
        DemoDatasetPaths.RequireRoot();
        Directory.CreateDirectory(root);
        var modelDir = IntegrationTextModelPaths.ResolveModelDirectory();
        await using var provider = IntegrationTestServiceCollection.Build(modelDir, Path.Combine(root, "zvec"));
        var search = provider.GetRequiredService<ITextSearchService>();
        var scope = provider.GetRequiredService<IScopeManagerService>();
        var grantPath = Path.Combine(root, "grant");
        Directory.CreateDirectory(grantPath);
        var grant = new ScopeFolderGrant(grantPath, "golden", GrantFileKinds.Text);
        scope.TryAddGrant(grant);
        var grants = new List<ScopeFolderGrant> { grant };

        var cases = new (string SourceNote, string DestName)[]
        {
            ("notes/ar_رحلة_أسوان.md", "ar_aswan.md"),
            ("notes/pt_lisboa.md", "pt_lisboa.md"),
            ("notes/fr_recette_ratatouille.md", "fr_ratatouille.md"),
            ("notes/nl_fiets_utrecht.md", "nl_fiets.md"),
            ("notes/de_fahrrad_reparatur.md", "de_fahrrad.md"),
            ("notes/es_viaje_granada.md", "es_granada.md"),
            ("notes/en_sourdough_bread_recipe.md", "en_sourdough.md"),
        };

        var recalls = new List<double>(cases.Length);
        foreach (var (sourceNote, destName) in cases)
        {
            var sourcePath = DemoDatasetPaths.Combine(sourceNote);
            File.Exists(sourcePath).Should().BeTrue($"missing {sourcePath}");
            var text = await File.ReadAllTextAsync(sourcePath, TestContext.Current.CancellationToken);
            var query = FirstSentence(text);
            query.Should().NotBeNullOrWhiteSpace();
            var destPath = Path.Combine(grantPath, destName);
            await File.WriteAllTextAsync(destPath, text, TestContext.Current.CancellationToken);
        }

        await provider.GetRequiredService<IGrantedTextIngestService>()
            .IngestGrantedTextAsync(TestContext.Current.CancellationToken);

        foreach (var (sourceNote, destName) in cases)
        {
            var text = await File.ReadAllTextAsync(DemoDatasetPaths.Combine(sourceNote), TestContext.Current.CancellationToken);
            var query = FirstSentence(text);
            var destPath = Path.Combine(grantPath, destName);
            var hits = await search.FindAsync(query, grants, TestContext.Current.CancellationToken);
            var rank = hits.ToList().FindIndex(h => string.Equals(h.SourcePath, destPath, StringComparison.OrdinalIgnoreCase));
            var recallAt10 = rank >= 0 && rank < 10 ? 100d : 0d;
            recalls.Add(recallAt10);
        }

        (recalls.Max() - recalls.Min()).Should().BeLessThanOrEqualTo(5d);
        recalls.Should().AllSatisfy(r => r.Should().BeGreaterThan(99.99d));
    }

    private static string FirstSentence(string text)
    {
        var trimmed = text.Trim();
        var end = trimmed.IndexOfAny(['.', '!', '?', '\n', '\r']);
        return end < 0 ? trimmed : trimmed[..end].Trim();
    }
}
