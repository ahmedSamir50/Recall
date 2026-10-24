using FluentAssertions;
using Recall.Adapters.Embedding;
using Recall.Core.Ingest;
using Recall.IntegrationTests.Infrastructure;

namespace Recall.IntegrationTests;

public sealed class MediaContextProbeTests
{
    [Fact]
    public async Task Mtmd_OneContext_EmbedsVisionThenAudio()
    {
        var directory = IntegrationTextModelPaths.ResolveModelDirectory();
        var modelPath = Path.Combine(directory, IntegrationTextModelPaths.ModelFileName);
        await File.ReadAllBytesAsync(modelPath, TestContext.Current.CancellationToken);
        var mmprojPath = MediaContextProbePaths.ResolveMmprojFile();

        var wasOneContext = MediaContextPolicy.OneContext;
        try
        {
            MediaContextProbe.EnsureApplied(modelPath, mmprojPath);
            MediaContextPolicy.OneContext.Should().BeTrue();
        }
        finally
        {
            MediaContextPolicy.OneContext = wasOneContext;
        }
    }
}
