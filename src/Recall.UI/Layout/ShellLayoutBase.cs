using Microsoft.AspNetCore.Components;
using Recall.UI.State;

namespace Recall.UI.Layout;

public abstract class ShellLayoutBase : LayoutComponentBase, IDisposable
{
    private bool disposed;
    private IDisposable? storeSubscription;

    [Inject]
    protected ShellUiStore Ui { get; set; } = null!;

    protected override void OnInitialized()
    {
        storeSubscription = Ui.State.Subscribe(_ => OnStoreChanged());
    }

    protected virtual void OnStoreChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            storeSubscription?.Dispose();
        }

        disposed = true;
    }
}
