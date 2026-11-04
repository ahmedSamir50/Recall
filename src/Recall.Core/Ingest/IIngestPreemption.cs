namespace Recall.Core.Ingest;

public interface IIngestPreemption
{
    void SearchStarted();

    void SearchFinished();

    bool IsSearching { get; }

    CancellationToken Token { get; }
}

public sealed class IngestPreemption : IIngestPreemption, IDisposable
{
    private readonly object sync = new();
    private CancellationTokenSource source = new();
    private int searching;

    public bool IsSearching => Volatile.Read(ref searching) > 0;

    public CancellationToken Token
    {
        get
        {
            lock (sync)
            {
                return source.Token;
            }
        }
    }

    public void SearchStarted()
    {
        lock (sync)
        {
            if (searching == 0)
            {
                if (source.IsCancellationRequested)
                {
                    source.Dispose();
                    source = new CancellationTokenSource();
                }

                source.Cancel();
            }

            searching++;
        }
    }

    public void SearchFinished()
    {
        lock (sync)
        {
            if (searching == 0)
            {
                return;
            }

            searching--;
            if (searching == 0 && source.IsCancellationRequested)
            {
                source.Dispose();
                source = new CancellationTokenSource();
            }
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            source.Dispose();
        }
    }
}
