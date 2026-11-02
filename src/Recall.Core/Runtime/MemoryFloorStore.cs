using System.Text.Json;

namespace Recall.Core.Runtime;

public sealed class MemoryFloorStore
{
    public const string FileName = "memory-floor.json";

    private readonly string? path;

    public MemoryFloorStore(string? directory)
    {
        path = string.IsNullOrWhiteSpace(directory) ? null : Path.Combine(directory, FileName);
    }

    public long LoadObserved()
    {
        if (path is null || !File.Exists(path))
        {
            return 0;
        }

        try
        {
            var record = JsonSerializer.Deserialize<MemoryFloorRecord>(File.ReadAllText(path));
            return record is null ? 0 : Math.Max(0, record.ObservedFloorBytes);
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            return 0;
        }
    }

    public void SaveObserved(long bytes)
    {
        if (path is null || bytes <= 0)
        {
            return;
        }

        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(new MemoryFloorRecord(bytes)));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // A floor that cannot be persisted is not a reason to stop ingest.
        }
    }
}

public sealed record MemoryFloorRecord(long ObservedFloorBytes);
