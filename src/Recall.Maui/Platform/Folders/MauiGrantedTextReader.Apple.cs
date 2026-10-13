#if IOS || MACCATALYST
using Foundation;
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
        if (string.IsNullOrWhiteSpace(grant.ReopenToken))
        {
            documents = NoDocuments;
            return false;
        }

        documents = ReadBookmarkAsync(grant, cancellationToken);
        return true;
    }

    private async IAsyncEnumerable<GrantedTextDocument> ReadBookmarkAsync(
        ScopeFolderGrant grant,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        NSUrl? url = null;
        var started = false;
        try
        {
            var bytes = Convert.FromBase64String(grant.ReopenToken!);
            url = NSUrl.FromBookmarkData(NSData.FromArray(bytes), default, null, out _, out _);
            started = url?.StartAccessingSecurityScopedResource() ?? false;
            if (url?.Path is not string path || path.Length == 0)
            {
                yield break;
            }

            var scoped = grant with { Path = path };
            await foreach (var document in files.ReadAsync(scoped, cancellationToken).ConfigureAwait(false))
            {
                yield return document;
            }
        }
        finally
        {
            if (started)
            {
                url?.StopAccessingSecurityScopedResource();
            }
        }
    }
}
#endif
