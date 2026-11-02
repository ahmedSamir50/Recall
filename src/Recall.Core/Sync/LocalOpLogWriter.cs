using Recall.Core.Vocabulary;
using System.Globalization;

namespace Recall.Core.Sync;

public sealed class LocalOpLogWriter
{
    public const string DirectoryName = "oplog";

    public const string DayFilePattern = "yyyy-MM-dd";

    public const int MaxEntriesInMemory = 10_000;

    private readonly string? directory;
    private readonly Queue<LocalOpLogEntry> entries = new();

    public LocalOpLogWriter(string? directory = null)
    {
        this.directory = directory;
    }

    public IReadOnlyList<LocalOpLogEntry> Entries => [.. entries];

    public void AppendInsert(string contentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        AppendOperation(OpLogOperation.Insert, contentId);
    }

    public void AppendDelete(string contentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        AppendOperation(OpLogOperation.Delete, contentId);
    }

    public void AppendPin(string contentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        AppendOperation(OpLogOperation.Pin, contentId);
    }

    public void AppendUnpin(string contentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        AppendOperation(OpLogOperation.Unpin, contentId);
    }

    private void AppendOperation(OpLogOperation operation, string contentId)
    {
        var entry = new LocalOpLogEntry(operation, contentId, DateTimeOffset.UtcNow, Sent: false);
        if (entries.Count >= MaxEntriesInMemory)
        {
            entries.Dequeue();
        }

        entries.Enqueue(entry);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, entry.At.UtcDateTime.ToString(DayFilePattern, CultureInfo.InvariantCulture) + ".log");
        var line = string.Create(CultureInfo.InvariantCulture, $"{entry.At:o}\t{entry.Operation}\t{entry.ContentId}{Environment.NewLine}");
        File.AppendAllText(path, line);
    }
}

public sealed record LocalOpLogEntry(OpLogOperation Operation, string ContentId, DateTimeOffset At, bool Sent);
