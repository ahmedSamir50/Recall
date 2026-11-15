using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Net;
using System.Text;
using Recall.Core.Age;
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.Core.Text;
using Recall.UI.Components.Find;
using Recall.UI.Localization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Recall.UI.Pages;

#pragma warning disable S8949 // Only the audio file picker uses audioPickCts; other awaits are unrelated.
public partial class Home
{
  private const string PictureWaitingKey = "Search_PictureWaiting";
  private const string AudioWaitingKey = "Search_AudioWaiting";
  private const string WorkingKey = "Search_Working";
  private const string TimedOutKey = "Search_TimedOut";
  private const int HoldMilliseconds = 500;

  private static readonly string PictureAccept = string.Join(",", DatasetFolderSkip.ImageExtensions);

  private static readonly string[] AudioAcceptExtensions =
      MediaFileKinds.AudioAccept.Split(',', StringSplitOptions.RemoveEmptyEntries);

  private static bool ShowPictureControl =>
      RecallText.HasValue(PictureWaitingKey, CultureInfo.GetCultureInfo("en"))
      && RecallText.HasValue(PictureWaitingKey, CultureInfo.GetCultureInfo("ar"));

  private static bool ShowAudioControl =>
      RecallText.HasValue(AudioWaitingKey, CultureInfo.GetCultureInfo("en"))
      && RecallText.HasValue(AudioWaitingKey, CultureInfo.GetCultureInfo("ar"));

  [Inject]
  private ITextSearchService Search { get; set; } = null!;

  [Inject]
  private ILocalFileOpener Opener { get; set; } = null!;

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
  private IModelDownloadService ModelDownloads { get; set; } = null!;

  [Inject]
  private PinService Pins { get; set; } = null!;

  [Inject]
  private IIngestCursorStore Cursors { get; set; } = null!;

  [Inject]
  private IGrantFileUrl GrantUrls { get; set; } = null!;

  [Inject]
  private IFolderReveal FolderReveal { get; set; } = null!;

  private string? query;
  private byte[]? pickedPicture;
  private string? lensStatusKey;
  private IReadOnlyList<MediaSearchHit> pictureHits = [];
  private bool pictureDownloadStarted;
  private float[]? pickedAudio;
  private string? pickedAudioName;
  private string? audioLensStatusKey;
  private IReadOnlyList<MediaSearchHit> audioHits = [];
  private bool audioDownloadStarted;
  private int audioPickGeneration;
  private int pictureGeneration;
  private CancellationTokenSource? audioPickCts;
  private CancellationTokenSource? findCts;
  private string? faultKey;
  private TextFindView textView = TextFindView.Empty;
  private bool hasSearched;
  private bool findWorking;
  private TextFindCard? menuCard;
  private CancellationTokenSource? holdCts;
  private bool modalVisible;
  private TextFindCard? modalCard;
  private string? modalMediaUrl;

  private IReadOnlyList<TextFindCard> DisplayCards
  {
    get
    {
      var list = new List<TextFindCard>(textView.Cards);
      list.AddRange(TextFindSession.BuildMedia(pictureHits, Opener.Exists));
      list.AddRange(TextFindSession.BuildMedia(audioHits, Opener.Exists));
      return list;
    }
  }

  private IReadOnlyList<SearchResultGroup> ResultGroups => SearchGroups.Split(DisplayCards);

  private bool HasAnyResults => DisplayCards.Count > 0;

  private bool HasLensActive => pickedPicture is not null || pickedAudioName is not null;

  private bool ShowEmptyPrompt => !findWorking && !HasAnyResults && !hasSearched && !HasLensActive
      && lensStatusKey is null && audioLensStatusKey is null && faultKey is null;

  private bool ShowStatusLine => findWorking || lensStatusKey == TimedOutKey || audioLensStatusKey == TimedOutKey
      || lensStatusKey == WorkingKey || audioLensStatusKey == WorkingKey;

  private string? StatusLineKey => findWorking ? WorkingKey : lensStatusKey ?? audioLensStatusKey;

