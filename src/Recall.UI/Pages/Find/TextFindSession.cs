using System.Globalization;
using Recall.Core.Search;

namespace Recall.UI.Pages;

public static class TextFindSession
{
    public const int ImmediateCardCount = 5;

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
            return new TextFindCard(
                hit.SourcePath,
                hit.FileName,
                "Kind_Document",
                hit.ModifiedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                "Search_Match_MeaningAndWords",
                unavailable);
        }).ToList();

        var immediate = cards.Take(ImmediateCardCount).ToList();
        var deferred = cards.Skip(ImmediateCardCount).ToList();
        return new TextFindView(immediate, deferred, true);
    }
}

public sealed record TextFindCard(string SourcePath, string FileName, string KindKey, string LocalTime, string ReasonKey, bool Unavailable);

public sealed record TextFindView(IReadOnlyList<TextFindCard> Immediate, IReadOnlyList<TextFindCard> Deferred, bool HasHits)
{
    public static TextFindView Empty { get; } = new TextFindView([], [], false);
}
