using Recall.Core.Checkpoints;
using Recall.Core.Runtime;
using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public sealed class IngestStatusService
{
    private readonly IIngestCursorStore store;
    private readonly IMemoryBudget memory;
    private readonly IngestPassMetrics metrics;

    public IngestStatusService(IIngestCursorStore store, IMemoryBudget memory, IngestPassMetrics? metrics = null)
    {
        this.store = store;
        this.memory = memory;
        this.metrics = metrics ?? new IngestPassMetrics();
    }

    public DeviceVerdict Verdict => memory.Verdict;

    public IReadOnlyList<IngestPassMetric> Metrics => metrics.Snapshot();

    public IReadOnlyList<IngestKindCount> List(GrantFileKinds enabled) => store.CountEnabled(enabled);

    public IReadOnlyList<string> ListUnfinished(IReadOnlyList<ScopeFolderGrant> grants)
    {
        var paths = new List<string>();
        foreach (var grant in grants)
        {
            paths.AddRange(store.ListUnfinishedPaths(grant.Path));
        }

        return paths;
    }

    public IReadOnlyList<IngestMissingNote> ListMissingNotes(IReadOnlyList<ScopeFolderGrant> grants)
    {
        var notes = new List<IngestMissingNote>();
        foreach (var grant in grants)
        {
            foreach (var row in store.ListFiles(grant.Path))
            {
                if (row.State == IngestCursorState.Missing && !string.IsNullOrEmpty(row.MissingStatusKey))
                {
                    notes.Add(new IngestMissingNote(row.Path, row.MissingStatusKey));
                }
            }
        }

        return notes;
    }
}

public readonly record struct IngestMissingNote(string Path, string StatusKey);
