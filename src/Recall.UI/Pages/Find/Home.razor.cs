using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.UI.Localization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Recall.UI.Pages;

#pragma warning disable S8949 // Only the audio file picker uses audioPickCts; other awaits are unrelated.
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
    private IAudioSearchService Audios { get; set; } = null!;

    [Inject]
    private IAudioSource AudioSource { get; set; } = null!;

    [Inject]
    private IAudioOffReport AudioOff { get; set; } = null!;

    [Inject]
    private IMediaPreview Preview { get; set; } = null!;

    [Inject]
    private IModelDownloadService ModelDownloads { get; set; } = null!;

    [Inject]
    private IngestStatusService IngestStatus { get; set; } = null!;

    private const string PictureWaitingKey = "Search_PictureWaiting";

    private const string AudioWaitingKey = "Search_AudioWaiting";

    private static readonly string PictureAccept = string.Join(",", DatasetFolderSkip.ImageExtensions);

    private static readonly string[] AudioAcceptExtensions =
        MediaFileKinds.AudioAccept.Split(',', StringSplitOptions.RemoveEmptyEntries);

    private static bool ShowPictureControl =>
        RecallText.HasValue(PictureWaitingKey, CultureInfo.GetCultureInfo("en"))
        && RecallText.HasValue(PictureWaitingKey, CultureInfo.GetCultureInfo("ar"));

    private static bool ShowAudioControl =>
        RecallText.HasValue(AudioWaitingKey, CultureInfo.GetCultureInfo("en"))
        && RecallText.HasValue(AudioWaitingKey, CultureInfo.GetCultureInfo("ar"));

    private string? query;

    private byte[]? pickedPicture;

    private string? pictureKey;

    private IReadOnlyList<MediaSearchHit> pictureHits = [];

    private bool pictureDownloadStarted;

    private float[]? pickedAudio;

    private string? audioKey;

    private IReadOnlyList<MediaSearchHit> audioHits = [];

    private bool audioDownloadStarted;

    private int audioPickGeneration;

    private CancellationTokenSource? audioPickCts;

    private string? faultKey;

    private TextFindView view = TextFindView.Empty;

    private bool hasSearched;

    private IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> offers = [];

    private IReadOnlyList<IngestKindCount> ingestRows = [];

    protected override void OnInitialized()
    {
        offers = IndexNow.ListOffers();
        RefreshIngestRows();
    }

    private void RefreshIngestRows()
    {
        var enabled = GrantFileKinds.None;
        foreach (var grant in Ui.Current.Grants)
        {
            enabled |= grant.EnabledKinds;
        }

        ingestRows = IngestStatus.List(enabled);
    }

    public static string IngestKindLabel(GrantFileKinds kind) => kind switch
    {
        GrantFileKinds.Text => "Ingest_Text",
        GrantFileKinds.Code => "Ingest_Code",
        GrantFileKinds.Image => "Ingest_Image",
        GrantFileKinds.Video => "Ingest_Video",
        GrantFileKinds.Audio => "Ingest_Audio",
        _ => "Ingest_Text",
    };

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

    [SuppressMessage("Major Code Smell", "S5693:Limit the content length of HTTP requests", Justification = "The audio read stream is capped at 1024 MiB.")]
    private async Task OnAudioSelectedAsync(InputFileChangeEventArgs args)
    {
        var file = args.File;
        if (file is null || !AudioAcceptExtensions.Contains(Path.GetExtension(file.Name), StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var generation = ++audioPickGeneration;
        if (audioPickCts is not null)
        {
            await audioPickCts.CancelAsync().ConfigureAwait(false);
            audioPickCts.Dispose();
        }
        audioPickCts = new CancellationTokenSource();
        var token = audioPickCts.Token;

        await using var stream = file.OpenReadStream(maxAllowedSize: 1024L * 1024 * 1024, cancellationToken: token);
        var bytes = new byte[file.Size];
        var offset = 0;
        while (offset < bytes.Length)
        {
            var read = await stream.ReadAsync(bytes.AsMemory(offset), token).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            offset += read;
        }

        if (generation != audioPickGeneration)
        {
            return;
        }

        try
        {
            pickedAudio = await Task.Run(
                () =>
                    WavPcm.IsWav(bytes)
                        ? WavPcm.Read(bytes)
                        : AudioSource.ReadCompressed(bytes),
                token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception)
        {
            pickedAudio = null;
            audioKey = "Search_Fault";
            audioDownloadStarted = false;
            return;
        }

        await RunAudioSearchAsync(generation, token).ConfigureAwait(false);
    }

    private async Task RunAudioSearchAsync(int generation, CancellationToken cancellationToken)
    {
        if (pickedAudio is null || generation != audioPickGeneration)
        {
            return;
        }

        audioHits = [];
        audioKey = null;
        var decision = AudioQueryGate.Decide(ModelDownloads.GetAudioDownloadSnapshot().Phase);
        if (decision == PictureQueryDecision.Wait)
        {
            audioKey = AudioWaitingKey;
            if (!audioDownloadStarted)
            {
                audioDownloadStarted = true;
                try
                {
                    await ModelDownloads.DownloadAudioModelAsync(null, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    audioDownloadStarted = false;
                    return;
                }
                catch (Exception)
                {
                    audioKey = "Search_AudioFailed";
                    audioDownloadStarted = false;
                    return;
                }
            }

            decision = AudioQueryGate.Decide(ModelDownloads.GetAudioDownloadSnapshot().Phase);
        }

        if (generation != audioPickGeneration)
        {
            return;
        }

        if (decision == PictureQueryDecision.Wait)
        {
            return;
        }

        if (decision == PictureQueryDecision.Fail)
        {
            audioKey = "Search_AudioFailed";
            audioDownloadStarted = false;
            return;
        }

        try
        {
            var hits = await Audios.FindAsync(pickedAudio, Ui.Current.Grants, cancellationToken).ConfigureAwait(false);
            if (generation != audioPickGeneration)
            {
                return;
            }

            audioHits = hits;
            if (hits.Count == 0)
            {
                audioKey = "Search_NoHitsLead";
            }
        }
        catch (OperationCanceledException)
        {
            audioDownloadStarted = false;
        }
        catch (Exception)
        {
            audioKey = "Search_Fault";
            audioDownloadStarted = false;
        }
    }

    private async Task OpenHitAsync(MediaSearchHit hit)
    {
        if (MediaHitStart.Seeks(MediaHitStart.Seconds(hit)))
        {
            await Preview.PlayAsync(hit.SourcePath, MediaHitStart.Seconds(hit));
            return;
        }

        var result = await Opener.OpenAsync(hit.SourcePath);
        if (result == LocalFileOpenResult.Failed)
        {
            faultKey = TextFindSession.OpenFaultKey(result);
        }
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

    private async Task IndexNowAsync(string path)
    {
        try
        {
            await IndexNow.IndexAsync(path);
            offers = IndexNow.ListOffers();
            RefreshIngestRows();
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

        if (MediaHitStart.Seeks(card.StartSeconds))
        {
            await Preview.PlayAsync(card.SourcePath, card.StartSeconds);
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
