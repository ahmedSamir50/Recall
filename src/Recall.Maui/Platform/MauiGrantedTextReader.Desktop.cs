#if !ANDROID && !IOS && !MACCATALYST
using Recall.Core.Ingest;
using Recall.Core.Scope;

namespace Recall.Maui.Platform;

public sealed partial class MauiGrantedTextReader
{
    private partial bool TryPlatform(
        ScopeFolderGrant grant,
        CancellationToken cancellationToken,
        out IAsyncEnumerable<GrantedTextDocument> documents)
    {
        _ = grant;
        _ = cancellationToken;
        documents = NoDocuments;
        return false;
    }
}
#endif
