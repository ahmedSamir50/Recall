using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recall.Adapters.Embedding;
using Recall.Core.Age;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Vocabulary;
using Recall.IntegrationTests.Infrastructure;
using Recall.Maui.Platform;

namespace Recall.IntegrationTests;

public sealed class AgeCutDemoProofTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "recall_agecut_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void AgeCut_DemoMedia_Passes()
    {
        DemoDatasetPaths.RequireRoot();
        Directory.CreateDirectory(root);
        var modelDir = IntegrationTextModelPaths.ResolveModelDirectory();
        var mmprojPath = MediaContextProbePaths.ResolveMmprojFile();
        var visionDir = Path.GetDirectoryName(mmprojPath)!;
        var modelPath = Path.Combine(modelDir, IntegrationTextModelPaths.ModelFileName);
        MediaContextProbe.EnsureApplied(modelPath, mmprojPath);
        using var provider = IntegrationTestServiceCollection.Build(modelDir, Path.Combine(root, "zvec"), visionDir);
        AgeCutCorpus.Configure(
            provider.GetRequiredService<IVisionEmbeddingEncoder>(),
            provider.GetRequiredService<IAudioEmbeddingEncoder>(),
            provider.GetRequiredService<IVideoFrameSamplerService>(),
            provider.GetRequiredService<IAudioSource>(),
            provider.GetRequiredService<IIngestModelSession>());
        foreach (var modality in new[] { Modality.Image, Modality.Video, Modality.Audio })
        {
            var loaded = AgeCutCorpus.TryLoad(modality, out var items);
            loaded.Should().BeTrue($"demo corpus missing for {modality}");
            items.Should().NotBeEmpty();
            var passes = AgeCutGate.Passes(modality, items);
            if (!passes)
            {
                throw new InvalidOperationException("Age cut gate failed.");
            }
        }
    }
}
