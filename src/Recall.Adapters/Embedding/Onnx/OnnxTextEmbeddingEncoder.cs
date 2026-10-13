using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Recall.Core.Ports;
using Recall.Core.Vectors;
using Tokenizers.DotNet;

namespace Recall.Adapters.Embedding;

public sealed class OnnxTextEmbeddingEncoder : ITextEmbeddingEncoder, IDisposable
{
    private static readonly DenseTensor<float> EmptyModality = new([0, OnnxTextGraph.EmptyModalityWidth]);

    private readonly InferenceSession session;
    private readonly Tokenizer tokenizer;
    private readonly bool ownsTokenizer;
    private readonly SemaphoreSlim gate = new(1, 1);

    public OnnxTextEmbeddingEncoder(string modelPath, Tokenizer tokenizer, bool ownsTokenizer)
    {
        session = new InferenceSession(modelPath);
        this.tokenizer = tokenizer;
        this.ownsTokenizer = ownsTokenizer;
    }

    public async ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefixedText);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var encoded = tokenizer.Encode(prefixedText);
            var length = encoded.Length;
            var ids = new long[length];
            var attention = new long[length];
            for (var i = 0; i < length; i++)
            {
                ids[i] = encoded[i];
                attention[i] = 1;
            }

            var inputs = new List<NamedOnnxValue>(6)
            {
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.InputIds, new DenseTensor<long>(ids, [1, length])),
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.AttentionMask, new DenseTensor<long>(attention, [1, length])),
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.PixelValues, EmptyModality),
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.PixelAttentionMask, EmptyModality),
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.AudioValues, EmptyModality),
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.AudioAttentionMask, EmptyModality),
            };

            using var results = session.Run(inputs);
            var chosen = results.FirstOrDefault(result => result.Name.Contains(OnnxTextGraph.SentenceOutputMarker, StringComparison.OrdinalIgnoreCase))
                ?? results[0];
            var tensor = chosen.AsTensor<float>();
            var flat = tensor.ToArray();
            var hidden = tensor.Dimensions[^1];
            var vector = flat.Length == MatryoshkaVectorMath.NativeWidth
                ? flat
                : TokenMeanPool.Mean(flat, hidden);
            MatryoshkaVectorMath.L2Normalize(vector);
            return vector;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        gate.Dispose();
        if (ownsTokenizer)
        {
            tokenizer.Dispose();
        }

        session.Dispose();
    }
}
