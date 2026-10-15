using System.Globalization;
using Recall.Core.Vocabulary;

namespace Recall.Core.Sync;

public sealed class LocalOpLogWriter
{
    public const string DirectoryName = "oplog";

    public const string DayFilePattern = "yyyy-MM-dd";

    private readonly string? directory;
    private readonly List<LocalOpLogEntry> entries = [];

    public LocalOpLogWriter(string? directory = null)
    {
        this.directory = directory;
    }

    public IReadOnlyList<LocalOpLogEntry> Entries => entries;

    public void AppendInsert(string contentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        var entry = new LocalOpLogEntry(OpLogOperation.Insert, contentId, DateTimeOffset.UtcNow, Sent: false);
        entries.Add(entry);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, entry.At.UtcDateTime.ToString(DayFilePattern, CultureInfo.InvariantCulture) + ".log");
        var line = string.Create(CultureInfo.InvariantCulture, $"{entry.At:o}\t{entry.Operation}\t{entry.ContentId}{Environment.NewLine}");
        File.AppendAllText(path, line);
    }

    public void AppendDelete(string contentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        var entry = new LocalOpLogEntry(OpLogOperation.Delete, contentId, DateTimeOffset.UtcNow, Sent: false);
        entries.Add(entry);
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
