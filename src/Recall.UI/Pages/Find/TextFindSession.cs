using Recall.Core.Ingest;
using Recall.Core.Scope;
using Recall.Core.Search;
using System.Globalization;

namespace Recall.UI.Pages;

public static class TextFindSession
{
    public const int CardExcerptChars = 480;

    public static bool ShouldQuery(string? text) => !string.IsNullOrWhiteSpace(text);

    public static string? FaultKey(Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

        return "Search_Fault";
    }

    public static bool MarkUnavailable(LocalFileOpenResult result) => result == LocalFileOpenResult.Missing;

    public static string? OpenFaultKey(LocalFileOpenResult result) =>
        result == LocalFileOpenResult.Failed ? "Search_OpenFailed" : null;

    public static TextFindView Build(IReadOnlyList<TextSearchHit> hits, Func<string, bool> fileExists)
    {
        if (hits.Count == 0)
        {
            return TextFindView.Empty;
        }

        var cards = hits.Select(hit => MapTextHit(hit, fileExists)).ToList();
        return new TextFindView(cards, true);
    }

    public static IReadOnlyList<TextFindCard> BuildMedia(IReadOnlyList<MediaSearchHit> hits, Func<string, bool> fileExists)
    {
        return hits.Select(hit => MapMediaHit(hit, fileExists)).ToList();
    }

    private static TextFindCard MapTextHit(TextSearchHit hit, Func<string, bool> fileExists)
    {
        var unavailable = hit.SourceMissing || !fileExists(hit.SourcePath);
        var localTime = hit.ModifiedAt == DateTimeOffset.UnixEpoch
            ? string.Empty
            : hit.ModifiedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var reasonKey = ReasonKey(hit.MatchReason);
        var showPin = MediaFileKinds.IsImage(hit.SourcePath)
            || MediaFileKinds.IsVideo(hit.SourcePath)
            || MediaFileKinds.IsAudio(hit.SourcePath);
        return new TextFindCard(
            hit.SourcePath,
            hit.FileName,
            TextFindCardKind.KindKeyFromPath(hit.SourcePath),
            localTime,
            reasonKey,
            unavailable,
            Excerpt(hit.RawNote),
            hit.StartSeconds,
            showPin,
            MatchStrength.Percent(hit.Score),
            hit.MatchReason == TextMatchReason.DenseAndFullText);
    }

    private static TextFindCard MapMediaHit(MediaSearchHit hit, Func<string, bool> fileExists)
    {
        var path = hit.SourcePath;
        var unavailable = !fileExists(path);
        return new TextFindCard(
            path,
            Path.GetFileName(path),
            TextFindCardKind.KindKeyFromPath(path),
            string.Empty,
            string.Empty,
            unavailable,
            string.Empty,
            hit.StartSeconds,
            true,
            MatchStrength.Percent(hit.Similarity),
            false);
    }

    private static string ReasonKey(TextMatchReason reason) => reason switch
    {
        TextMatchReason.DenseAndFullText => "Search_Match_MeaningAndWords",
        TextMatchReason.FullText => "Search_Match_Words",
        TextMatchReason.WordsOnMedia => "Search_Match_WordsOnMedia",
        _ => "Search_Match_Words",
    };

    private static string Excerpt(string raw)
    {
        if (raw.Length <= CardExcerptChars)
        {
            return raw;
        }

        return string.Concat(raw.AsSpan(0, CardExcerptChars - 1), "\u2026");
    }
}