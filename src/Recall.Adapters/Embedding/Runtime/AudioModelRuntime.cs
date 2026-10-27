using Recall.Adapters.Embedding.Llama;
using Recall.Core.Download;
using Recall.Core.Ingest;
using Recall.Core.Ports;

namespace Recall.Adapters.Embedding;

public sealed class AudioModelRuntime : IAudioEmbeddingEncoder, IDisposable
{
    public const string OpenAudioFailedFormat = "recall_embed audio open failed with code {0}.";

    public const string EmbedAudioFailedFormat = "recall_embed audio embed failed with code {0}.";

    private readonly string modelDirectory;
    private readonly AudioModelDownloadOptions options;
    private readonly IRecallEmbedLibrary library;
    private readonly TextModelRuntime? textRuntime;
    private bool opened;
    private bool ownsVisionContext;
    private int disposed;

    public AudioModelRuntime(
        string modelDirectory,
        TextModelRuntime textRuntime,
        AudioModelDownloadOptions? options = null)
        : this(modelDirectory, options, null, textRuntime)
    {
    }

    internal AudioModelRuntime(
        string modelDirectory,
        IRecallEmbedLibrary library,
        AudioModelDownloadOptions? options = null)
        : this(modelDirectory, options, library, null)
    {
    }

    private AudioModelRuntime(
        string modelDirectory,
        AudioModelDownloadOptions? options,
        IRecallEmbedLibrary? library,
        TextModelRuntime? textRuntime)
    {
        this.modelDirectory = modelDirectory;
        this.options = options ?? new AudioModelDownloadOptions();
        this.library = library ?? new RecallEmbedNative();
        this.textRuntime = textRuntime;
    }

    public bool IsModelReady => File.Exists(Path.Combine(modelDirectory, options.ModelFileName));

    public bool IsLoaded { get; private set; }

    public void EmbedPcm(ReadOnlyMemory<float> mono16k, float[] destination768)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        ArgumentNullException.ThrowIfNull(destination768);
        LlamaNativeGate.Enter();
        try
        {
            EnsureAudioOpen();
            IsLoaded = true;
            var embedCode = library.EmbedAudio(mono16k.Span.ToArray(), destination768);
            if (embedCode != 0)
            {
                throw new InvalidOperationException(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    EmbedAudioFailedFormat,
                    embedCode));
            }
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    public void OpenSession()
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        LlamaNativeGate.Enter();
        try
        {
            EnsureAudioOpen();
            IsLoaded = true;
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    public void CloseAudio()
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        LlamaNativeGate.Enter();
        try
        {
            CloseOpenedContext();
            IsLoaded = false;
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    public void Dispose()
    {
        if (disposed != 0)
        {
            return;
        }

        disposed = 1;
        LlamaNativeGate.Enter();
        try
        {
            CloseOpenedContext();
            IsLoaded = false;
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    private void EnsureAudioOpen()
    {
        if (opened)
        {
            return;
        }

        textRuntime?.GetEncoder();
        var modelPath = Path.Combine(modelDirectory, options.ModelFileName);
        if (MediaContextPolicy.OneContext)
        {
            var visionCode = library.OpenVision(modelPath);
            if (visionCode == 0)
            {
                ownsVisionContext = true;
                opened = true;
                return;
            }

            if (visionCode == 8)
            {
                opened = true;
                return;
            }

            throw new InvalidOperationException(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                OpenAudioFailedFormat,
                visionCode));
        }

        var code = library.OpenAudio(modelPath);
        if (code != 0)
        {
            throw new InvalidOperationException(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                OpenAudioFailedFormat,
                code));
        }

        opened = true;
    }

    private void CloseOpenedContext()
    {
        if (!opened)
        {
            return;
        }

        if (MediaContextPolicy.OneContext)
        {
            if (ownsVisionContext)
            {
                library.CloseVision();
            }
        }
        else
        {
            library.CloseAudio();
        }

        opened = false;
        ownsVisionContext = false;
    }
}
