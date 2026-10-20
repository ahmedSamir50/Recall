namespace Recall.Core.Download;

public sealed class TextModelDownloadOptions
{
    public const string SectionName = "TextModel";

    public string ModelBaseUrl { get; set; } = "https://huggingface.co/ggml-org/embeddinggemma-2-GGUF/resolve/main/";

    public string ModelFileName { get; set; } = "embeddinggemma-2-Q8_0.gguf";

    public string ModelSha256 { get; set; } = "2188ac1deca4b77dffefd603c2776a9d76d9d74ec01841392982ebb840b09135";

    public string UserAgentProduct { get; set; } = "Recall";

    public string UserAgentVersion { get; set; } = "0.1";

    public string RelativeModelDirectory { get; set; } = "models/text";

    public string RelativeVectorDirectory { get; set; } = "zvec";

    public Uri ModelBaseUri => new(ModelBaseUrl);
}
