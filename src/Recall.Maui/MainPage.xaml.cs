using Recall.Core.Ports;

namespace Recall.Maui;

public partial class MainPage : ContentPage
{
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

		Handler.MauiContext?.Services.GetService<IGrantedFolderAccessService>()?.RestorePersistedAccess();
	}
}
