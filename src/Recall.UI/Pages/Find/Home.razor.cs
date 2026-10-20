using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Recall.Core.Ports;
using Recall.Core.Scope;

namespace Recall.UI.Pages;

public partial class Home
{
    [Inject]
    private ITextSearchService Search { get; set; } = null!;

    [Inject]
    private ILocalFileOpener Opener { get; set; } = null!;

    [Inject]
    private IIndexNowService IndexNow { get; set; } = null!;

    [Inject]
    private IRemoteFileSkipReport RemoteSkips { get; set; } = null!;

    [Inject]
    private IDatasetSkipReport DatasetSkips { get; set; } = null!;

    private string? query;

    private string? faultKey;

    private TextFindView view = TextFindView.Empty;

    private bool hasSearched;

    private IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> offers = [];

    protected override void OnInitialized()
    {
        offers = IndexNow.ListOffers();
    }

    private Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        if (!string.Equals(args.Key, "Enter", StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        return SubmitAsync();
    }

    private async Task SubmitAsync()
    {
        if (!TextFindSession.ShouldQuery(query))
        {
            view = TextFindView.Empty;
            faultKey = null;
            hasSearched = false;
            return;
        }

        try
        {
            RemoteSkips.Clear();
            DatasetSkips.Clear();
            var hits = await Search.FindAsync(query!.Trim(), Ui.Current.Grants);
            view = TextFindSession.Build(hits, Opener.Exists);
            faultKey = null;
            hasSearched = true;
        }
        catch (Exception ex)
        {
            faultKey = TextFindSession.FaultKey(ex);
            view = TextFindView.Empty;
            hasSearched = true;
        }
    }

    private async Task IndexNowAsync(string path)
    {
        try
        {
            await IndexNow.IndexAsync(path);
            offers = IndexNow.ListOffers();
            faultKey = null;
        }
        catch (Exception ex)
        {
            faultKey = TextFindSession.FaultKey(ex);
        }
    }

    private async Task OpenAsync(TextFindCard card)
    {
        if (card.Unavailable)
        {
            return;
        }

        var result = await Opener.OpenAsync(card.SourcePath);
        if (result == Recall.Core.Search.LocalFileOpenResult.Failed)
        {
            faultKey = TextFindSession.OpenFaultKey(result);
        }
        else if (result == Recall.Core.Search.LocalFileOpenResult.Missing)
        {
            var unavailable = card with { Unavailable = true };
            view = new TextFindView(
                view.Immediate.Select(c => c.SourcePath == card.SourcePath ? unavailable : c).ToList(),
                view.Deferred.Select(c => c.SourcePath == card.SourcePath ? unavailable : c).ToList(),
                view.HasHits);
        }
    }
}
