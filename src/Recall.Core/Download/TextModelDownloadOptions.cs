namespace Recall.Core.Download;

public sealed class TextModelDownloadOptions
{
    public string OnnxBaseUrl { get; set; } = "https://huggingface.co/onnx-community/embeddinggemma-2-ONNX/resolve/main/onnx/";

    public string RepositoryBaseUrl { get; set; } = "https://huggingface.co/onnx-community/embeddinggemma-2-ONNX/resolve/main/";

    public string OnnxFileName { get; set; } = "model_quantized.onnx";

    public string OnnxDataFileName { get; set; } = "model_quantized.onnx_data";

    public string TokenizerFileName { get; set; } = "tokenizer.json";

    public string OnnxSha256 { get; set; } = string.Empty;

    public string OnnxDataSha256 { get; set; } = string.Empty;

    public string TokenizerSha256 { get; set; } = string.Empty;

    public string UserAgentProduct { get; set; } = "Recall";

    public string UserAgentVersion { get; set; } = "0.1";

    public string RelativeModelDirectory { get; set; } = "models/text";

    public string RelativeVectorDirectory { get; set; } = "zvec";

    public Uri OnnxBaseUri => new(OnnxBaseUrl);

    public Uri RepositoryBaseUri => new(RepositoryBaseUrl);
}
