using Microsoft.AspNetCore.Components;

namespace Recall.UI.State;

public abstract class UiStoreComponentBase : ComponentBase, IDisposable
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
