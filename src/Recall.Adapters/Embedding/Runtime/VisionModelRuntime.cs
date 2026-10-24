using Recall.Adapters.Embedding.Llama;
using Recall.Core.Download;
using Recall.Core.Ports;

namespace Recall.Adapters.Embedding;

public sealed class VisionModelRuntime : IVisionEmbeddingEncoder, IDisposable
{
    private readonly string modelDirectory;
    private readonly VisionModelDownloadOptions options;
    private readonly IRecallEmbedLibrary library;
    private readonly TextModelRuntime? textRuntime;
    private bool opened;
    private int disposed;

    public VisionModelRuntime(
        string modelDirectory,
        TextModelRuntime textRuntime,
        VisionModelDownloadOptions? options = null)
        : this(modelDirectory, options, null, textRuntime)
    {
    }

    internal VisionModelRuntime(
        string modelDirectory,
        IRecallEmbedLibrary library,
        VisionModelDownloadOptions? options = null)
        : this(modelDirectory, options, library, null)
    {
    }

    private VisionModelRuntime(
        string modelDirectory,
        VisionModelDownloadOptions? options,
        IRecallEmbedLibrary? library,
        TextModelRuntime? textRuntime)
    {
        this.modelDirectory = modelDirectory;
        this.options = options ?? new VisionModelDownloadOptions();
        this.library = library ?? new RecallEmbedNative();
        this.textRuntime = textRuntime;
    }

    public bool IsLoaded { get; private set; }

    public bool IsModelReady => File.Exists(Path.Combine(modelDirectory, options.ModelFileName));

    public void EmbedJpeg(ReadOnlyMemory<byte> jpeg, float[] destination768)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        ArgumentNullException.ThrowIfNull(destination768);
        LlamaNativeGate.Enter();
        var openedHere = false;
        try
        {
            if (!opened)
            {
                OpenVisionCore();
                openedHere = true;
            }

            IsLoaded = true;
            library.EmbedVision(jpeg.ToArray(), destination768);
        }
        finally
        {
            if (openedHere && opened)
            {
                library.CloseVision();
                opened = false;
                IsLoaded = false;
            }

            LlamaNativeGate.Exit();
        }
    }

    public void OpenSession()
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        LlamaNativeGate.Enter();
        try
        {
            if (!opened)
            {
                OpenVisionCore();
            }

            IsLoaded = true;
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    public void CloseSession()
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        LlamaNativeGate.Enter();
        try
        {
            if (opened)
            {
                library.CloseVision();
                opened = false;
            }

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
            if (opened)
            {
                library.CloseVision();
                opened = false;
            }

            IsLoaded = false;
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    private void OpenVisionCore()
    {
        textRuntime?.GetEncoder();
        var modelPath = Path.Combine(modelDirectory, options.ModelFileName);
        var code = library.OpenVision(modelPath);
        if (code != 0)
        {
            throw new InvalidOperationException($"recall_embed vision open failed with code {code}.");
        }

        opened = true;
    }
}
