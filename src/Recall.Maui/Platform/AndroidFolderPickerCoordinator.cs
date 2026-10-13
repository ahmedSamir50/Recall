#if ANDROID
using Android.App;
using Android.Content;
using Recall.Core.Ports;

namespace Recall.Maui.Platform;

internal static class AndroidFolderPickerCoordinator
{
    internal const int OpenDocumentTreeRequestCode = 44001;

    private static TaskCompletionSource<FolderPickerResult>? pending;

    public static Task<FolderPickerResult> PickAsync(CancellationToken cancellationToken)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity is null)
        {
            return Task.FromResult(new FolderPickerResult(FolderPickerOutcome.Denied, null));
        }

        pending = new TaskCompletionSource<FolderPickerResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => pending.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Cancelled, null)));

        var intent = new Intent(Intent.ActionOpenDocumentTree);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        intent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

        activity.StartActivityForResult(intent, OpenDocumentTreeRequestCode);
#pragma warning disable VSTHRD003
        return pending.Task;
#pragma warning restore VSTHRD003
    }

    public static void Complete(Result resultCode, Intent? data)
    {
        if (pending is null)
        {
            return;
        }

        var tcs = pending;
        pending = null;

        if (resultCode != Result.Ok || data?.Data is null)
        {
            tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Cancelled, null));
            return;
        }

        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var uri = data.Data;
        try
        {
            activity?.ContentResolver?.TakePersistableUriPermission(
                uri,
                ActivityFlags.GrantReadUriPermission);
        }
        catch (Exception)
        {
            tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Denied, null));
            return;
        }

        var path = uri.ToString() ?? string.Empty;
        tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Selected, path, path));
    }
}
#endif
