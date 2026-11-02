using Recall.Core.Scope;
using Recall.Core.Search;
using System.Globalization;

namespace Recall.UI.Pages;

public static class TextFindSession
{
    public const int ImmediateCardCount = 5;

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

        var cards = hits.Select(hit =>
        {
            var unavailable = hit.SourceMissing || !fileExists(hit.SourcePath);
            var localTime = hit.ModifiedAt == DateTimeOffset.UnixEpoch
                ? string.Empty
                : hit.ModifiedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var showPin = MediaFileKinds.IsImage(hit.SourcePath)
                || MediaFileKinds.IsVideo(hit.SourcePath)
                || MediaFileKinds.IsAudio(hit.SourcePath);
            return new TextFindCard(
                hit.SourcePath,
                hit.FileName,
                "Kind_Document",
                localTime,
                "Search_Match_MeaningAndWords",
                unavailable,
                Excerpt(hit.RawNote),
                hit.StartSeconds,
                showPin);
        }).ToList();

        var immediate = cards.Take(ImmediateCardCount).ToList();
        var deferred = cards.Skip(ImmediateCardCount).ToList();
        return new TextFindView(immediate, deferred, true);
    }

    private static string Excerpt(string raw)
    {
        if (raw.Length <= CardExcerptChars)
        {
            return raw;
        }

        return string.Concat(raw.AsSpan(0, CardExcerptChars - 1), "\u2026");
    }
}

public sealed record TextFindCard(string SourcePath, string FileName, string KindKey, string LocalTime, string ReasonKey, bool Unavailable, string RawNote = "", double StartSeconds = 0, bool ShowPin = false);

public sealed record TextFindView(IReadOnlyList<TextFindCard> Immediate, IReadOnlyList<TextFindCard> Deferred, bool HasHits)
{
    public static TextFindView Empty { get; } = new TextFindView([], [], false);
}
