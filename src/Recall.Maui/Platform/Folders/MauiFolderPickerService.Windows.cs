#if WINDOWS
using Recall.Core.Ports;
using WinRT.Interop;

namespace Recall.Maui.Platform;

public sealed partial class MauiFolderPickerService
{
    private partial async Task<FolderPickerResult> PickFolderPlatformAsync(CancellationToken cancellationToken)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeFilter.Add("*");

            var window = Application.Current?.Windows.FirstOrDefault();
            if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            {
                var hwnd = WindowNative.GetWindowHandle(nativeWindow);
                InitializeWithWindow.Initialize(picker, hwnd);
            }

            var folder = await picker.PickSingleFolderAsync().AsTask(cancellationToken);
            if (folder is null)
            {
                return new FolderPickerResult(FolderPickerOutcome.Cancelled, null);
            }

            return new FolderPickerResult(FolderPickerOutcome.Selected, folder.Path);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or PermissionException)
        {
            return new FolderPickerResult(FolderPickerOutcome.Denied, null);
        }
    }
}
#endif
