using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiLocalFileOpener(IMediaPreview preview) : ILocalFileOpener
{
    public bool Exists(string sourcePath)
    {
        var probe = LocalFileOpenPolicy.Classify(sourcePath);
        return LocalFileOpenPolicy.Exists(probe, File.Exists(sourcePath), ContentExists(sourcePath));
    }

    public async Task<LocalFileOpenResult> OpenAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        var probe = LocalFileOpenPolicy.Classify(sourcePath);
        if (probe == LocalFileProbe.Empty)
        {
            return LocalFileOpenPolicy.Decide(probe, filesystemExists: false, launcherOpened: false, platformAccepted: false, threw: false);
        }

        if (probe == LocalFileProbe.ContentUri)
        {
            try
            {
                var accepted = TryOpenContent(sourcePath);
                return LocalFileOpenPolicy.Decide(probe, filesystemExists: false, launcherOpened: false, platformAccepted: accepted, threw: false);
            }
            catch
            {
                return LocalFileOpenPolicy.Decide(probe, filesystemExists: false, launcherOpened: false, platformAccepted: false, threw: true);
            }
        }

        try
        {
            var exists = File.Exists(sourcePath);
            var opened = false;
            if (exists)
            {
                opened = await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(sourcePath) }).ConfigureAwait(false);
            }

            return LocalFileOpenPolicy.Decide(probe, filesystemExists: exists, launcherOpened: opened, platformAccepted: false, threw: false);
        }
        catch
        {
            return LocalFileOpenPolicy.Decide(probe, filesystemExists: false, launcherOpened: false, platformAccepted: false, threw: true);
        }
    }

    public async Task<LocalFileOpenResult> OpenAtAsync(string sourcePath, double startSeconds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);
        if (!File.Exists(sourcePath))
        {
            return LocalFileOpenResult.Missing;
        }

        if ((MediaFileKinds.IsAudio(sourcePath) || MediaFileKinds.IsVideo(sourcePath))
            && MediaHitStart.Seeks(startSeconds))
        {
            await preview.PlayAsync(sourcePath, startSeconds, cancellationToken).ConfigureAwait(false);
            return LocalFileOpenResult.Opened;
        }

        return await OpenAsync(sourcePath, cancellationToken).ConfigureAwait(false);
    }

    private partial bool TryOpenContent(string sourcePath);

    private partial bool ContentExists(string sourcePath);
}
