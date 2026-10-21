using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.UI.Localization;

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

    [Inject]
    private IPictureSearchService Pictures { get; set; } = null!;

    [Inject]
    private IModelDownloadService ModelDownloads { get; set; } = null!;

    private const string PictureWaitingKey = "Search_PictureWaiting";

    private static readonly string PictureAccept = string.Join(",", DatasetFolderSkip.ImageExtensions);

    private static bool ShowPictureControl =>
        RecallText.HasValue(PictureWaitingKey, CultureInfo.GetCultureInfo("en"))
        && RecallText.HasValue(PictureWaitingKey, CultureInfo.GetCultureInfo("ar"));

    private string? query;

    private byte[]? pickedPicture;

    private string? pictureKey;

    private IReadOnlyList<MediaSearchHit> pictureHits = [];

    private bool pictureDownloadStarted;

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

    [SuppressMessage("Major Code Smell", "S5693:Limit the content length of HTTP requests", Justification = "The picture read stream is capped at 32 MiB.")]
    private async Task OnPictureSelectedAsync(InputFileChangeEventArgs args)
    {
        var file = args.File;
        if (file is null || !DatasetFolderSkip.ImageExtensions.Contains(Path.GetExtension(file.Name), StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        await using var stream = file.OpenReadStream(maxAllowedSize: 32L * 1024 * 1024);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        pickedPicture = buffer.ToArray();
        await RunPictureSearchAsync();
    }

    private async Task RunPictureSearchAsync()
    {
        if (pickedPicture is null)
        {
            return;
        }

        pictureHits = [];
        pictureKey = null;
        var decision = PictureQueryGate.Decide(ModelDownloads.GetVisionDownloadSnapshot().Phase);
        if (decision == PictureQueryDecision.Wait)
        {
            pictureKey = PictureWaitingKey;
            if (!pictureDownloadStarted)
            {
                pictureDownloadStarted = true;
                try
                {
                    await ModelDownloads.DownloadVisionModelAsync(null);
                }
                catch (Exception)
                {
                    pictureKey = "Search_PictureFailed";
                    return;
                }
            }

            decision = PictureQueryGate.Decide(ModelDownloads.GetVisionDownloadSnapshot().Phase);
        }

        if (decision == PictureQueryDecision.Wait)
        {
            return;
        }

        if (decision == PictureQueryDecision.Fail)
        {
            pictureKey = "Search_PictureFailed";
            return;
        }

        try
        {
            var hits = await Pictures.FindAsync(pickedPicture, Ui.Current.Grants);
            pictureHits = hits;
            if (hits.Count == 0)
            {
                pictureKey = "Search_NoHitsLead";
            }
        }
        catch (Exception)
        {
            pictureKey = "Search_Fault";
        }
    }

    private static string FormatSeconds(double seconds) =>
        seconds.ToString("0.##", CultureInfo.InvariantCulture);

    private async Task OpenPictureAsync(MediaSearchHit hit)
    {
        var result = await Opener.OpenAsync(hit.SourcePath);
        if (result == LocalFileOpenResult.Failed)
        {
            faultKey = TextFindSession.OpenFaultKey(result);
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
