#if ANDROID
using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed partial class MauiFolderPickerService
{
    private partial Task<FolderPickerResult> PickFolderPlatformAsync(CancellationToken cancellationToken) =>
        AndroidFolderPickerCoordinator.PickAsync(cancellationToken);
}
#endif
