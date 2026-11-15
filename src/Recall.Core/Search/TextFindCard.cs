using System.Diagnostics.CodeAnalysis;
using Recall.Core.Ingest;
using Recall.Core.Scope;

namespace Recall.Core.Search;

public sealed record TextFindCard(
    string SourcePath,
    string FileName,
    string KindKey,
    string LocalTime,
    string ReasonKey,
    bool Unavailable,
    string RawNote = "",
    double StartSeconds = 0,
    bool ShowPin = false,
    int? MatchPercent = null,
    bool ShowMeaningHelp = false);

public sealed record TextFindView(IReadOnlyList<TextFindCard> Cards, bool HasHits)
{
    public const int ImmediateCardCount = 5;

    public static TextFindView Empty { get; } = new([], false);

    [SuppressMessage("Major Code Smell", "S2365:Properties should not copy collections", Justification = "Keeps Immediate and Deferred for existing unit tests.")]
    public IReadOnlyList<TextFindCard> Immediate => Cards.Take(ImmediateCardCount).ToList();

    [SuppressMessage("Major Code Smell", "S2365:Properties should not copy collections", Justification = "Keeps Immediate and Deferred for existing unit tests.")]
    public IReadOnlyList<TextFindCard> Deferred => Cards.Skip(ImmediateCardCount).ToList();
}

public static class TextFindCardKind
{
    public static SearchGroupKind KindFromPath(string sourcePath)
    {
        if (MediaFileKinds.IsImage(sourcePath))
        {
            return SearchGroupKind.Image;
        }

        if (MediaFileKinds.IsVideo(sourcePath))
        {
            return SearchGroupKind.Video;
        }

        if (MediaFileKinds.IsAudio(sourcePath))
        {
            return SearchGroupKind.Audio;
        }

        return SearchGroupKind.Text;
    }

    public static string KindKeyFromPath(string sourcePath)
    {
        if (MediaFileKinds.IsImage(sourcePath))
        {
            return "Search_Kind_Image";
        }

        if (MediaFileKinds.IsVideo(sourcePath))
        {
            return "Search_Kind_Video";
        }

        if (MediaFileKinds.IsAudio(sourcePath))
        {
            return "Search_Kind_Audio";
        }

        if (FileAdmission.IsCodeFile(sourcePath))
        {
            return "Search_Kind_Code";
        }

        return "Search_Kind_Text";
    }
}