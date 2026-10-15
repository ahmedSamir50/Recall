using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public sealed class ForegroundScanService
{
    private readonly IScopeManagerService scope;
    private readonly IGrantedTextReader reader;
    private readonly IIngestCursorStore cursors;
    private readonly ScopeChangeApplier applier;

    public ForegroundScanService(
        IScopeManagerService scope,
        IGrantedTextReader reader,
        IIngestCursorStore cursors,
        ScopeChangeApplier applier)
    {
        this.scope = scope;
        this.reader = reader;
        this.cursors = cursors;
        this.applier = applier;
    }

    public async Task ScanAsync(CancellationToken cancellationToken = default)
    {
        foreach (var grant in scope.ListGrants())
        {
            if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Text) && !grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
            {
                continue;
            }

            var documents = new List<GrantedTextDocument>();
            await foreach (var document in reader.ReadAsync(grant, cancellationToken).ConfigureAwait(false))
            {
                documents.Add(document);
            }

            var current = documents
                .Select(d => new WatchedFile(d.Path, ContentIdBuilder.HashSourceContent(d.Text)))
                .ToList();
            var stored = cursors.ListFiles(grant.Path);
            var changes = ForegroundGrantDiff.Diff(current, stored);
            var byPath = documents.ToDictionary(d => d.Path, StringComparer.OrdinalIgnoreCase);
            foreach (var change in changes)
            {
                GrantedTextDocument? document = null;
                if (change.Kind == ScopeChangeKind.Created || change.Kind == ScopeChangeKind.Modified || change.Kind == ScopeChangeKind.Renamed)
                {
                    byPath.TryGetValue(change.Path, out document);
                }

                await applier.ApplyAsync(change, grant, document, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
