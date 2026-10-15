#if ANDROID
using Android.Content;

namespace Recall.Maui.Platform.Files;

public sealed partial class MauiLocalFileOpener
{
    private partial bool ContentExists(string sourcePath)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var resolver = activity?.ContentResolver;
        if (activity is null || resolver is null)
        {
            return false;
        }

        try
        {
            var uri = Android.Net.Uri.Parse(sourcePath);
            if (uri is null)
            {
                return false;
            }

            using var stream = resolver.OpenInputStream(uri);
            return stream is not null;
        }
        catch
        {
            return false;
        }
    }

    private partial bool TryOpenContent(string sourcePath)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity is null)
        {
            return false;
        }

        try
        {
            var uri = Android.Net.Uri.Parse(sourcePath);
            if (uri is null)
            {
                return false;
            }

            var intent = new Intent(Intent.ActionView);
            intent.SetData(uri);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            activity.StartActivity(intent);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
#endif
