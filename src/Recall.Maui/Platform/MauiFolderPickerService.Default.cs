#if !WINDOWS && !ANDROID && !IOS && !MACCATALYST
using Recall.Core.Ports;

namespace Recall.Maui.Platform;

public sealed partial class MauiFolderPickerService
{
    private partial Task<FolderPickerResult> PickFolderPlatformAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new FolderPickerResult(FolderPickerOutcome.Cancelled, null));
}
#endif
