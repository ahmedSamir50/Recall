using Recall.Core.Ports;

namespace Recall.Adapters.Embedding;

public sealed class LazyOnnxTextEmbeddingEncoder : ITextEmbeddingEncoder
{
    private readonly TextModelRuntime runtime;

    public LazyOnnxTextEmbeddingEncoder(TextModelRuntime runtime)
    {
        this.runtime = runtime;
    }

    public ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
    {
        return runtime.GetEncoder().EmbedPrefixedTextAsync(prefixedText, cancellationToken);
    }
}
