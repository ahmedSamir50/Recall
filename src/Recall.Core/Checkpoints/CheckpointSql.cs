namespace Recall.Core.Checkpoints;

public static class CheckpointSql
{
    public const string FileName = "checkpoints.db";

    public const string CreateFileTable =
        "CREATE TABLE IF NOT EXISTS ingest_file(path TEXT PRIMARY KEY, content_hash TEXT NOT NULL, state INTEGER NOT NULL, grant_path TEXT NOT NULL)";

    public const string CreateChunkTable =
        "CREATE TABLE IF NOT EXISTS ingest_chunk(content_id TEXT PRIMARY KEY, path TEXT NOT NULL, ordinal INTEGER NOT NULL, missing INTEGER NOT NULL, retired INTEGER NOT NULL)";

    public const string CreateChunkPathIndex =
        "CREATE INDEX IF NOT EXISTS ix_ingest_chunk_path ON ingest_chunk(path)";

    public const string CreateGrantTable =
        "CREATE TABLE IF NOT EXISTS grant_scan(grant_path TEXT PRIMARY KEY, state INTEGER NOT NULL)";

    public const string EnsureSchema =
        CreateFileTable + ";" + CreateChunkTable + ";" + CreateChunkPathIndex + ";" + CreateGrantTable;
}
