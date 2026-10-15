namespace Recall.Adapters.Embedding;

public static class OnnxTextGraph
{
    public const string InputIds = "input_ids";

    public const string AttentionMask = "attention_mask";

    public const string ImageFeatures = "image_features";

    public const string VideoFeatures = "video_features";

    public const string AudioFeatures = "audio_features";

    public const string SentenceOutputMarker = "sentence";

    public static int[] EmptyFeatureDimensions(int[] metadataDimensions)
    {
        ArgumentNullException.ThrowIfNull(metadataDimensions);
        if (metadataDimensions.Length == 0)
        {
            throw new ArgumentException("Dimensions must not be empty.", nameof(metadataDimensions));
        }

        var result = new int[metadataDimensions.Length];
        var hasZero = false;
        for (var i = 0; i < metadataDimensions.Length; i++)
        {
            var value = metadataDimensions[i] < 0 ? 0 : metadataDimensions[i];
            result[i] = value;
            if (value == 0)
            {
                hasZero = true;
            }
        }

        if (!hasZero)
        {
            result[0] = 0;
        }

        return result;
    }

    public static void RequireTextInputs(IEnumerable<string> inputNames)
    {
        ArgumentNullException.ThrowIfNull(inputNames);
        var names = inputNames.ToHashSet(StringComparer.Ordinal);
        if (!names.Contains(InputIds)
            || !names.Contains(AttentionMask)
            || !names.Contains(ImageFeatures)
            || !names.Contains(VideoFeatures)
            || !names.Contains(AudioFeatures))
        {
            throw new InvalidOperationException("Text model inputs are missing.");
        }
    }

    public static void RequireFloat(Type elementType)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        if (elementType != typeof(float))
        {
            throw new InvalidOperationException("Feature input must be float.");
        }
    }
}
