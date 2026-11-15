using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recall.Adapters.Embedding;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.IntegrationTests.Infrastructure;

namespace Recall.IntegrationTests;

public sealed class SearchProofIntegrationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "recall_search_proof_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        try
        {
            Directory.Delete(root, true);
        }
        catch
        {
            // best effort
        }
    }

    [Fact]
    public async Task ExactImage_SameBytes_IsHitZero()
    {
        Environment.SetEnvironmentVariable("RECALL_EMBED_CPU_ONLY", "1");
        var modelDir = IntegrationTextModelPaths.ResolveModelDirectory();
        var mmproj = MediaContextProbePaths.ResolveMmprojFile();
        var visionDir = Path.GetDirectoryName(mmproj) ?? modelDir;
        MediaContextProbe.EnsureApplied(Path.Combine(modelDir, IntegrationTextModelPaths.ModelFileName), mmproj);

        Directory.CreateDirectory(root);
        await using var provider = IntegrationTestServiceCollection.Build(modelDir, Path.Combine(root, "zvec"), visionDir);
        var grantPath = Path.Combine(root, "pics");
        Directory.CreateDirectory(grantPath);
        var grant = new ScopeFolderGrant(grantPath, "pics", GrantFileKinds.Image);
        provider.GetRequiredService<IScopeManagerService>().TryAddGrant(grant);
        var grants = new List<ScopeFolderGrant> { grant };

        var paths = new string[6];
        for (var i = 0; i < paths.Length; i++)
        {
            paths[i] = Path.Combine(grantPath, $"img{i + 1}.jpg");
            await File.WriteAllBytesAsync(paths[i], DistinctJpeg(i + 2), TestContext.Current.CancellationToken);
        }

        var vision = provider.GetRequiredService<IVisionFileIngest>();
        foreach (var path in paths)
        {
            await vision.IndexImageAsync(grant, path, TestContext.Current.CancellationToken);
        }

        var queryBytes = await File.ReadAllBytesAsync(paths[0], TestContext.Current.CancellationToken);
        var hits = await provider.GetRequiredService<IPictureSearchService>()
            .FindAsync(queryBytes, grants, TestContext.Current.CancellationToken);

        hits.Should().NotBeEmpty();
        hits[0].SourcePath.Should().Be(paths[0]);
    }

    [Fact]
    public async Task ExactWord_BeatsFiveOtherFiles()
    {
        Environment.SetEnvironmentVariable("RECALL_EMBED_CPU_ONLY", "1");
        var modelDir = IntegrationTextModelPaths.ResolveModelDirectory();
        Directory.CreateDirectory(root);
        await using var provider = IntegrationTestServiceCollection.Build(modelDir, Path.Combine(root, "zvec"));
        var grantPath = Path.Combine(root, "notes");
        Directory.CreateDirectory(grantPath);
        var grant = new ScopeFolderGrant(grantPath, "notes", GrantFileKinds.Text);
        provider.GetRequiredService<IScopeManagerService>().TryAddGrant(grant);
        var grants = new List<ScopeFolderGrant> { grant };

        const string keyword = "xylophonequartz";
        var topics = new[]
        {
            "Urban bicycle commuting includes helmet habits, lane positioning, and weather planning for safe daily rides.",
            "Sourdough baking needs a lively starter, long ferments, careful scoring, and steady oven steam for open crumb.",
            "Marine biology surveys track coral bleaching, fish counts, plankton blooms, and water temperature along reefs.",
            "Database normalization reduces update anomalies, preserves integrity, and keeps reporting schemas maintainable.",
            "Garden composting balances greens and browns, aerates piles weekly, and keeps moisture steady for breakdown.",
        };

        var targetPath = Path.Combine(grantPath, "six.txt");
        for (var i = 0; i < topics.Length; i++)
        {
            var path = Path.Combine(grantPath, $"note{i + 1}.txt");
            await File.WriteAllTextAsync(path, topics[i], TestContext.Current.CancellationToken);
        }

        await File.WriteAllTextAsync(
            targetPath,
            $"Instrument essays mention resonance, mallets, and tone. Only this file contains the token {keyword} in the whole set.",
            TestContext.Current.CancellationToken);

        await provider.GetRequiredService<IGrantedTextIngestService>()
            .IngestGrantedTextAsync(TestContext.Current.CancellationToken);

        var hits = await provider.GetRequiredService<ITextSearchService>()
            .FindAsync(keyword, grants, TestContext.Current.CancellationToken);

        hits.Count.Should().BeGreaterThan(1);
        hits[0].SourcePath.Should().Be(targetPath);
    }

    private static byte[] DistinctJpeg(int dimension)
    {
        var jpeg = (byte[])MediaContextProbe.MinimalJpeg().Clone();
        var sof = FindMarker(jpeg, 0xC0);
        jpeg[sof + 5] = (byte)(dimension >> 8);
        jpeg[sof + 6] = (byte)(dimension & 0xFF);
        jpeg[sof + 7] = (byte)(dimension >> 8);
        jpeg[sof + 8] = (byte)(dimension & 0xFF);
        return jpeg;
    }

    private static int FindMarker(byte[] jpeg, byte marker)
    {
        for (var i = 0; i < jpeg.Length - 1; i++)
        {
            if (jpeg[i] == 0xFF && jpeg[i + 1] == marker)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"JPEG marker 0x{marker:X2} not found.");
    }
}