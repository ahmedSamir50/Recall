#if ANDROID
using Android.App;
using Android.Content;
using Recall.Maui.Platform;

namespace Recall.Maui;

public partial class MainActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == AndroidFolderPickerCoordinator.OpenDocumentTreeRequestCode)
        {
            AndroidFolderPickerCoordinator.Complete(resultCode, data);
        }
    }
}
#endif
