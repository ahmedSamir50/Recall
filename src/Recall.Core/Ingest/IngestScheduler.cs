namespace Recall.Core.Ingest;

public enum IngestModel
{
    Text = 0,
    Media = 1,
    Ocr = 2,
}

public readonly record struct IngestJob(IngestModel Model, string Path);

public sealed class IngestScheduler
{
    public const int OpenFileThreshold = 20;

    public static readonly TimeSpan OpenIdleDelay = TimeSpan.FromSeconds(30);

    private readonly object sync = new();
    private readonly Queue<IngestJob>[] queues =
    [
        new Queue<IngestJob>(),
        new Queue<IngestJob>(),
        new Queue<IngestJob>(),
    ];

    private readonly DateTimeOffset?[] lastEventAt = [null, null, null];

    public void Enqueue(IngestJob job)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(job.Path);
        lock (sync)
        {
            queues[(int)job.Model].Enqueue(job);
            lastEventAt[(int)job.Model] = DateTimeOffset.UtcNow;
        }
    }

    public bool ShouldOpen(IngestModel model, DateTimeOffset now)
    {
        lock (sync)
        {
            var queue = queues[(int)model];
            if (queue.Count == 0)
            {
                return false;
            }

            if (queue.Count >= OpenFileThreshold)
            {
                return true;
            }

            var last = lastEventAt[(int)model];
            return last.HasValue && now - last.Value >= OpenIdleDelay;
        }
    }

    public int QueueCount(IngestModel model)
    {
        lock (sync)
        {
            return queues[(int)model].Count;
        }
    }

    internal bool TryDequeue(IngestModel model, out IngestJob job)
    {
        lock (sync)
        {
            var queue = queues[(int)model];
            if (queue.Count == 0)
            {
                job = default;
                return false;
            }

            job = queue.Dequeue();
            return true;
        }
    }
}
