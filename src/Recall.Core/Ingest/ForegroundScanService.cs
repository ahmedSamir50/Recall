using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Runtime;
using Recall.Core.Scope;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public sealed class ForegroundScanService
{
    private readonly IScopeManagerService scope;
    private readonly IGrantedTextReader reader;
    private readonly IIngestCursorStore cursors;
    private readonly ScopeChangeApplier applier;
    private readonly IMemoryBudget memory;
    private readonly MemoryGate gate;

    public ForegroundScanService(
        IScopeManagerService scope,
        IGrantedTextReader reader,
        IIngestCursorStore cursors,
        ScopeChangeApplier applier,
        IMemoryBudget memory,
        MemoryGate gate)
    {
        this.scope = scope;
        this.reader = reader;
        this.cursors = cursors;
        this.applier = applier;
        this.memory = memory;
        this.gate = gate;
    }

    public async Task ScanAsync(CancellationToken cancellationToken = default)
    {
        foreach (var grant in scope.ListGrants())
        {
            if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Text) && !grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
            {
                continue;
            }

            if (!ObserveGate())
            {
                return;
            }

            var current = new List<WatchedFile>();
            await foreach (var document in reader.ReadAsync(grant, cancellationToken).ConfigureAwait(false))
            {
                current.Add(new WatchedFile(document.Path, ContentIdBuilder.HashSourceContent(document.Text)));
            }

            var stored = cursors.ListFiles(grant.Path);
            var changes = ForegroundGrantDiff.Diff(current, stored);
            if (changes.Count == 0)
            {
                continue;
            }

            var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var change in changes)
            {
                if (change.Kind != ScopeChangeKind.Deleted)
                {
                    wanted.Add(change.Path);
                }
            }

            var documents = new Dictionary<string, GrantedTextDocument>(StringComparer.OrdinalIgnoreCase);
            if (wanted.Count > 0)
            {
                await foreach (var document in reader.ReadAsync(grant, cancellationToken).ConfigureAwait(false))
                {
                    if (wanted.Contains(document.Path))
                    {
                        documents[document.Path] = document;
                    }
                }
            }

            foreach (var change in changes)
            {
                if (!ObserveGate())
                {
                    return;
                }

                documents.TryGetValue(change.Path, out var document);
                await applier.ApplyAsync(change, grant, document, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private bool ObserveGate()
    {
        if (memory.Verdict == DeviceVerdict.BelowMinimum)
        {
            gate.Block();
            return false;
        }

        gate.Observe(
            memory.Pressure,
            memory.CapacityBytes,
            memory.WorkingSetBytes,
            memory.EffectiveFloorBytes,
            DateTimeOffset.UtcNow);
        return gate.IsOpen();
    }
}
