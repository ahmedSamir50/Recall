using Recall.Core.Download;

namespace Recall.Core.Search;

public enum PictureQueryDecision
{
    Wait = 0,
    Search = 1,
    Fail = 2,
}

public static class PictureQueryGate
{
    public static PictureQueryDecision Decide(TextModelDownloadPhase phase) => phase switch
    {
        TextModelDownloadPhase.NotStarted => PictureQueryDecision.Wait,
        TextModelDownloadPhase.Downloading => PictureQueryDecision.Wait,
        TextModelDownloadPhase.Ready => PictureQueryDecision.Search,
        _ => PictureQueryDecision.Fail,
    };
}
