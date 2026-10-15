namespace Recall.Core.Checkpoints;

public enum IngestCursorState
{
    Reading = 1,
    Finished = 2,
    Missing = 3,
}

public enum IngestCursorDecision
{
    Read = 0,
    Skip = 1,
}

public sealed record IngestFileRow(string Path, string ContentHash, IngestCursorState State, string GrantPath);

public sealed record IngestChunkRow(string ContentId, string Path, int Ordinal, bool Missing, bool Retired);

public sealed record GrantScanRow(string GrantPath, IngestCursorState State);

public static class IngestCursorPolicy
{
    public static IngestCursorDecision Decide(IngestFileRow? row, string contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
        {
            throw new ArgumentException("Content hash must not be blank.", nameof(contentHash));
        }

        if (row is null)
        {
            return IngestCursorDecision.Read;
        }

        if (row.State == IngestCursorState.Reading)
        {
            return IngestCursorDecision.Read;
        }

        if (row.State == IngestCursorState.Missing)
        {
            return IngestCursorDecision.Read;
        }

        if (row.State == IngestCursorState.Finished
            && string.Equals(row.ContentHash, contentHash, StringComparison.Ordinal))
        {
            return IngestCursorDecision.Skip;
        }

        return IngestCursorDecision.Read;
    }
}
