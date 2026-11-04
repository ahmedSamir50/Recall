namespace Recall.Core.Ingest;

public readonly record struct IngestPassMetric(IngestModel Model, int Jobs, long OpenMs, long TotalMs);

public sealed class IngestPassMetrics
{
    public const int MetricRingSize = 64;

    private readonly object sync = new();
    private readonly IngestPassMetric[] ring = new IngestPassMetric[MetricRingSize];
    private int next;
    private int count;

    public void Record(IngestPassMetric metric)
    {
        lock (sync)
        {
            ring[next] = metric;
            next = (next + 1) % MetricRingSize;
            if (count < MetricRingSize)
            {
                count++;
            }
        }
    }

    public IReadOnlyList<IngestPassMetric> Snapshot()
    {
        lock (sync)
        {
            var result = new List<IngestPassMetric>(count);
            var start = count == MetricRingSize ? next : 0;
            for (var index = 0; index < count; index++)
            {
                result.Add(ring[(start + index) % MetricRingSize]);
            }

            return result;
        }
    }

    public void Clear()
    {
        lock (sync)
        {
            next = 0;
            count = 0;
        }
    }
}
