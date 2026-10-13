using Recall.Core.Vocabulary;

namespace Recall.Core.Sync;

public sealed class LocalOpLogWriter
{
    private readonly List<LocalOpLogEntry> entries = [];

    public IReadOnlyList<LocalOpLogEntry> Entries => entries;

    public void AppendInsert(string contentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        entries.Add(new LocalOpLogEntry(OpLogOperation.Insert, contentId, DateTimeOffset.UtcNow, Sent: false));
    }
}

public sealed record LocalOpLogEntry(OpLogOperation Operation, string ContentId, DateTimeOffset At, bool Sent);
