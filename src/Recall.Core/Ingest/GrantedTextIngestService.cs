using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Text;

namespace Recall.Core.Ingest;

public sealed class GrantedTextIngestService : IGrantedTextIngestService
{
    private readonly IScopeManagerService scope;
    private readonly IGrantedTextReader reader;
    private readonly TextIngestEngine engine;

    public GrantedTextIngestService(IScopeManagerService scope, IGrantedTextReader reader, TextIngestEngine engine)
    {
        this.scope = scope;
        this.reader = reader;
        this.engine = engine;
    }

    public async Task IngestGrantedTextAsync(CancellationToken cancellationToken = default)
    {
        foreach (var grant in scope.ListGrants())
        {
            if (!grant.EnabledKinds.HasFlag(GrantFileKinds.Text) && !grant.EnabledKinds.HasFlag(GrantFileKinds.Code))
            {
                continue;
            }

            await foreach (var document in reader.ReadAsync(grant, cancellationToken).ConfigureAwait(false))
            {
                await engine.IngestFileAsync(document.Path, document.Text, document.ModifiedAt, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
