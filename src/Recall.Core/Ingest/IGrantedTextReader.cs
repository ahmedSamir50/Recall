using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public interface IGrantedTextReader
{
    IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, CancellationToken cancellationToken);
}
