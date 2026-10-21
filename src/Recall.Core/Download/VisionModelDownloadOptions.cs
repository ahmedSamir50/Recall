namespace Recall.Core.Download;

public sealed class VisionModelDownloadOptions
{
    public const string SectionName = "VisionModel";

    public string ModelBaseUrl { get; set; } = "https://huggingface.co/ggml-org/embeddinggemma-2-GGUF/resolve/main/";

    public string ModelFileName { get; set; } = "mmproj-embeddinggemma-2-Q8_0.gguf";

    public string ModelSha256 { get; set; } = "c4a8a52691ecef40618438928bdf9e68379b854e24166f292592353db0aab64f";

    public string RelativeModelDirectory { get; set; } = "models/vision";

    public Uri ModelBaseUri => new(ModelBaseUrl);
}
