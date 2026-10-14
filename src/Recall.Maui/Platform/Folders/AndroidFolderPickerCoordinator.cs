#if ANDROID
using Android.App;
using Android.Content;
using AndroidX.Activity.Result;
using Recall.Core.Ports;

namespace Recall.Maui.Platform;

internal static class AndroidFolderPickerCoordinator
{
    internal const int OpenDocumentTreeRequestCode = 44001;

    private static TaskCompletionSource<FolderPickerResult>? pending;
    private static ActivityResultLauncher? treeLauncher;

    public static void BindLauncher(ActivityResultLauncher launcher)
    {
        treeLauncher = launcher;
    }

    public static Task<FolderPickerResult> PickAsync(CancellationToken cancellationToken)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity is null || treeLauncher is null)
        {
            return Task.FromResult(new FolderPickerResult(FolderPickerOutcome.Denied, null));
        }

        pending = new TaskCompletionSource<FolderPickerResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => pending.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Cancelled, null)));

        activity.RunOnUiThread(() =>
        {
            try
            {
                treeLauncher.Launch(null);
            }
            catch (Exception)
            {
                pending?.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Denied, null));
            }
        });
#pragma warning disable VSTHRD003
        return pending.Task;
#pragma warning restore VSTHRD003
    }

    public static void CompleteUri(Android.Net.Uri? uri)
    {
        if (pending is null)
        {
            return;
        }

        var tcs = pending;
        pending = null;

        if (uri is null)
        {
            tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Cancelled, null));
            return;
        }

        try
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            activity?.ContentResolver?.TakePersistableUriPermission(uri, ActivityFlags.GrantReadUriPermission);
            var path = uri.ToString() ?? string.Empty;
            tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Selected, path, path));
        }
        catch (Exception)
        {
            tcs.TrySetResult(new FolderPickerResult(FolderPickerOutcome.Denied, null));
        }
    }

    public static void Complete(Result resultCode, Intent? data)
    {
        if (resultCode != Result.Ok)
        {
            CompleteUri(null);
            return;
        }

        CompleteUri(data?.Data);
    }
}
#endif
