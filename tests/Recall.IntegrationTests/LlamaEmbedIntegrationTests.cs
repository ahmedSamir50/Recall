using FluentAssertions;
using Recall.Adapters.Embedding;
using Recall.Adapters.Embedding.Llama;
using Recall.Core.Download;
using Recall.IntegrationTests.Infrastructure;
using System.Security.Cryptography;
using System.Text;

[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

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

    [Fact]
    public void TokenEnds_RealLibrary_OneCall()
    {
        var directory = IntegrationTextModelPaths.ResolveModelDirectory();
        var library = new CountingRecallEmbedLibrary();
        using var runtime = new TextModelRuntime(directory, library);
        runtime.OpenSession();
        var document = Document(200 * 1024);

        var ends = library.TokenEnds(document);

        library.TokenEndsCalls.Should().Be(1, "the uncapped native entry point returns the whole token map in one call");
        ends.Length.Should().BeGreaterThan(7680, "the old RECALL_EMBED_MAX_TOKENS cap stopped the map at 7680 tokens");
        ends.Should().BeInAscendingOrder();
        ends[^1].Should().Be(Encoding.UTF8.GetByteCount(document), "the last token end covers the whole document");
    }

    private static string Document(int bytes)
    {
        const string sentence = "The red planet has two small moons and a thin carbon dioxide atmosphere. ";
        var sentenceBytes = Encoding.UTF8.GetByteCount(sentence);
        var builder = new StringBuilder(bytes);
        for (var written = 0; written < bytes; written += sentenceBytes)
        {
            builder.Append(sentence);
        }

        return builder.ToString();
    }

    private sealed class CountingRecallEmbedLibrary : IRecallEmbedLibrary
    {
        private readonly RecallEmbedNative inner = new();

        public int TokenEndsCalls { get; private set; }

        public int Open(string modelPath) => inner.Open(modelPath);

        public void Close() => inner.Close();

        public int CountTokens(string text) => inner.CountTokens(text);

        public int[] TokenEnds(string text)
        {
            TokenEndsCalls++;
            return inner.TokenEnds(text);
        }

        public int Embed(string text, float[] destination768) => inner.Embed(text, destination768);

        public int OpenVision(string mmprojPath) => inner.OpenVision(mmprojPath);

        public int EmbedVision(byte[] jpeg, float[] destination768) => inner.EmbedVision(jpeg, destination768);

        public void CloseVision() => inner.CloseVision();

        public int OpenAudio(string mmprojPath) => inner.OpenAudio(mmprojPath);

        public int EmbedAudio(float[] samples, float[] destination768) => inner.EmbedAudio(samples, destination768);

        public void CloseAudio() => inner.CloseAudio();
    }
}
