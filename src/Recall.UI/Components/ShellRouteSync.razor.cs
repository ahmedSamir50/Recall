using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Recall.UI.Components;

public sealed partial class ShellRouteSync
{
    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Ui.SetRoute(new Uri(Nav.Uri).AbsolutePath);
        Nav.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        Ui.SetRoute(new Uri(e.Location).AbsolutePath);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Nav.LocationChanged -= OnLocationChanged;
        }

        base.Dispose(disposing);
    }
}
