namespace Recall.Maui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) { Title = "Recall" };
		var services = IPlatformApplication.Current?.Services;
		if (services is null)
		{
			return window;
		}

		var coordinator = services.GetService(typeof(Recall.Core.Ingest.ScopeWatchCoordinator)) as Recall.Core.Ingest.ScopeWatchCoordinator;
		var shell = services.GetService(typeof(Recall.Core.Ports.IRecallShellKind)) as Recall.Core.Ports.IRecallShellKind;
		if (coordinator is null || shell is null)
		{
			return window;
		}

		window.Created += OnWindowCreated;
		window.Resumed += OnWindowResumed;
		window.Destroying += OnWindowDestroying;
		return window;

		void OnWindowCreated(object? sender, EventArgs args)
		{
			coordinator.OnCreated(shell.Kind);
		}

		void OnWindowResumed(object? sender, EventArgs args)
		{
			coordinator.OnResumed(shell.Kind);
		}

		void OnWindowDestroying(object? sender, EventArgs args)
		{
			coordinator.Stop();
		}
	}
}
