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

    public const string CreateChunkPathTable =
        "CREATE TABLE IF NOT EXISTS ingest_chunk_path(content_id TEXT NOT NULL, path TEXT NOT NULL, missing INTEGER NOT NULL, retired INTEGER NOT NULL, PRIMARY KEY(content_id, path))";

    public const string CreatePendingTable =
        "CREATE TABLE IF NOT EXISTS pending_reconcile(content_id TEXT PRIMARY KEY)";

    public const string CreateBm25OnlyTable =
        "CREATE TABLE IF NOT EXISTS bm25_only(content_id TEXT PRIMARY KEY, path TEXT NOT NULL, raw_text TEXT NOT NULL)";

    public const string AddEncoderHashColumn =
        "ALTER TABLE ingest_file ADD COLUMN IF NOT EXISTS encoder_hash TEXT";

    public const string AddChunkPageStartColumn =
        "ALTER TABLE ingest_chunk ADD COLUMN IF NOT EXISTS page_start INTEGER NOT NULL DEFAULT 0";

    public const string AddChunkPageEndColumn =
        "ALTER TABLE ingest_chunk ADD COLUMN IF NOT EXISTS page_end INTEGER NOT NULL DEFAULT 0";

    public const string AddChunkCharStartColumn =
        "ALTER TABLE ingest_chunk ADD COLUMN IF NOT EXISTS char_start INTEGER NOT NULL DEFAULT 0";

    public const string AddChunkCharEndColumn =
        "ALTER TABLE ingest_chunk ADD COLUMN IF NOT EXISTS char_end INTEGER NOT NULL DEFAULT 0";

    public const string CreateOcrPageTable =
        "CREATE TABLE IF NOT EXISTS ocr_page(path TEXT NOT NULL, page_number INTEGER NOT NULL, PRIMARY KEY(path, page_number))";

    public const string EnsureSchema =
        CreateFileTable + ";" + CreateChunkTable + ";" + CreateChunkPathIndex + ";" + CreateGrantTable + ";" + CreateChunkPathTable + ";" + CreatePendingTable + ";" + CreateBm25OnlyTable + ";" + CreateOcrPageTable;
}
