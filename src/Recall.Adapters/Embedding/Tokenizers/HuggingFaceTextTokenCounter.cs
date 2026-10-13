using Recall.Core.Ports;
using Tokenizers.DotNet;

namespace Recall.Adapters.Embedding;

public sealed class HuggingFaceTextTokenCounter : ITextTokenCounter, IDisposable
{
    private readonly Tokenizer tokenizer;
    private readonly bool ownsTokenizer;

    public HuggingFaceTextTokenCounter(Tokenizer tokenizer, bool ownsTokenizer)
    {
        this.tokenizer = tokenizer;
        this.ownsTokenizer = ownsTokenizer;
    }

    public int CountTokens(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return tokenizer.Encode(text).Length;
    }

    public void Dispose()
    {
        if (ownsTokenizer)
        {
            tokenizer.Dispose();
        }
    }
}
