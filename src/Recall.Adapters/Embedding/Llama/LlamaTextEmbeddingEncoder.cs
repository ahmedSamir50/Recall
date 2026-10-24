using Recall.Core.Ports;
using Recall.Core.Vectors;

namespace Recall.Adapters.Embedding.Llama;

public sealed class LlamaTextEmbeddingEncoder : ITextEmbeddingEncoder, IDisposable
{
    private readonly IRecallEmbedLibrary library;
    private int disposed;

    internal LlamaTextEmbeddingEncoder(IRecallEmbedLibrary library)
    {
        this.library = library;
    }

    public async ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefixedText);
        cancellationToken.ThrowIfCancellationRequested();
        await LlamaNativeGate.EnterAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var buffer = new float[MatryoshkaVectorMath.NativeWidth];
            var code = library.Embed(prefixedText, buffer);
            if (code != 0)
            {
                throw new InvalidOperationException($"recall_embed embed failed with code {code}.");
            }

            MatryoshkaVectorMath.L2Normalize(buffer);
            return buffer;
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    public void Dispose() => disposed = 1;
}
