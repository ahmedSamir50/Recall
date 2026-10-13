using Recall.Core.Download;
using Recall.Core.Faults;
using Recall.Core.Ports;

namespace Recall.Adapters.Embedding;

public sealed class TextModelRuntime : IDisposable
{
    private readonly string modelDirectory;
    private readonly RecallShellKind shellKind;
    private readonly TextModelDownloadOptions options;
    private readonly object gate = new();
    private OnnxTextEmbeddingEncoder? encoder;
    private HuggingFaceTextTokenCounter? counter;
    private Tokenizers.DotNet.Tokenizer? tokenizer;

    public TextModelRuntime(
        string modelDirectory,
        RecallShellKind shellKind = RecallShellKind.Unknown,
        TextModelDownloadOptions? options = null)
    {
        this.modelDirectory = modelDirectory;
        this.shellKind = shellKind;
        this.options = options ?? new TextModelDownloadOptions();
    }

    public bool IsModelReady =>
        File.Exists(Path.Combine(modelDirectory, options.OnnxFileName))
        && File.Exists(Path.Combine(modelDirectory, options.TokenizerFileName))
        && File.Exists(Path.Combine(modelDirectory, options.OnnxDataFileName));

    public OnnxTextEmbeddingEncoder GetEncoder()
    {
        EnsureLoaded();
        return encoder!;
    }

    public HuggingFaceTextTokenCounter GetCounter()
    {
        EnsureLoaded();
        return counter!;
    }

    public void Dispose()
    {
        encoder?.Dispose();
        counter?.Dispose();
        tokenizer?.Dispose();
        encoder = null;
        counter = null;
        tokenizer = null;
    }

    private void EnsureLoaded()
    {
        if (encoder is not null)
        {
            return;
        }

        lock (gate)
        {
            if (encoder is not null)
            {
                return;
            }

            if (!IsModelReady)
            {
                throw new RecallFaultException(RecallFault.TextModelNotReady);
            }

            if (!TextTokenizerRuntime.IsSupported(shellKind))
            {
                throw new RecallFaultException(RecallFault.TokenizerNativeUnsupported);
            }

            tokenizer = HuggingFaceTokenizerFactory.Create(Path.Combine(modelDirectory, options.TokenizerFileName));
            encoder = new OnnxTextEmbeddingEncoder(Path.Combine(modelDirectory, options.OnnxFileName), tokenizer, ownsTokenizer: false);
            counter = new HuggingFaceTextTokenCounter(tokenizer, ownsTokenizer: false);
        }
    }
}
