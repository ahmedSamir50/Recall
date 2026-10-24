using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Maui.Platform;

public sealed partial class MauiGrantedTextReader : IGrantedTextReader
{
    private readonly DirectoryGrantedTextReader files;
    private readonly IGrantedFolderAccessService access;
    private int restored;

    public MauiGrantedTextReader(
        IGrantedFolderAccessService access,
        IUserSkipStore skips,
        ILocalContentProbe probe,
        IRemoteFileSkipReport remoteSkip,
        IDatasetSkipReport datasetSkip)
    {
        this.access = access;
        files = new DirectoryGrantedTextReader(skips, probe, remoteSkip, datasetSkip);
    }

    public IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref restored, 1) == 0)
        {
            access.RestorePersistedAccess();
        }

        if (TryPlatform(grant, cancellationToken, out var documents))
        {
            return documents;
        }

        return files.ReadAsync(grant, cancellationToken);
    }

    public IAsyncEnumerable<string> ListAudioAndVideoAsync(ScopeFolderGrant grant, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref restored, 1) == 0)
        {
            access.RestorePersistedAccess();
        }

        return files.ListAudioAndVideoAsync(grant, cancellationToken);
    }

    private static IAsyncEnumerable<GrantedTextDocument> NoDocuments { get; } = new EmptyDocuments();

    private sealed class EmptyDocuments : IAsyncEnumerable<GrantedTextDocument>
    {
        public IAsyncEnumerator<GrantedTextDocument> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return EmptyEnumerator.Instance;
        }

        private sealed class EmptyEnumerator : IAsyncEnumerator<GrantedTextDocument>
        {
            public static readonly EmptyEnumerator Instance = new();

            public GrantedTextDocument Current { get; } = new(string.Empty, string.Empty, string.Empty, DateTimeOffset.UnixEpoch);

            public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(false);

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private partial bool TryPlatform(
        ScopeFolderGrant grant,
        CancellationToken cancellationToken,
        out IAsyncEnumerable<GrantedTextDocument> documents);
}
