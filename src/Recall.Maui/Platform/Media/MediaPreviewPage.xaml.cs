using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace Recall.Maui.Platform.Media;

public partial class MediaPreviewPage : ContentPage
{
    private readonly double startSeconds;

#if IOS || MACCATALYST
    private bool seekAfterReady;
#endif

    public MediaPreviewPage(string path, double startSeconds)
    {
        InitializeComponent();
        this.startSeconds = startSeconds;
        Player.MediaOpened += OnPlayerMediaOpened;
#if IOS || MACCATALYST
        Player.StateChanged += OnPlayerStateChanged;
#endif
        Player.Source = MediaSource.FromFile(path);
    }

    protected override void OnDisappearing()
    {
        Player.MediaOpened -= OnPlayerMediaOpened;
#if IOS || MACCATALYST
        Player.StateChanged -= OnPlayerStateChanged;
#endif
        Player.Stop();
        Player.Source = null;
        Player.Handler?.DisconnectHandler();
        base.OnDisappearing();
    }

    private void OnPlayerMediaOpened(object? sender, EventArgs args)
    {
#if IOS || MACCATALYST
        seekAfterReady = true;
#else
        SeekAndPlayOnUiThread();
#endif
    }

#if IOS || MACCATALYST
    private void OnPlayerStateChanged(object? sender, MediaStateChangedEventArgs args)
    {
        if (!seekAfterReady)
        {
            return;
        }

        if (args.NewState is not (MediaElementState.Stopped or MediaElementState.Paused))
        {
            return;
        }

        seekAfterReady = false;
        SeekAndPlayOnUiThread();
    }
#endif

    private void SeekAndPlayOnUiThread()
    {
        _ = Dispatcher.DispatchAsync(async () =>
        {
            try
            {
                await Player.SeekTo(TimeSpan.FromSeconds(startSeconds), CancellationToken.None);
                Player.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        });
    }
}
