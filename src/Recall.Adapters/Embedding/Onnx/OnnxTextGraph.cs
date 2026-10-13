namespace Recall.Adapters.Embedding;

public static class OnnxTextGraph
{
    public const string InputIds = "input_ids";

    public const string AttentionMask = "attention_mask";

    public const string PixelValues = "pixel_values";

    public const string PixelAttentionMask = "pixel_attention_mask";

    public const string AudioValues = "audio_values";

    public const string AudioAttentionMask = "audio_attention_mask";

    public const string SentenceOutputMarker = "sentence";

    public const int EmptyModalityWidth = 512;
}
