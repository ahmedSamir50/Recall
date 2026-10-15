using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public sealed class IndexNowService : IIndexNowService
{
    private readonly IScopeManagerService scope;
    private readonly IIngestCursorStore cursors;
    private readonly IGrantedTextIngestService ingest;

    public IndexNowService(IScopeManagerService scope, IIngestCursorStore cursors, IGrantedTextIngestService ingest)
    {
        this.scope = scope;
        this.cursors = cursors;
        this.ingest = ingest;
    }

    public IReadOnlyList<ScopeFolderGrant> ListOffers()
    {
        var grants = scope.ListGrants();
        var finished = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var grant in grants)
        {
            if (grant is null || string.IsNullOrWhiteSpace(grant.Path))
            {
                continue;
            }

            var normalized = ScopeGrantPolicy.Normalize(grant.Path);
            if (cursors.IsGrantFinished(grant.Path) || cursors.IsGrantFinished(normalized))
            {
                finished.Add(normalized);
                finished.Add(grant.Path);
            }
        }

        return IndexNowOfferPolicy.ListOffers(grants, finished);
    }

    public async Task IndexAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await ingest.IngestGrantAsync(path, cancellationToken).ConfigureAwait(false);
    }
}
