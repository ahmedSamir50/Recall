#if IOS || MACCATALYST
#pragma warning disable CA1422
using Foundation;
using Recall.Core.Ports;
using UIKit;

namespace Recall.Maui.Platform;

public sealed partial class MauiFolderPickerService
{
    private partial Task<FolderPickerResult> PickFolderPlatformAsync(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<FolderPickerResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Cancelled, null)));

        var controller = new UIDocumentPickerViewController(
            ["public.folder"],
            UIDocumentPickerMode.Open)
        {
            AllowsMultipleSelection = false,
        };

        controller.Delegate = new FolderPickerDelegate(tcs);
        var root = UIApplication.SharedApplication.KeyWindow?.RootViewController;
        while (root?.PresentedViewController is not null)
        {
            root = root.PresentedViewController;
        }

        if (root is null)
        {
            tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Denied, null));
            return tcs.Task;
        }

        root.PresentViewController(controller, true, null);
        return tcs.Task;
    }

    private sealed class FolderPickerDelegate : UIDocumentPickerDelegate
    {
        private readonly TaskCompletionSource<FolderPickerResult> tcs;

        public FolderPickerDelegate(TaskCompletionSource<FolderPickerResult> tcs) => this.tcs = tcs;

        public override void WasCancelled(UIDocumentPickerViewController controller) =>
            tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Cancelled, null));

        public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
        {
            if (!url.StartAccessingSecurityScopedResource())
            {
                tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Denied, null));
                return;
            }

            NSError? error;
            var bookmark = url.CreateBookmarkData(
                NSUrlBookmarkCreationOptions.MinimalBookmark,
                null,
                null,
                out error);
            if (bookmark is null)
            {
                url.StopAccessingSecurityScopedResource();
                tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Denied, null));
                return;
            }

            var token = Convert.ToBase64String(bookmark.ToArray());
            var path = url.Path ?? url.AbsoluteString ?? string.Empty;
            tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Selected, path, token));
        }
    }
}
#pragma warning restore CA1422
#endif
