using Microsoft.Data.Sqlite;
using Recall.Core.Indexing;
using Recall.Core.Vocabulary;

namespace Recall.Core.Checkpoints;

public sealed class SqliteIngestCursorStore : IIngestCursorStore
{
    private readonly string databasePath;
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private int writeDepth;
    private int writeOwner;
    private int spanReady;

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
        command.CommandText = "SELECT content_id, path, ordinal, missing, retired, page_start, page_end, char_start, char_end FROM ingest_chunk WHERE path = @path";
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
        command.CommandText = "SELECT content_id, path, ordinal, missing, retired, page_start, page_end, char_start, char_end FROM ingest_chunk WHERE content_id = @id";
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
        FinishPath(path, contentHash, grantPath, encoderHash, [], null);
    }

    public void FinishPath(string path, string contentHash, string grantPath, string encoderHash, IReadOnlyList<ChunkWrite> chunks, IReadOnlyList<Bm25OnlyRow>? bm25Only = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        ArgumentNullException.ThrowIfNull(chunks);
        bm25Only ??= [];
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSchema(connection, transaction);
            UpsertFinishedFile(connection, transaction, path, contentHash, grantPath, encoderHash);

            foreach (var row in bm25Only)
            {
                if (string.IsNullOrWhiteSpace(row.ContentId) || string.IsNullOrWhiteSpace(row.Path))
                {
                    throw new ArgumentException("Content id and path must not be blank.");
                }

                using var bm25Command = connection.CreateCommand();
                bm25Command.Transaction = transaction;
                bm25Command.CommandText = "INSERT OR REPLACE INTO bm25_only(content_id, path, raw_text) VALUES(@id, @path, @raw)";
                bm25Command.Parameters.AddWithValue("@id", row.ContentId);
                bm25Command.Parameters.AddWithValue("@path", row.Path);
                bm25Command.Parameters.AddWithValue("@raw", row.RawText);
                bm25Command.ExecuteNonQuery();
            }

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
                    InsertChunkRow(
                        connection,
                        transaction,
                        chunk.ContentId,
                        path,
                        chunk.Ordinal,
                        chunk.PageStart,
                        chunk.PageEnd,
                        chunk.CharStart,
                        chunk.CharEnd);
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

    public void ReplaceDocumentPatches(string path, IReadOnlyList<ChunkWrite> patches)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        ArgumentNullException.ThrowIfNull(patches);
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSchema(connection, transaction);
            var newIds = new HashSet<string>(patches.Select(patch => patch.ContentId), StringComparer.Ordinal);
            RetireAbsentAliases(connection, transaction, path, newIds);
            using (var deleteLive = connection.CreateCommand())
            {
                deleteLive.Transaction = transaction;
                deleteLive.CommandText = "DELETE FROM ingest_chunk WHERE path = @path AND retired = 0";
                deleteLive.Parameters.AddWithValue("@path", path);
                deleteLive.ExecuteNonQuery();
            }

            foreach (var patch in patches)
            {
                if (string.IsNullOrWhiteSpace(patch.ContentId))
                {
                    throw new ArgumentException("Content id must not be blank.", nameof(patches));
                }

                if (!ChunkRowExists(connection, transaction, patch.ContentId))
                {
                    InsertChunkRow(
                        connection,
                        transaction,
                        patch.ContentId,
                        path,
                        patch.Ordinal,
                        patch.PageStart,
                        patch.PageEnd,
                        patch.CharStart,
                        patch.CharEnd);
                }

                AddAliasInTransaction(connection, transaction, patch.ContentId, path);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void ReplaceOcrPages(string path, IReadOnlyList<int> pageNumbers)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        ArgumentNullException.ThrowIfNull(pageNumbers);
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSchema(connection, transaction);
            using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM ocr_page WHERE path = @path";
                delete.Parameters.AddWithValue("@path", path);
                delete.ExecuteNonQuery();
            }

            foreach (var pageNumber in pageNumbers)
            {
                using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO ocr_page(path, page_number) VALUES (@path, @page)";
                insert.Parameters.AddWithValue("@path", path);
                insert.Parameters.AddWithValue("@page", pageNumber);
                insert.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public IReadOnlyList<int> ListOcrPages(string path)
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
        command.CommandText = "SELECT page_number FROM ocr_page WHERE path = @path ORDER BY page_number";
        command.Parameters.AddWithValue("@path", path);
        var pages = new List<int>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            pages.Add(reader.GetInt32(0));
        }

        transaction.Commit();
        return pages;
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
            MoveLiveAliasesInTransaction(connection, transaction, fromContentId, toContentId);
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
            "DELETE FROM ocr_page",
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
        if (!TryAcquireWrite())
        {
            lease = null;
            return false;
        }

        lease = CreateWriteLease();
        return true;
    }

    public CheckpointLease EnterWrite()
    {
        AcquireWrite();
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

    public void UpsertMediaFile(string path, Modality modality)
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
        command.CommandText = "INSERT INTO media_file(path, modality, width) VALUES(@path, @modality, @width) ON CONFLICT(path, modality) DO NOTHING";
        command.Parameters.AddWithValue("@path", path);
        command.Parameters.AddWithValue("@modality", (int)modality);
        command.Parameters.AddWithValue("@width", MediaCollections.Width);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public MediaFileRow? FindMediaFile(string path, Modality modality)
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
        command.CommandText = "SELECT path, modality, width FROM media_file WHERE path = @path AND modality = @modality";
        command.Parameters.AddWithValue("@path", path);
        command.Parameters.AddWithValue("@modality", (int)modality);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            transaction.Commit();
            return null;
        }

        var row = new MediaFileRow(reader.GetString(0), (Modality)reader.GetInt32(1), reader.GetInt32(2));
        transaction.Commit();
        return row;
    }

    public IReadOnlyList<MediaFileRow> ListMediaFiles()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT path, modality, width FROM media_file ORDER BY path, modality";
        var list = new List<MediaFileRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new MediaFileRow(reader.GetString(0), (Modality)reader.GetInt32(1), reader.GetInt32(2)));
        }

        transaction.Commit();
        return list;
    }

    public void SetMediaWidth(string path, Modality modality, int width)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        if (width is not (MediaCollections.Width or MediaCollections.OldWidth))
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE media_file SET width = @width WHERE path = @path AND modality = @modality";
        command.Parameters.AddWithValue("@path", path);
        command.Parameters.AddWithValue("@modality", (int)modality);
        command.Parameters.AddWithValue("@width", width);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public void CommitAgeCut(string fromContentId, string toContentId)
    {
        if (string.IsNullOrWhiteSpace(fromContentId) || string.IsNullOrWhiteSpace(toContentId))
        {
            throw new ArgumentException("Content id must not be blank.");
        }

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureSchema(connection, transaction);
            MoveLiveAliasesInTransaction(connection, transaction, fromContentId, toContentId);
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT OR REPLACE INTO age_cut_pending(long_id, short_id) VALUES(@longId, @shortId)";
            command.Parameters.AddWithValue("@longId", fromContentId);
            command.Parameters.AddWithValue("@shortId", toContentId);
            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public IReadOnlyList<AgeCutPending> ListAgeCuts()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT long_id, short_id FROM age_cut_pending ORDER BY long_id";
        var list = new List<AgeCutPending>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new AgeCutPending(reader.GetString(0), reader.GetString(1)));
        }

        transaction.Commit();
        return list;
    }

    public void CompleteAgeCut(string longId)
    {
        if (string.IsNullOrWhiteSpace(longId))
        {
            throw new ArgumentException("Content id must not be blank.");
        }

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM age_cut_pending WHERE long_id = @longId";
            delete.Parameters.AddWithValue("@longId", longId);
            delete.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public bool IsPinned(string path)
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
        command.CommandText = "SELECT 1 FROM ingest_pin WHERE path = @path LIMIT 1";
        command.Parameters.AddWithValue("@path", path);
        var pinned = command.ExecuteScalar() is not null;
        transaction.Commit();
        return pinned;
    }

    public void AddPin(string path)
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
        command.CommandText = "INSERT OR IGNORE INTO ingest_pin(path) VALUES(@path)";
        command.Parameters.AddWithValue("@path", path);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public void RemovePin(string path)
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
        command.CommandText = "DELETE FROM ingest_pin WHERE path = @path";
        command.Parameters.AddWithValue("@path", path);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public void StagePinResume(string path, string longId, string shortId)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be blank.", nameof(path));
        }

        if (string.IsNullOrWhiteSpace(longId) || string.IsNullOrWhiteSpace(shortId))
        {
            throw new ArgumentException("Content id must not be blank.");
        }

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT OR REPLACE INTO pin_resume(path, long_id, short_id) VALUES(@path, @longId, @shortId)";
        command.Parameters.AddWithValue("@path", path);
        command.Parameters.AddWithValue("@longId", longId);
        command.Parameters.AddWithValue("@shortId", shortId);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public IReadOnlyList<PinResumeRow> ListPinResumes()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT path, long_id, short_id FROM pin_resume ORDER BY path";
        var list = new List<PinResumeRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new PinResumeRow(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        transaction.Commit();
        return list;
    }

    public void CompletePinResume(string path)
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
        command.CommandText = "DELETE FROM pin_resume WHERE path = @path";
        command.Parameters.AddWithValue("@path", path);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    private static void MoveLiveAliasesInTransaction(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string fromContentId,
        string toContentId)
    {
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
    }

    private CheckpointLease CreateWriteLease()
    {
        var acquired = true;
        return new CheckpointLease(
            () =>
            {
                if (acquired)
                {
                    ReleaseWrite();
                    acquired = false;
                }
            },
            () =>
            {
                if (acquired)
                {
                    ReleaseWrite();
                    acquired = false;
                }
            });
    }

    private void AcquireWrite()
    {
        var id = Environment.CurrentManagedThreadId;
        if (writeDepth > 0 && writeOwner == id)
        {
            writeDepth++;
            return;
        }

        writeLock.Wait();
        writeOwner = id;
        writeDepth = 1;
    }

    private bool TryAcquireWrite()
    {
        var id = Environment.CurrentManagedThreadId;
        if (writeDepth > 0 && writeOwner == id)
        {
            writeDepth++;
            return true;
        }

        if (!writeLock.Wait(0))
        {
            return false;
        }

        writeOwner = id;
        writeDepth = 1;
        return true;
    }

    private void ReleaseWrite()
    {
        writeDepth--;
        if (writeDepth > 0)
        {
            return;
        }

        writeOwner = 0;
        writeDepth = 0;
        writeLock.Release();
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
            reader.GetInt32(4) != 0,
            reader.GetInt32(5),
            reader.GetInt32(6),
            reader.GetInt32(7),
            reader.GetInt32(8));

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
        int ordinal,
        int pageStart,
        int pageEnd,
        int charStart,
        int charEnd)
    {
        using var chunkCommand = connection.CreateCommand();
        chunkCommand.Transaction = transaction;
        chunkCommand.CommandText = "INSERT INTO ingest_chunk(content_id, path, ordinal, missing, retired, page_start, page_end, char_start, char_end) VALUES(@id, @path, @ordinal, 0, 0, @pageStart, @pageEnd, @charStart, @charEnd)";
        chunkCommand.Parameters.AddWithValue("@id", contentId);
        chunkCommand.Parameters.AddWithValue("@path", path);
        chunkCommand.Parameters.AddWithValue("@ordinal", ordinal);
        chunkCommand.Parameters.AddWithValue("@pageStart", pageStart);
        chunkCommand.Parameters.AddWithValue("@pageEnd", pageEnd);
        chunkCommand.Parameters.AddWithValue("@charStart", charStart);
        chunkCommand.Parameters.AddWithValue("@charEnd", charEnd);
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

    private void EnsureSchema(SqliteConnection connection, SqliteTransaction transaction)
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

        using (var ocrPage = connection.CreateCommand())
        {
            ocrPage.Transaction = transaction;
            ocrPage.CommandText = CheckpointSql.CreateOcrPageTable;
            ocrPage.ExecuteNonQuery();
        }

        using (var mediaFile = connection.CreateCommand())
        {
            mediaFile.Transaction = transaction;
            mediaFile.CommandText = CheckpointSql.CreateMediaFileTable;
            mediaFile.ExecuteNonQuery();
        }

        using (var ageCut = connection.CreateCommand())
        {
            ageCut.Transaction = transaction;
            ageCut.CommandText = CheckpointSql.CreateAgeCutPendingTable;
            ageCut.ExecuteNonQuery();
        }

        using (var ingestPin = connection.CreateCommand())
        {
            ingestPin.Transaction = transaction;
            ingestPin.CommandText = CheckpointSql.CreateIngestPinTable;
            ingestPin.ExecuteNonQuery();
        }

        using (var pinResume = connection.CreateCommand())
        {
            pinResume.Transaction = transaction;
            pinResume.CommandText = CheckpointSql.CreatePinResumeTable;
            pinResume.ExecuteNonQuery();
        }

        if (!HasColumn(connection, transaction, "PRAGMA table_info(ingest_file)", "encoder_hash"))
        {
            using var alter = connection.CreateCommand();
            alter.Transaction = transaction;
            alter.CommandText = "ALTER TABLE ingest_file ADD COLUMN encoder_hash TEXT NOT NULL DEFAULT ''";
            alter.ExecuteNonQuery();
        }

        EnsureChunkSpanColumns(connection, transaction);

        CopyChunkPairsToChunkPath(connection, transaction);
    }

    private void EnsureChunkSpanColumns(SqliteConnection connection, SqliteTransaction transaction)
    {
        if (Volatile.Read(ref spanReady) != 0)
        {
            return;
        }

        AcquireWrite();
        try
        {
            if (spanReady != 0)
            {
                return;
            }

            AddSpanColumn(connection, transaction, "page_start", "ALTER TABLE ingest_chunk ADD COLUMN page_start INTEGER NOT NULL DEFAULT 0");
            AddSpanColumn(connection, transaction, "page_end", "ALTER TABLE ingest_chunk ADD COLUMN page_end INTEGER NOT NULL DEFAULT 0");
            AddSpanColumn(connection, transaction, "char_start", "ALTER TABLE ingest_chunk ADD COLUMN char_start INTEGER NOT NULL DEFAULT 0");
            AddSpanColumn(connection, transaction, "char_end", "ALTER TABLE ingest_chunk ADD COLUMN char_end INTEGER NOT NULL DEFAULT 0");

            spanReady = 1;
        }
        finally
        {
            ReleaseWrite();
        }
    }

    private static void AddSpanColumn(SqliteConnection connection, SqliteTransaction transaction, string columnName, string alter)
    {
        if (HasColumn(connection, transaction, "PRAGMA table_info(ingest_chunk)", columnName))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = alter;
        command.ExecuteNonQuery();
    }

    private static bool HasColumn(SqliteConnection connection, SqliteTransaction transaction, string tableInfo, string columnName)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = tableInfo;
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
