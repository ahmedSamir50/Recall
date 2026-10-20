using System.Security.Cryptography;
using FluentAssertions;
using Recall.Adapters.Embedding;
using Recall.Core.Download;
using Recall.IntegrationTests.Infrastructure;

namespace Recall.IntegrationTests;

public sealed class LlamaEmbedIntegrationTests
{
    [Fact]
    public async Task Embed_LocalGguf_Is768AndUnit()
    {
        var directory = IntegrationTextModelPaths.ResolveModelDirectory();

        var modelPath = Path.Combine(directory, IntegrationTextModelPaths.ModelFileName);
        var options = new TextModelDownloadOptions();
        var bytes = await File.ReadAllBytesAsync(modelPath, TestContext.Current.CancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        hash.Should().Be(options.ModelSha256);

        using var runtime = new TextModelRuntime(directory);
        var encoder = runtime.GetEncoder();
        var counter = runtime.GetCounter();
        var vector = await encoder.EmbedPrefixedTextAsync(
            "task: search result | query: red planet",
            TestContext.Current.CancellationToken);
        vector.Length.Should().Be(768);
        L2Norm(vector.Span).Should().BeInRange(0.999, 1.001);
        counter.CountTokens("red planet", TestContext.Current.CancellationToken).Should().BeGreaterThan(0);
    }

    private static double L2Norm(ReadOnlySpan<float> vector)
    {
        double sum = 0;
        foreach (var value in vector)
        {
            sum += value * value;
        }

        return Math.Sqrt(sum);
    }
}
