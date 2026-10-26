using Recall.Core.Checkpoints;
using Recall.Core.Scope;

namespace Recall.Core.Ingest;

internal static class IngestKindCountPolicy
{
    public static void ValidateSingleKind(GrantFileKinds kind)
    {
        if (kind is GrantFileKinds.Text or GrantFileKinds.Code or GrantFileKinds.Image or GrantFileKinds.Video or GrantFileKinds.Audio)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(kind));
    }

    public static bool IsSeen(IngestCursorState state) =>
        state is IngestCursorState.Reading or IngestCursorState.Finished or IngestCursorState.Missing;

    public static bool IsFinished(IngestCursorState state) => state == IngestCursorState.Finished;
}
