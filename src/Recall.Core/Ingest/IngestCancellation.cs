using Recall.Core.Checkpoints;

namespace Recall.Core.Ingest;

public static class IngestCancellation
{
    public static IngestCursorState PauseOnlyReading(IngestCursorState state) =>
        state switch
        {
            IngestCursorState.Reading => IngestCursorState.Paused,
            IngestCursorState.Finished => IngestCursorState.Finished,
            IngestCursorState.Missing => IngestCursorState.Missing,
            IngestCursorState.Paused => IngestCursorState.Paused,
            _ => state,
        };
}