using Recall.Core.Ports;

namespace Recall.Core.Download;

public static class TextModelReadyIngest
{
    public static async Task ReadGrantsAsync(
        TextModelDownloadPhase phase,
        IGrantedTextIngestService ingest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ingest);
        if (phase != TextModelDownloadPhase.Ready)
        {
            return;
        }

        await ingest.IngestGrantedTextAsync(cancellationToken).ConfigureAwait(false);
    }
}
