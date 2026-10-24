using Recall.Core.Ports;

namespace Recall.Maui.Platform.Media;

public sealed class MauiMediaPreview : IMediaPreview
{
    public async Task PlayAsync(string path, double startSeconds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var window = Application.Current is null || Application.Current.Windows.Count == 0 ? null : Application.Current.Windows[0];
        var page = window?.Page;
        if (page is null)
        {
            return;
        }

        await page.Dispatcher.DispatchAsync(
            () => page.Navigation.PushModalAsync(new MediaPreviewPage(path, startSeconds)));
    }
}