  private string? LensDataUri => pickedPicture is null ? null : "data:image/jpeg;base64," + Convert.ToBase64String(pickedPicture);

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
      textView = TextFindView.Empty;
      faultKey = null;
      hasSearched = false;
      return;
    }

    await CancelFindAsync().ConfigureAwait(false);
    findCts = new CancellationTokenSource();
    var linked = CancellationTokenSource.CreateLinkedTokenSource(findCts.Token);
    linked.CancelAfter(SearchDeadline.FindTimeout);
    findWorking = true;
    menuCard = null;

    try
    {
      RemoteSkips.Clear();
      DatasetSkips.Clear();
      var hits = await Search.FindAsync(query!.Trim(), Ui.Current.Grants, linked.Token);
      textView = TextFindSession.Build(hits, Opener.Exists);
      faultKey = null;
      hasSearched = true;
    }
    catch (OperationCanceledException) when (findCts.IsCancellationRequested)
    {
      textView = TextFindView.Empty;
      faultKey = TimedOutKey;
      hasSearched = true;
    }
    catch (Exception ex)
    {
      faultKey = TextFindSession.FaultKey(ex);
      textView = TextFindView.Empty;
      hasSearched = true;
    }
    finally
    {
      findWorking = false;
      linked.Dispose();
    }
  }

  private async Task ClearPictureAsync()
  {
    pictureGeneration++;
    await CancelFindAsync().ConfigureAwait(false);
    pickedPicture = null;
    pictureHits = [];
    lensStatusKey = null;
    pictureDownloadStarted = false;
    menuCard = null;
    await CloseModalAsync();
  }

  private async Task ClearAudioAsync()
  {
    audioPickGeneration++;
    await CancelAudioPickAsync().ConfigureAwait(false);
    await CancelFindAsync().ConfigureAwait(false);
    pickedAudio = null;
    pickedAudioName = null;
    audioHits = [];
    audioLensStatusKey = null;
    audioDownloadStarted = false;
    menuCard = null;
    await CloseModalAsync();
  }

  [SuppressMessage("Major Code Smell", "S5693:Limit the content length of HTTP requests", Justification = "The picture read stream is capped at 32 MiB.")]
  private async Task OnPictureSelectedAsync(InputFileChangeEventArgs args)
  {
    var file = args.File;
    if (file is null || !DatasetFolderSkip.ImageExtensions.Contains(Path.GetExtension(file.Name), StringComparer.OrdinalIgnoreCase))
    {
      return;
    }

    var generation = ++pictureGeneration;
    audioPickGeneration++;
    await CancelAudioPickAsync().ConfigureAwait(false);
    pickedAudio = null;
    pickedAudioName = null;
    audioHits = [];
    audioLensStatusKey = null;
    await using var stream = file.OpenReadStream(maxAllowedSize: 32L * 1024 * 1024);
    using var buffer = new MemoryStream();
    await stream.CopyToAsync(buffer);
    if (generation != pictureGeneration)
    {
      return;
    }

    pickedPicture = buffer.ToArray();
    lensStatusKey = WorkingKey;
    findWorking = true;
    await RunPictureSearchAsync(generation);
  }

  [SuppressMessage("Major Code Smell", "S5693:Limit the content length of HTTP requests", Justification = "The audio read stream is capped at 1024 MiB.")]
  private async Task OnAudioSelectedAsync(InputFileChangeEventArgs args)
  {
    var file = args.File;
    if (file is null || !AudioAcceptExtensions.Contains(Path.GetExtension(file.Name), StringComparer.OrdinalIgnoreCase))
    {
      return;
    }

    pictureGeneration++;
    pickedPicture = null;
    pictureHits = [];
    lensStatusKey = null;
    pickedAudioName = file.Name;

    var generation = ++audioPickGeneration;
    await CancelAudioPickAsync().ConfigureAwait(false);
    audioPickCts = new CancellationTokenSource();
    var token = audioPickCts.Token;

    var bytes = new byte[file.Size];
    try
    {
      await using var stream = file.OpenReadStream(maxAllowedSize: 1024L * 1024 * 1024, cancellationToken: token);
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
    }
    catch (OperationCanceledException)
    {
      return;
    }

    if (generation != audioPickGeneration)
    {
      return;
    }

    try
    {
      var pcm = await Task.Run(
          () =>
              WavPcm.IsWav(bytes)
                  ? WavPcm.Read(bytes)
                  : AudioSource.ReadCompressed(bytes),
          token).ConfigureAwait(false);
      if (generation != audioPickGeneration)
      {
        return;
      }

      pickedAudio = pcm;
    }
    catch (OperationCanceledException)
    {
      return;
    }
    catch (Exception)
    {
      if (generation != audioPickGeneration)
      {
        return;
      }

      pickedAudio = null;
      audioLensStatusKey = "Search_Fault";
      audioDownloadStarted = false;
      return;
    }

    audioLensStatusKey = WorkingKey;
    findWorking = true;
    await RunAudioSearchAsync(generation, token).ConfigureAwait(false);
  }

  private async Task RunAudioSearchAsync(int generation, CancellationToken cancellationToken)
  {
    if (pickedAudio is null || generation != audioPickGeneration)
    {
      return;
    }

    audioHits = [];
    await CancelFindAsync().ConfigureAwait(false);
    findCts = new CancellationTokenSource();
    var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, findCts.Token);
    linked.CancelAfter(SearchDeadline.FindTimeout);

    var decision = AudioQueryGate.Decide(ModelDownloads.GetAudioDownloadSnapshot().Phase);
    if (decision == PictureQueryDecision.Wait)
    {
      audioLensStatusKey = AudioWaitingKey;
      findWorking = false;
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
          if (generation != audioPickGeneration)
          {
            return;
          }

          audioLensStatusKey = "Search_AudioFailed";
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
      audioLensStatusKey = "Search_AudioFailed";
      findWorking = false;
      audioDownloadStarted = false;
      return;
    }

    try
    {
      findWorking = true;
      audioLensStatusKey = WorkingKey;
      var hits = await Audios.FindAsync(pickedAudio, Ui.Current.Grants, linked.Token).ConfigureAwait(false);
      if (generation != audioPickGeneration)
      {
        return;
      }

      audioHits = hits;
      audioLensStatusKey = hits.Count == 0 ? "Search_NoHitsLead" : null;
    }
    catch (OperationCanceledException)
    {
      if (generation != audioPickGeneration)
      {
        return;
      }

      audioHits = [];
      audioLensStatusKey = TimedOutKey;
      audioDownloadStarted = false;
    }
    catch (Exception)
    {
      if (generation != audioPickGeneration)
      {
        return;
      }

      audioLensStatusKey = "Search_Fault";
      audioDownloadStarted = false;
    }
    finally
    {
      findWorking = false;
      linked.Dispose();
    }
  }

  private async Task RunPictureSearchAsync(int generation)
  {
    if (pickedPicture is null || generation != pictureGeneration)
    {
      return;
    }

    pictureHits = [];
    await CancelFindAsync().ConfigureAwait(false);
    findCts = new CancellationTokenSource();
    findCts.CancelAfter(SearchDeadline.FindTimeout);

    var decision = PictureQueryGate.Decide(ModelDownloads.GetVisionDownloadSnapshot().Phase);
    if (decision == PictureQueryDecision.Wait)
    {
      lensStatusKey = PictureWaitingKey;
      findWorking = false;
      if (!pictureDownloadStarted)
      {
        pictureDownloadStarted = true;
        try
        {
          await ModelDownloads.DownloadVisionModelAsync(null);
        }
        catch (Exception)
        {
          if (generation != pictureGeneration)
          {
            return;
          }

          lensStatusKey = "Search_PictureFailed";
          return;
        }
      }

      if (generation != pictureGeneration || pickedPicture is null)
      {
        return;
      }

      decision = PictureQueryGate.Decide(ModelDownloads.GetVisionDownloadSnapshot().Phase);
    }

    if (generation != pictureGeneration || pickedPicture is null)
    {
      return;
    }

    if (decision == PictureQueryDecision.Wait)
    {
      return;
    }

    if (decision == PictureQueryDecision.Fail)
    {
      lensStatusKey = "Search_PictureFailed";
      findWorking = false;
      return;
    }

    try
    {
      findWorking = true;
      lensStatusKey = WorkingKey;
      var hits = await Pictures.FindAsync(pickedPicture, Ui.Current.Grants, findCts.Token);
      if (generation != pictureGeneration)
      {
        return;
      }

      pictureHits = hits;
      lensStatusKey = hits.Count == 0 ? "Search_NoHitsLead" : null;
    }
    catch (OperationCanceledException)
    {
      if (generation != pictureGeneration)
      {
        return;
      }

      pictureHits = [];
      lensStatusKey = TimedOutKey;
    }
    catch (Exception)
    {
      if (generation != pictureGeneration)
      {
        return;
      }

      lensStatusKey = "Search_Fault";
    }
    finally
    {
      findWorking = false;
    }
  }

  private void OpenMenu(TextFindCard card) => menuCard = card;

  private void CloseMenu() => menuCard = null;

  private void OnCardPointerDown(TextFindCard card)
  {
    _ = CancelHoldAsync();
    holdCts = new CancellationTokenSource();
    var token = holdCts.Token;
    _ = Task.Run(
        async () =>
        {
          try
          {
            await Task.Delay(HoldMilliseconds, token).ConfigureAwait(false);
            await InvokeAsync(() =>
            {
              if (token.IsCancellationRequested)
              {
                return;
              }

              menuCard = card with { };
              StateHasChanged();
            }).ConfigureAwait(false);
          }
          catch (OperationCanceledException)
          {
            // Hold cancelled before the menu opened.
          }
        },
        token);
  }

  private void OnCardPointerUp() => _ = CancelHoldAsync();

  private async Task OpenCardAsync(TextFindCard card)
  {
    CloseMenu();
    if (card.Unavailable)
    {
      return;
    }

    var result = MediaHitStart.Seeks(card.StartSeconds)
        ? await Opener.OpenAtAsync(card.SourcePath, card.StartSeconds)
        : await Opener.OpenAsync(card.SourcePath);
    if (result == LocalFileOpenResult.Failed)
    {
      faultKey = TextFindSession.OpenFaultKey(result);
    }
    else if (result == LocalFileOpenResult.Missing)
    {
      MarkUnavailable(card);
    }
  }

  private Task RevealCardAsync(TextFindCard card)
  {
    CloseMenu();
    if (!FolderReveal.Reveal(card.SourcePath))
    {
      faultKey = "Search_OpenPathUnsupported";
    }

    return Task.CompletedTask;
  }

  private async Task ViewOrPlayAsync(TextFindCard card)
  {
    CloseMenu();
    if (card.Unavailable)
    {
      return;
    }

    modalCard = card;
    modalMediaUrl = GrantUrls.Build(card.SourcePath, Ui.Current.Grants);
    modalVisible = true;
    await InvokeAsync(StateHasChanged);
  }

  private async Task CloseModalAsync()
  {
    modalVisible = false;
    modalCard = null;
    modalMediaUrl = null;
    await InvokeAsync(StateHasChanged);
  }

  private void MarkUnavailable(TextFindCard card)
  {
    var unavailable = card with { Unavailable = true };
    textView = new TextFindView(
        textView.Cards.Select(c => string.Equals(c.SourcePath, card.SourcePath, StringComparison.OrdinalIgnoreCase) ? unavailable : c).ToList(),
        textView.HasHits);
  }

  private void PinKeep(TextFindCard card)
  {
    CloseMenu();
    if (card.Unavailable || !card.ShowPin)
    {
      return;
    }

    if (Cursors.IsPinned(card.SourcePath))
    {
      Pins.Unpin(card.SourcePath);
      return;
    }

    Pins.Pin(card.SourcePath);
  }

  private static bool IsTextGroupCard(TextFindCard card) =>
      TextFindCardKind.KindFromPath(card.SourcePath) == SearchGroupKind.Text;

  private MarkupString ExcerptMarkup(TextFindCard card)
  {
    if (string.IsNullOrEmpty(card.RawNote) || string.IsNullOrWhiteSpace(query))
    {
      return (MarkupString)WebUtility.HtmlEncode(card.RawNote);
    }

    var marks = Bm25Highlight.Marks(card.RawNote, query.Trim());
    if (marks.Count == 0)
    {
      return (MarkupString)WebUtility.HtmlEncode(card.RawNote);
    }

    var ordered = marks.OrderBy(m => m.Start).ToList();
    var builder = new StringBuilder();
    var cursor = 0;
    foreach (var mark in ordered)
    {
      if (mark.Start < cursor)
      {
        continue;
      }

      builder.Append(WebUtility.HtmlEncode(card.RawNote[cursor..mark.Start]));
      builder.Append("<mark>");
      builder.Append(WebUtility.HtmlEncode(card.RawNote.Substring(mark.Start, mark.Length)));
      builder.Append("</mark>");
      cursor = mark.Start + mark.Length;
    }

    builder.Append(WebUtility.HtmlEncode(card.RawNote[cursor..]));
    return (MarkupString)builder.ToString();
  }

  private static string? ThumbnailUrl(IGrantFileUrl grantUrls, TextFindCard card, IReadOnlyList<ScopeFolderGrant> grants)
  {
    if (IsTextGroupCard(card) || card.Unavailable)
    {
      return null;
    }

    return grantUrls.Build(card.SourcePath, grants);
  }

  private async Task CancelAudioPickAsync()
  {
    if (audioPickCts is null)
    {
      return;
    }

    await audioPickCts.CancelAsync().ConfigureAwait(false);
    audioPickCts.Dispose();
    audioPickCts = null;
  }

  private async Task CancelFindAsync()
  {
    if (findCts is null)
    {
      return;
    }

    await findCts.CancelAsync().ConfigureAwait(false);
    findCts.Dispose();
    findCts = null;
  }

  private async Task CancelHoldAsync()
  {
    if (holdCts is null)
    {
      return;
    }

    await holdCts.CancelAsync().ConfigureAwait(false);
    holdCts.Dispose();
    holdCts = null;
  }
}