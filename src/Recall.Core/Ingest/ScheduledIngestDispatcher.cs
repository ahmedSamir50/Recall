using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.Core.Ingest;

public sealed class ScheduledIngestDispatcher
{
    private readonly IScopeManagerService scope;
    private readonly IGrantedTextIngestService ingest;
    private readonly IAudioFileIngest? audio;
    private readonly IVisionFileIngest? vision;
    private readonly IUserSkipStore skips;
    private readonly ILocalContentProbe contentProbe;
    private readonly IRemoteFileSkipReport remoteSkip;

    public ScheduledIngestDispatcher(
        IScopeManagerService scope,
        IGrantedTextIngestService ingest,
        IUserSkipStore skips,
        ILocalContentProbe contentProbe,
        IRemoteFileSkipReport remoteSkip,
        IAudioFileIngest? audio = null,
        IVisionFileIngest? vision = null)
    {
        this.scope = scope;
        this.ingest = ingest;
        this.skips = skips;
        this.contentProbe = contentProbe;
        this.remoteSkip = remoteSkip;
        this.audio = audio;
        this.vision = vision;
    }

    public IngestMediaLeg Leg { get; private set; }

    public void SetLeg(IngestMediaLeg leg) => Leg = leg;

    public async Task DispatchTextAsync(string path, CancellationToken cancellationToken)
    {
        var grant = FindGrant(path);
        if (grant is null)
        {
            return;
        }

        var document = await FilesystemScopeFileReader.TryReadAsync(
            grant,
            path,
            skips,
            cancellationToken,
            contentProbe,
            remoteSkip).ConfigureAwait(false);
        if (document is null)
        {
            return;
        }

        await ingest.IngestDocumentAsync(grant, document, cancellationToken).ConfigureAwait(false);
    }

    public async Task DispatchMediaAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var grant = FindGrant(path);
        if (grant is null)
        {
            return;
        }

        var fileName = Path.GetFileName(path);
        if (MediaIngestFilter.UserSkipped(fileName, skips.Load()))
        {
            return;
        }

        if (MediaFileKinds.IsImage(path))
        {
            if (vision is null || !MediaIngestFilter.KindAllowed(grant.EnabledKinds, fileName))
            {
                return;
            }

            await vision.IndexImageAsync(grant, path, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (MediaFileKinds.IsAudio(path))
        {
            if (audio is null)
            {
                return;
            }

            await audio.IndexAsync(grant, path, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!MediaFileKinds.IsVideo(path) || !MediaIngestFilter.KindAllowed(grant.EnabledKinds, fileName))
        {
            return;
        }

        if (vision is not null && Leg is IngestMediaLeg.Shared or IngestMediaLeg.Vision)
        {
            await vision.IndexVideoFramesAsync(grant, path, cancellationToken).ConfigureAwait(false);
        }

        if (audio is not null && Leg is IngestMediaLeg.Shared or IngestMediaLeg.Audio or IngestMediaLeg.None)
        {
            await audio.IndexAsync(grant, path, cancellationToken).ConfigureAwait(false);
        }
    }

    private ScopeFolderGrant? FindGrant(string path)
    {
        foreach (var grant in scope.ListGrants())
        {
            if (grant is null || string.IsNullOrWhiteSpace(grant.Path))
            {
                continue;
            }

            if (ContentUri.IsContent(grant.Path))
            {
                if (string.Equals(grant.Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    return grant;
                }

                continue;
            }

            if (ScopeGrantPolicy.IsInsideGrant(grant.Path, path))
            {
                return grant;
            }
        }

        return null;
    }
}
