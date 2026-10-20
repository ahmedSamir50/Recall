using Recall.Core.Ports;

namespace Recall.Adapters.Embedding.Llama;

public sealed class LlamaTextTokenCounter : ITextTokenCounter
{
    private readonly IRecallEmbedLibrary library;
    private readonly SemaphoreSlim gate;

    internal LlamaTextTokenCounter(IRecallEmbedLibrary library, SemaphoreSlim gate)
    {
        this.library = library;
        this.gate = gate;
    }

    public int CountTokens(string text) => CountTokens(text, default);

    public int CountTokens(string text, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        cancellationToken.ThrowIfCancellationRequested();
        gate.Wait(cancellationToken);
        try
        {
            return library.CountTokens(text);
        }
        finally
        {
            gate.Release();
        }
    }
}
