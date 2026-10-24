using Recall.Adapters.Embedding.Llama;
using Recall.Core.Download;
using Recall.Core.Faults;
using Recall.Core.Ports;

namespace Recall.Adapters.Embedding;

public sealed class TextModelRuntime : IDisposable
{
    private readonly string modelDirectory;
    private readonly TextModelDownloadOptions options;
    private readonly IRecallEmbedLibrary library;
    private readonly object loadGate = new();
    private LlamaTextEmbeddingEncoder? encoder;
    private LlamaTextTokenCounter? counter;
    private bool opened;
    private int disposed;

    public TextModelRuntime(
        string modelDirectory,
        RecallShellKind shellKind = RecallShellKind.Unknown,
        TextModelDownloadOptions? options = null)
        : this(modelDirectory, shellKind, options, null)
    {
    }

    internal TextModelRuntime(
        string modelDirectory,
        IRecallEmbedLibrary library,
        TextModelDownloadOptions? options = null)
        : this(modelDirectory, RecallShellKind.Unknown, options, library)
    {
    }

    private TextModelRuntime(
        string modelDirectory,
        RecallShellKind shellKind,
        TextModelDownloadOptions? options,
        IRecallEmbedLibrary? library)
    {
        this.modelDirectory = modelDirectory;
        _ = shellKind;
        this.options = options ?? new TextModelDownloadOptions();
        this.library = library ?? new RecallEmbedNative();
    }

    public bool IsModelReady => File.Exists(Path.Combine(modelDirectory, options.ModelFileName));

    public LlamaTextEmbeddingEncoder GetEncoder()
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        LlamaNativeGate.Enter();
        try
        {
            EnsureLoaded();
            return encoder!;
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    public LlamaTextTokenCounter GetCounter()
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        LlamaNativeGate.Enter();
        try
        {
            EnsureLoaded();
            return counter!;
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
            encoder?.Dispose();
            encoder = null;
            counter = null;
            if (opened)
            {
                library.Close();
                opened = false;
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
            EnsureLoaded();
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
            encoder?.Dispose();
            encoder = null;
            counter = null;
            if (opened)
            {
                library.Close();
                opened = false;
            }
        }
        finally
        {
            LlamaNativeGate.Exit();
        }
    }

    private void EnsureLoaded()
    {
        if (encoder is not null)
        {
            return;
        }

        lock (loadGate)
        {
            if (encoder is not null)
            {
                return;
            }

            if (!IsModelReady)
            {
                throw new RecallFaultException(RecallFault.TextModelNotReady);
            }

            if (!opened)
            {
                var modelPath = Path.Combine(modelDirectory, options.ModelFileName);
                var code = library.Open(modelPath);
                if (code != 0)
                {
                    throw new InvalidOperationException($"recall_embed open failed with code {code}.");
                }

                opened = true;
            }

            encoder = new LlamaTextEmbeddingEncoder(library);
            counter = new LlamaTextTokenCounter(library);
        }
    }
}
