using Recall.Core.Ports;

namespace Recall.Adapters.Embedding;

public sealed class LazyTextTokenCounter : ITextTokenCounter
{
    private readonly TextModelRuntime runtime;

    public LazyTextTokenCounter(TextModelRuntime runtime)
    {
        this.runtime = runtime;
    }

    public int CountTokens(string text) => runtime.GetCounter().CountTokens(text);
}
