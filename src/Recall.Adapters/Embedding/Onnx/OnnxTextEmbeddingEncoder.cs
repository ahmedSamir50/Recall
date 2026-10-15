using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Recall.Core.Ports;
using Recall.Core.Vectors;
using Tokenizers.DotNet;

namespace Recall.Adapters.Embedding;

/// <summary>Embeds prefixed text with the EmbeddingGemma ONNX model (text path only).</summary>
/// <remarks>Image, video, and audio features are sent as empty tensors sized from the model
/// metadata, so a text query runs without vision or audio input. The gate allows one inference
/// at a time because the session and tokenizer are not thread-safe.</remarks>
public sealed class OnnxTextEmbeddingEncoder : ITextEmbeddingEncoder, IDisposable
{
    private readonly InferenceSession session;
    private readonly Tokenizer tokenizer;
    private readonly bool ownsTokenizer;
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>Opens the ONNX session for a model file. The tokenizer is shared.</summary>
    /// <param name="modelPath">Path to the text ONNX file.</param>
    /// <param name="tokenizer">Tokenizer matching the model's tokenizer.json.</param>
    /// <param name="ownsTokenizer">True to dispose the tokenizer with this encoder.</param>
    public OnnxTextEmbeddingEncoder(string modelPath, Tokenizer tokenizer, bool ownsTokenizer)
    {
        session = new InferenceSession(modelPath);
        this.tokenizer = tokenizer;
        this.ownsTokenizer = ownsTokenizer;
    }

    /// <summary>Embeds one prefixed text and returns an L2-normalized 768-wide vector.</summary>
    /// <remarks>Steps: tokenize to ids plus an all-ones attention mask, verify the five
    /// graph inputs exist and the feature inputs are float, run with empty image, video,
    /// and audio features, prefer the output whose name contains "sentence", mean-pool only
    /// when the output is not already 768 wide, then L2-normalize.</remarks>
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

            OnnxTextGraph.RequireTextInputs(session.InputMetadata.Keys);
            OnnxTextGraph.RequireFloat(session.InputMetadata[OnnxTextGraph.ImageFeatures].ElementType);
            OnnxTextGraph.RequireFloat(session.InputMetadata[OnnxTextGraph.VideoFeatures].ElementType);
            OnnxTextGraph.RequireFloat(session.InputMetadata[OnnxTextGraph.AudioFeatures].ElementType);
            var imageFeatures = new DenseTensor<float>(OnnxTextGraph.EmptyFeatureDimensions(session.InputMetadata[OnnxTextGraph.ImageFeatures].Dimensions));
            var videoFeatures = new DenseTensor<float>(OnnxTextGraph.EmptyFeatureDimensions(session.InputMetadata[OnnxTextGraph.VideoFeatures].Dimensions));
            var audioFeatures = new DenseTensor<float>(OnnxTextGraph.EmptyFeatureDimensions(session.InputMetadata[OnnxTextGraph.AudioFeatures].Dimensions));

            var inputs = new List<NamedOnnxValue>(5)
            {
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.InputIds, new DenseTensor<long>(ids, [1, length])),
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.AttentionMask, new DenseTensor<long>(attention, [1, length])),
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.ImageFeatures, imageFeatures),
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.VideoFeatures, videoFeatures),
                NamedOnnxValue.CreateFromTensor(OnnxTextGraph.AudioFeatures, audioFeatures),
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

    /// <summary>Releases the gate, the tokenizer when owned, and the ONNX session.</summary>
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
