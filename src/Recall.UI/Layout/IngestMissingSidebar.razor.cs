using Microsoft.AspNetCore.Components;
using Recall.Core.Ingest;

namespace Recall.UI.Layout;

public partial class IngestMissingSidebar
{
    [Inject]
    private IngestStatusService IngestStatus { get; set; } = null!;

    [Parameter]
    public bool Compact { get; set; }

    private string ListClass => Compact
        ? "p-3 rounded-xl border bd text-12 mt-2 max-h-[8rem] overflow-y-auto overflow-x-hidden min-w-0 w-full"
        : "p-3 rounded-xl border bd text-12 mt-2 max-h-[16rem] overflow-y-auto overflow-x-hidden min-w-0 w-full";

    private Timer? timer;

    private IReadOnlyList<IngestMissingNote> notes = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RefreshNotes();
        timer = new Timer(static state => ((IngestMissingSidebar)state!).OnTimer(), this, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    protected override void OnStoreChanged()
    {
        RefreshNotes();
        base.OnStoreChanged();
    }

    private void OnTimer()
    {
#pragma warning disable VSTHRD110
        InvokeAsync(RefreshAsync);
#pragma warning restore VSTHRD110
    }

    private Task RefreshAsync()
    {
        RefreshNotes();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void RefreshNotes()
    {
        notes = IngestStatus.ListMissingNotes(Ui.Current.Grants);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer?.Dispose();
        }

        base.Dispose(disposing);
    }
}
