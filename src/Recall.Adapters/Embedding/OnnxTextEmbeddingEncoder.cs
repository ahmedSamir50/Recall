using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Recall.Core.Ports;
using Recall.Core.Vectors;
using Tokenizers.DotNet;

namespace Recall.Adapters.Embedding;

public sealed class OnnxTextEmbeddingEncoder : ITextEmbeddingEncoder, IDisposable
{
    private static readonly DenseTensor<float> EmptyModality = new([0, 512]);

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
                NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(ids, [1, length])),
                NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attention, [1, length])),
                NamedOnnxValue.CreateFromTensor("pixel_values", EmptyModality),
                NamedOnnxValue.CreateFromTensor("pixel_attention_mask", EmptyModality),
                NamedOnnxValue.CreateFromTensor("audio_values", EmptyModality),
                NamedOnnxValue.CreateFromTensor("audio_attention_mask", EmptyModality),
            };

            using var results = session.Run(inputs);
            var tensor = ReadSentenceTensor(results);
            var flat = tensor.ToArray();
            var vector = flat.Length == MatryoshkaVectorMath.NativeWidth
                ? flat
                : TokenMeanPool.Mean(flat, tensor.Dimensions[^1]);
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

    private static DenseTensor<float> ReadSentenceTensor(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
    {
        var chosen = results.FirstOrDefault(result => result.Name.Contains("sentence", StringComparison.OrdinalIgnoreCase))
            ?? results[0];
        return chosen.AsTensor<float>().ToDenseTensor();
    }
}
