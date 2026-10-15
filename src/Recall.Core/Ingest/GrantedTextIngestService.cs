using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public sealed class GrantedTextIngestService : IGrantedTextIngestService
{
    private readonly IScopeManagerService scope;
    private readonly IGrantedTextReader reader;
    private readonly TextIngestEngine engine;
    private readonly IIngestCursorStore cursors;

    public GrantedTextIngestService(IScopeManagerService scope, IGrantedTextReader reader, TextIngestEngine engine, IIngestCursorStore cursors)
    {
        this.scope = scope;
        this.reader = reader;
        this.engine = engine;
        this.cursors = cursors;
    }

    public async Task IngestGrantedTextAsync(CancellationToken cancellationToken = default)
    {
        foreach (var grant in scope.ListGrants())
        {
            await IngestGrantAsync(grant.Path, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task IngestGrantAsync(string path, CancellationToken cancellationToken = default)
    {
        var grant = scope.ListGrants()
            .FirstOrDefault(g => ScopeGrantPolicy.Normalize(g.Path).Equals(ScopeGrantPolicy.Normalize(path), StringComparison.OrdinalIgnoreCase));
        if (grant is null)
        {
            return;
        }

        if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Text) && !grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
        {
            return;
        }

        cursors.MarkGrantReading(grant.Path);
        await foreach (var document in reader.ReadAsync(grant, cancellationToken).ConfigureAwait(false))
        {
            await IngestDocumentAsync(grant, document, cancellationToken).ConfigureAwait(false);
        }

        cursors.MarkGrantFinished(grant.Path);
    }

    public async Task IngestDocumentAsync(ScopeFolderGrant grant, GrantedTextDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        ArgumentNullException.ThrowIfNull(document);
        var contentHash = ContentIdBuilder.HashSourceContent(document.Text);
        var row = cursors.GetFile(document.Path);
        if (IngestCursorPolicy.Decide(row, contentHash) == IngestCursorDecision.Skip)
        {
            return;
        }

        cursors.MarkReading(document.Path, contentHash, grant.Path);
        var result = await engine.IngestFileAsync(document.Path, document.Text, document.ModifiedAt, cancellationToken).ConfigureAwait(false);
        cursors.MarkFinished(
            document.Path,
            result.ContentHash,
            grant.Path,
            result.Chunks.Select(c => (c.ContentId, c.Ordinal)).ToList());
    }
}
