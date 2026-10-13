using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed partial class MauiFolderPickerService : IFolderPickerService
{
    public Task<FolderPickerResult> PickFolderAsync(CancellationToken cancellationToken = default) =>
        PickFolderPlatformAsync(cancellationToken);

    private partial Task<FolderPickerResult> PickFolderPlatformAsync(CancellationToken cancellationToken);
}
