using Recall.Core.Ports;
using Recall.Maui.Platform.Files;

namespace Recall.Maui;

public partial class MainPage : ContentPage
{
    private bool schemeRegistered;

	public MainPage()
	{
		InitializeComponent();
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();
		if (Handler is null)
		{
			return;
		}

        var services = Handler.MauiContext?.Services;
		services?.GetService<IGrantedFolderAccessService>()?.RestorePersistedAccess();
        if (!schemeRegistered && services is not null)
        {
            schemeRegistered = true;
            GrantFileSchemeRegistration.Register(services, blazorWebView);
        }
	}
}
