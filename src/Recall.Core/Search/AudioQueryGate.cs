using Recall.Core.Download;

namespace Recall.Core.Search;

public static class AudioQueryGate
{
    public static PictureQueryDecision Decide(TextModelDownloadPhase phase) => PictureQueryGate.Decide(phase);
}
