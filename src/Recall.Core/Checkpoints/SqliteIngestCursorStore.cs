using Microsoft.Data.Sqlite;

namespace Recall.Core.Checkpoints;

public sealed class SqliteIngestCursorStore : IIngestCursorStore
{
    private readonly string databasePath;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public SqliteIngestCursorStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path must not be blank.", nameof(databasePath));
        }

        this.databasePath = databasePath;
    }

    public IngestFileRow? GetFile(string path)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT path, content_hash, state, grant_path, encoder_hash FROM ingest_file WHERE path = @path";
        command.Parameters.AddWithValue("@path", path);
        using var reader = command.ExecuteReader();
        IngestFileRow? row = null;
        if (reader.Read())
        {
            row = ReadFileRow(reader);
        }

        transaction.Commit();
        return row;
    }

    public IReadOnlyList<IngestFileRow> ListFiles(string grantPath)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT path, content_hash, state, grant_path, encoder_hash FROM ingest_file WHERE grant_path = @grant";
        command.Parameters.AddWithValue("@grant", grantPath);
        var list = new List<IngestFileRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadFileRow(reader));
        }

        transaction.Commit();
        return list;
    }

    public IReadOnlyList<IngestChunkRow> ListChunks(string path)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT content_id, path, ordinal, missing, retired FROM ingest_chunk WHERE path = @path";
        command.Parameters.AddWithValue("@path", path);
        var list = new List<IngestChunkRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadChunkRow(reader));
        }

        transaction.Commit();
        return list;
    }

    public IngestChunkRow? FindChunk(string contentId)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT content_id, path, ordinal, missing, retired FROM ingest_chunk WHERE content_id = @id";
        command.Parameters.AddWithValue("@id", contentId);
        using var reader = command.ExecuteReader();
        IngestChunkRow? row = null;
        if (reader.Read())
        {
            row = ReadChunkRow(reader);
        }

        transaction.Commit();
        return row;
    }

    public void MarkReading(string path, string contentHash, string grantPath)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO ingest_file(path, content_hash, state, grant_path) VALUES(@path, @hash, @state, @grant) ON CONFLICT(path) DO UPDATE SET content_hash = @hash, state = @state, grant_path = @grant";
        command.Parameters.AddWithValue("@path", path);
        command.Parameters.AddWithValue("@hash", contentHash);
        command.Parameters.AddWithValue("@state", (int)IngestCursorState.Reading);
        command.Parameters.AddWithValue("@grant", grantPath);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public void MarkFinished(string path, string contentHash, string grantPath, string encoderHash)
    {
        FinishPath(path, contentHash, grantPath, encoderHash, []);
    }

    public void FinishPath(string path, string contentHash, string grantPath, string encoderHash, IReadOnlyList<ChunkWrite> chunks)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        ArgumentNullException.ThrowIfNull(chunks);
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSchema(connection, transaction);
            UpsertFinishedFile(connection, transaction, path, contentHash, grantPath, encoderHash);

            var newIds = new HashSet<string>(chunks.Select(c => c.ContentId), StringComparer.Ordinal);
            RetireAbsentAliases(connection, transaction, path, newIds);

            using (var deleteLive = connection.CreateCommand())
            {
                deleteLive.Transaction = transaction;
                deleteLive.CommandText = "DELETE FROM ingest_chunk WHERE path = @path AND retired = 0";
                deleteLive.Parameters.AddWithValue("@path", path);
                deleteLive.ExecuteNonQuery();
            }

            foreach (var chunk in chunks)
            {
                if (string.IsNullOrWhiteSpace(chunk.ContentId))
                {
                    throw new ArgumentException("Content id must not be blank.", nameof(chunks));
                }

                if (!ChunkRowExists(connection, transaction, chunk.ContentId))
                {
                    InsertChunkRow(connection, transaction, chunk.ContentId, path, chunk.Ordinal);
                }

                AddAliasInTransaction(connection, transaction, chunk.ContentId, path);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool AnyLivePath(string contentId)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM ingest_chunk_path WHERE content_id = @id AND missing = 0 AND retired = 0 LIMIT 1";
        command.Parameters.AddWithValue("@id", contentId);
        var result = command.ExecuteScalar();
        transaction.Commit();
        return result is not null;
    }

    public bool AnyOtherLivePath(string contentId, string path)
    {
        if (string.IsNullOrWhiteSpace(contentId) || string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Content id and path must not be blank.");
        }

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM ingest_chunk_path WHERE content_id = @id AND path <> @path AND missing = 0 AND retired = 0 LIMIT 1";
        command.Parameters.AddWithValue("@id", contentId);
        command.Parameters.AddWithValue("@path", path);
        var result = command.ExecuteScalar();
        transaction.Commit();
        return result is not null;
    }

    public IReadOnlyList<string> ListRetiredIds(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT content_id FROM ingest_chunk_path WHERE path = @path AND retired = 1";
        command.Parameters.AddWithValue("@path", path);
        var list = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(reader.GetString(0));
        }

        transaction.Commit();
        return list;
    }

    public void AddAlias(string contentId, string path)
    {
        if (string.IsNullOrWhiteSpace(contentId) || string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Content id and path must not be blank.");
        }

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSchema(connection, transaction);
            AddAliasInTransaction(connection, transaction, contentId, path);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void MoveLiveAliases(string fromContentId, string toContentId)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSchema(connection, transaction);
            var paths = new List<string>();
            using (var select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                select.CommandText = "SELECT path FROM ingest_chunk_path WHERE content_id = @fromId AND missing = 0 AND retired = 0";
                select.Parameters.AddWithValue("@fromId", fromContentId);
                using var reader = select.ExecuteReader();
                while (reader.Read())
                {
                    paths.Add(reader.GetString(0));
                }
            }

            using (var retire = connection.CreateCommand())
            {
                retire.Transaction = transaction;
                retire.CommandText = "UPDATE ingest_chunk_path SET retired = 1 WHERE content_id = @fromId AND missing = 0 AND retired = 0";
                retire.Parameters.AddWithValue("@fromId", fromContentId);
                retire.ExecuteNonQuery();
            }

            foreach (var path in paths)
            {
                AddAliasInTransaction(connection, transaction, toContentId, path);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void PutBm25Only(string contentId, string path, string rawText)
    {
        if (string.IsNullOrWhiteSpace(contentId) || string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Content id and path must not be blank.");
        }

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT OR REPLACE INTO bm25_only(content_id, path, raw_text) VALUES(@id, @path, @raw)";
        command.Parameters.AddWithValue("@id", contentId);
        command.Parameters.AddWithValue("@path", path);
        command.Parameters.AddWithValue("@raw", rawText);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public IReadOnlyList<Bm25OnlyRow> ListBm25Only()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT content_id, path, raw_text FROM bm25_only";
        var list = new List<Bm25OnlyRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Bm25OnlyRow(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        transaction.Commit();
        return list;
    }

    public IReadOnlyList<IngestAliasRow> ListLiveAliases(string contentId)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT content_id, path, missing, retired FROM ingest_chunk_path WHERE content_id = @id AND missing = 0 AND retired = 0";
        command.Parameters.AddWithValue("@id", contentId);
        var list = new List<IngestAliasRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new IngestAliasRow(reader.GetString(0), reader.GetString(1), reader.GetInt32(2) != 0, reader.GetInt32(3) != 0));
        }

        transaction.Commit();
        return list;
    }

    public IReadOnlyList<string> ListPending(int limit)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT content_id FROM pending_reconcile ORDER BY content_id LIMIT @limit";
        command.Parameters.AddWithValue("@limit", limit);
        var list = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(reader.GetString(0));
        }

        transaction.Commit();
        return list.OrderBy(id => id, StringComparer.Ordinal).Take(limit).ToList();
    }

    public int PendingCount()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM pending_reconcile";
        var result = command.ExecuteScalar();
        transaction.Commit();
        return Convert.ToInt32(result);
    }

    public void RemovePending(IReadOnlyList<string> contentIds)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        foreach (var id in contentIds)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM pending_reconcile WHERE content_id = @id";
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void EnqueuePending(string contentId)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT OR IGNORE INTO pending_reconcile(content_id) VALUES(@id)";
        command.Parameters.AddWithValue("@id", contentId);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public void ResetTextIngest()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        foreach (var sql in new[]
        {
            "DELETE FROM ingest_file",
            "DELETE FROM ingest_chunk",
            "DELETE FROM ingest_chunk_path",
            "DELETE FROM bm25_only",
            "DELETE FROM pending_reconcile",
            "DELETE FROM grant_scan",
        })
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public bool TryEnterWrite(out CheckpointLease? lease)
    {
        if (!writeLock.Wait(0))
        {
            lease = null;
            return false;
        }

        lease = CreateWriteLease();
        return true;
    }

    public CheckpointLease EnterWrite()
    {
        writeLock.Wait();
        return CreateWriteLease();
    }

    public void RenamePath(string previousPath, string nextPath)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        string? hash = null;
        int state = (int)IngestCursorState.Reading;
        string? grant = null;
        string? encoderHash = null;
        using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText = "SELECT content_hash, state, grant_path, encoder_hash FROM ingest_file WHERE path = @path";
            select.Parameters.AddWithValue("@path", previousPath);
            using var reader = select.ExecuteReader();
            if (!reader.Read())
            {
                transaction.Commit();
                return;
            }

            hash = reader.GetString(0);
            state = reader.GetInt32(1);
            grant = reader.GetString(2);
            encoderHash = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
        }

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO ingest_file(path, content_hash, state, grant_path, encoder_hash) VALUES(@path, @hash, @state, @grant, @encoder) ON CONFLICT(path) DO UPDATE SET content_hash = @hash, state = @state, grant_path = @grant, encoder_hash = @encoder";
            insert.Parameters.AddWithValue("@path", nextPath);
            insert.Parameters.AddWithValue("@hash", hash);
            insert.Parameters.AddWithValue("@state", state);
            insert.Parameters.AddWithValue("@grant", grant);
            insert.Parameters.AddWithValue("@encoder", encoderHash ?? string.Empty);
            insert.ExecuteNonQuery();
        }

        using (var updateChunks = connection.CreateCommand())
        {
            updateChunks.Transaction = transaction;
            updateChunks.CommandText = "UPDATE ingest_chunk SET path = @next WHERE path = @prev";
            updateChunks.Parameters.AddWithValue("@next", nextPath);
            updateChunks.Parameters.AddWithValue("@prev", previousPath);
            updateChunks.ExecuteNonQuery();
        }

        using (var updateAliases = connection.CreateCommand())
        {
            updateAliases.Transaction = transaction;
            updateAliases.CommandText = "UPDATE ingest_chunk_path SET path = @next WHERE path = @prev";
            updateAliases.Parameters.AddWithValue("@next", nextPath);
            updateAliases.Parameters.AddWithValue("@prev", previousPath);
            updateAliases.ExecuteNonQuery();
        }

        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM ingest_file WHERE path = @path";
            delete.Parameters.AddWithValue("@path", previousPath);
            delete.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void MarkMissing(string path)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using (var fileCommand = connection.CreateCommand())
        {
            fileCommand.Transaction = transaction;
            fileCommand.CommandText = "UPDATE ingest_file SET state = @state WHERE path = @path";
            fileCommand.Parameters.AddWithValue("@state", (int)IngestCursorState.Missing);
            fileCommand.Parameters.AddWithValue("@path", path);
            fileCommand.ExecuteNonQuery();
        }

        using (var chunkCommand = connection.CreateCommand())
        {
            chunkCommand.Transaction = transaction;
            chunkCommand.CommandText = "UPDATE ingest_chunk SET missing = 1 WHERE path = @path";
            chunkCommand.Parameters.AddWithValue("@path", path);
            chunkCommand.ExecuteNonQuery();
        }

        using (var aliasCommand = connection.CreateCommand())
        {
            aliasCommand.Transaction = transaction;
            aliasCommand.CommandText = "UPDATE ingest_chunk_path SET missing = 1 WHERE path = @path";
            aliasCommand.Parameters.AddWithValue("@path", path);
            aliasCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void RetirePath(string path)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "UPDATE ingest_chunk SET retired = 1 WHERE path = @path";
            command.Parameters.AddWithValue("@path", path);
            command.ExecuteNonQuery();
        }

        using (var alias = connection.CreateCommand())
        {
            alias.Transaction = transaction;
            alias.CommandText = "UPDATE ingest_chunk_path SET retired = 1 WHERE path = @path";
            alias.Parameters.AddWithValue("@path", path);
            alias.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void MarkGrantReading(string grantPath)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO grant_scan(grant_path, state) VALUES(@grant, @state) ON CONFLICT(grant_path) DO UPDATE SET state = @state";
        command.Parameters.AddWithValue("@grant", grantPath);
        command.Parameters.AddWithValue("@state", (int)IngestCursorState.Reading);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public void MarkGrantFinished(string grantPath)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO grant_scan(grant_path, state) VALUES(@grant, @state) ON CONFLICT(grant_path) DO UPDATE SET state = @state";
        command.Parameters.AddWithValue("@grant", grantPath);
        command.Parameters.AddWithValue("@state", (int)IngestCursorState.Finished);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public bool IsGrantFinished(string grantPath)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT state FROM grant_scan WHERE grant_path = @grant";
        command.Parameters.AddWithValue("@grant", grantPath);
        var value = command.ExecuteScalar();
        transaction.Commit();
        if (value is null || value == DBNull.Value)
        {
            return false;
        }

        return Convert.ToInt32(value) == (int)IngestCursorState.Finished;
    }

    private CheckpointLease CreateWriteLease()
    {
        var acquired = true;
        return new CheckpointLease(
            () =>
            {
                if (acquired)
                {
                    writeLock.Release();
                    acquired = false;
                }
            },
            () =>
            {
                if (acquired)
                {
                    writeLock.Release();
                    acquired = false;
                }
            });
    }

    private static IngestFileRow ReadFileRow(SqliteDataReader reader)
    {
        var encoder = reader.FieldCount > 4 && !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty;
        return new IngestFileRow(
            reader.GetString(0),
            reader.GetString(1),
            (IngestCursorState)reader.GetInt32(2),
            reader.GetString(3),
            encoder);
    }

    private static IngestChunkRow ReadChunkRow(SqliteDataReader reader) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetInt32(3) != 0,
            reader.GetInt32(4) != 0);

    private static void UpsertFinishedFile(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string path,
        string contentHash,
        string grantPath,
        string encoderHash)
    {
        using var fileCommand = connection.CreateCommand();
        fileCommand.Transaction = transaction;
        fileCommand.CommandText = "INSERT INTO ingest_file(path, content_hash, state, grant_path, encoder_hash) VALUES(@path, @hash, @state, @grant, @encoder) ON CONFLICT(path) DO UPDATE SET content_hash = @hash, state = @state, grant_path = @grant, encoder_hash = @encoder";
        fileCommand.Parameters.AddWithValue("@path", path);
        fileCommand.Parameters.AddWithValue("@hash", contentHash);
        fileCommand.Parameters.AddWithValue("@state", (int)IngestCursorState.Finished);
        fileCommand.Parameters.AddWithValue("@grant", grantPath);
        fileCommand.Parameters.AddWithValue("@encoder", encoderHash);
        fileCommand.ExecuteNonQuery();
    }

    private static void RetireAbsentAliases(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string path,
        HashSet<string> newIds)
    {
        var existing = new List<string>();
        using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText = "SELECT content_id FROM ingest_chunk_path WHERE path = @path AND retired = 0";
            select.Parameters.AddWithValue("@path", path);
            using var reader = select.ExecuteReader();
            while (reader.Read())
            {
                existing.Add(reader.GetString(0));
            }
        }

        foreach (var id in existing.Where(id => !newIds.Contains(id)))
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE ingest_chunk_path SET retired = 1 WHERE content_id = @id AND path = @path";
            update.Parameters.AddWithValue("@id", id);
            update.Parameters.AddWithValue("@path", path);
            update.ExecuteNonQuery();
        }
    }

    private static bool ChunkRowExists(SqliteConnection connection, SqliteTransaction transaction, string contentId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM ingest_chunk WHERE content_id = @id LIMIT 1";
        command.Parameters.AddWithValue("@id", contentId);
        return command.ExecuteScalar() is not null;
    }

    private static void InsertChunkRow(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string contentId,
        string path,
        int ordinal)
    {
        using var chunkCommand = connection.CreateCommand();
        chunkCommand.Transaction = transaction;
        chunkCommand.CommandText = "INSERT INTO ingest_chunk(content_id, path, ordinal, missing, retired) VALUES(@id, @path, @ordinal, 0, 0)";
        chunkCommand.Parameters.AddWithValue("@id", contentId);
        chunkCommand.Parameters.AddWithValue("@path", path);
        chunkCommand.Parameters.AddWithValue("@ordinal", ordinal);
        chunkCommand.ExecuteNonQuery();
    }

    private static void AddAliasInTransaction(SqliteConnection connection, SqliteTransaction transaction, string contentId, string path)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO ingest_chunk_path(content_id, path, missing, retired) VALUES(@id, @path, 0, 0) ON CONFLICT(content_id, path) DO UPDATE SET missing = 0, retired = 0";
        command.Parameters.AddWithValue("@id", contentId);
        command.Parameters.AddWithValue("@path", path);
        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        return connection;
    }

    private static void EnsureSchema(SqliteConnection connection, SqliteTransaction transaction)
    {
        using (var file = connection.CreateCommand())
        {
            file.Transaction = transaction;
            file.CommandText = CheckpointSql.CreateFileTable;
            file.ExecuteNonQuery();
        }

        using (var chunk = connection.CreateCommand())
        {
            chunk.Transaction = transaction;
            chunk.CommandText = CheckpointSql.CreateChunkTable;
            chunk.ExecuteNonQuery();
        }

        using (var index = connection.CreateCommand())
        {
            index.Transaction = transaction;
            index.CommandText = CheckpointSql.CreateChunkPathIndex;
            index.ExecuteNonQuery();
        }

        using (var grant = connection.CreateCommand())
        {
            grant.Transaction = transaction;
            grant.CommandText = CheckpointSql.CreateGrantTable;
            grant.ExecuteNonQuery();
        }

        using (var pathTable = connection.CreateCommand())
        {
            pathTable.Transaction = transaction;
            pathTable.CommandText = CheckpointSql.CreateChunkPathTable;
            pathTable.ExecuteNonQuery();
        }

        using (var pending = connection.CreateCommand())
        {
            pending.Transaction = transaction;
            pending.CommandText = CheckpointSql.CreatePendingTable;
            pending.ExecuteNonQuery();
        }

        using (var bm25Only = connection.CreateCommand())
        {
            bm25Only.Transaction = transaction;
            bm25Only.CommandText = CheckpointSql.CreateBm25OnlyTable;
            bm25Only.ExecuteNonQuery();
        }

        if (!HasColumn(connection, transaction, "ingest_file", "encoder_hash"))
        {
            using var alter = connection.CreateCommand();
            alter.Transaction = transaction;
            alter.CommandText = "ALTER TABLE ingest_file ADD COLUMN encoder_hash TEXT NOT NULL DEFAULT ''";
            alter.ExecuteNonQuery();
        }

        CopyChunkPairsToChunkPath(connection, transaction);
    }

    private static bool HasColumn(SqliteConnection connection, SqliteTransaction transaction, string tableName, string columnName)
    {
        if (!string.Equals(tableName, "ingest_file", StringComparison.Ordinal))
        {
            throw new ArgumentException("Unknown table name.", nameof(tableName));
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA table_info(ingest_file)";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(1);
            if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void CopyChunkPairsToChunkPath(SqliteConnection connection, SqliteTransaction transaction)
    {
        var existing = new HashSet<string>(StringComparer.Ordinal);
        using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText = "SELECT content_id, path FROM ingest_chunk_path";
            using var reader = select.ExecuteReader();
            while (reader.Read())
            {
                existing.Add(reader.GetString(0) + "\u0001" + reader.GetString(1));
            }
        }

        using var pairSelect = connection.CreateCommand();
        pairSelect.Transaction = transaction;
        pairSelect.CommandText = "SELECT content_id, path, missing, retired FROM ingest_chunk";
        var rows = new List<(string ContentId, string Path, bool Missing, bool Retired)>();
        using (var reader = pairSelect.ExecuteReader())
        {
            while (reader.Read())
            {
                rows.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetInt32(2) != 0,
                    reader.GetInt32(3) != 0));
            }
        }

        foreach (var row in rows)
        {
            var key = row.ContentId + "\u0001" + row.Path;
            if (existing.Add(key))
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT OR IGNORE INTO ingest_chunk_path(content_id, path, missing, retired) VALUES(@id, @path, @missing, @retired)";
                command.Parameters.AddWithValue("@id", row.ContentId);
                command.Parameters.AddWithValue("@path", row.Path);
                command.Parameters.AddWithValue("@missing", row.Missing ? 1 : 0);
                command.Parameters.AddWithValue("@retired", row.Retired ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }
    }
}
