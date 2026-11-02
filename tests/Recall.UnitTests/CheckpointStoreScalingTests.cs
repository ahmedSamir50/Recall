using FluentAssertions;
using Microsoft.Data.Sqlite;
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;

namespace Recall.UnitTests;

public sealed class CheckpointStoreScalingTests
{
    private static string NewDatabase()
    {
        var path = Path.Combine(Path.GetTempPath(), "recall-scale-" + Guid.NewGuid().ToString("N") + ".db");
        return path;
    }

    [Fact]
    public void AppendLiveChunks_StoresEveryChunkInOneTransaction()
    {
        var store = new SqliteIngestCursorStore(NewDatabase());
        var chunks = Enumerable.Range(0, 200)
            .Select(i => new ChunkWrite("id" + i.ToString("D8"), i))
            .ToList();

        store.AppendLiveChunks("/g/a", chunks);

        store.ListChunks("/g/a").Should().HaveCount(200);
        store.ListLiveAliases("id00000000").Should().ContainSingle();
    }

    [Fact]
    public void AppendLiveChunk_AllocationDoesNotGrowWithTable()
    {
        var store = new SqliteIngestCursorStore(NewDatabase());
        for (var i = 0; i < 200; i++)
        {
            store.AppendLiveChunk("/g/a", new ChunkWrite("a" + i.ToString("D8"), i));
        }

        var small = MeasureOneAppend(store, "probe-small");
        for (var i = 200; i < 2000; i++)
        {
            store.AppendLiveChunk("/g/a", new ChunkWrite("a" + i.ToString("D8"), i));
        }

        var large = MeasureOneAppend(store, "probe-large");
        var ratio = (double)large / Math.Max(1, small);
        ratio.Should().BeLessThan(3.0, "one append must not allocate in proportion to table size");
    }

    private static long MeasureOneAppend(SqliteIngestCursorStore store, string id)
    {
#pragma warning disable S1215
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
#pragma warning restore S1215
        var before = GC.GetTotalAllocatedBytes(precise: true);
        store.AppendLiveChunk("/g/a", new ChunkWrite(id, 0));
        return GC.GetTotalAllocatedBytes(precise: true) - before;
    }

    [Fact]
    public void MarkStopped_KeepsChunksLive()
    {
        var store = new SqliteIngestCursorStore(NewDatabase());
        store.MarkReading("/g/a", "hash", "/g");
        store.AppendLiveChunk("/g/a", new ChunkWrite("id0", 0));

        store.MarkStopped("/g/a", IngestStopKeys.MemoryBudget);

        var row = store.GetFile("/g/a") ?? throw new InvalidOperationException("row");
        row.State.Should().Be(IngestCursorState.Reading);
        row.MissingStatusKey.Should().Be(IngestStopKeys.MemoryBudget);
        store.AnyLivePath("id0").Should().BeTrue();
    }

    [Fact]
    public void FinishPath_ClearsAnyStopKey()
    {
        var store = new SqliteIngestCursorStore(NewDatabase());
        store.MarkReading("/g/a", "hash", "/g");
        store.MarkStopped("/g/a", IngestStopKeys.MemoryBudget);

        store.FinishPath("/g/a", "hash", "/g", "enc", [new ChunkWrite("id0", 0)]);

        var row = store.GetFile("/g/a") ?? throw new InvalidOperationException("row");
        row.State.Should().Be(IngestCursorState.Finished);
        row.MissingStatusKey.Should().BeEmpty();
    }

    [Fact]
    public void Migration_SetsUserVersion()
    {
        var path = NewDatabase();
        var store = new SqliteIngestCursorStore(path);
        store.AppendLiveChunk("/g/a", new ChunkWrite("id0", 0));

        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = CheckpointSql.ReadUserVersion;
        Convert.ToInt64(command.ExecuteScalar()).Should().Be(CheckpointSql.SchemaVersion);
    }
}
