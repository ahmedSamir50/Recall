using Microsoft.Data.Sqlite;

namespace Recall.Core.Checkpoints;

public sealed class SqliteIngestCursorStore : IIngestCursorStore
{
    private readonly string databasePath;

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
        command.CommandText = "SELECT path, content_hash, state, grant_path FROM ingest_file WHERE path = @path";
        command.Parameters.AddWithValue("@path", path);
        using var reader = command.ExecuteReader();
        IngestFileRow? row = null;
        if (reader.Read())
        {
            row = new IngestFileRow(
                reader.GetString(0),
                reader.GetString(1),
                (IngestCursorState)reader.GetInt32(2),
                reader.GetString(3));
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
        command.CommandText = "SELECT path, content_hash, state, grant_path FROM ingest_file WHERE grant_path = @grant";
        command.Parameters.AddWithValue("@grant", grantPath);
        var list = new List<IngestFileRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new IngestFileRow(
                reader.GetString(0),
                reader.GetString(1),
                (IngestCursorState)reader.GetInt32(2),
                reader.GetString(3)));
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
            list.Add(new IngestChunkRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3) != 0,
                reader.GetInt32(4) != 0));
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
            row = new IngestChunkRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3) != 0,
                reader.GetInt32(4) != 0);
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

    public void MarkFinished(string path, string contentHash, string grantPath, IReadOnlyList<(string ContentId, int Ordinal)> chunks)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var fileCommand = connection.CreateCommand();
        fileCommand.Transaction = transaction;
        fileCommand.CommandText = "INSERT INTO ingest_file(path, content_hash, state, grant_path) VALUES(@path, @hash, @state, @grant) ON CONFLICT(path) DO UPDATE SET content_hash = @hash, state = @state, grant_path = @grant";
        fileCommand.Parameters.AddWithValue("@path", path);
        fileCommand.Parameters.AddWithValue("@hash", contentHash);
        fileCommand.Parameters.AddWithValue("@state", (int)IngestCursorState.Finished);
        fileCommand.Parameters.AddWithValue("@grant", grantPath);
        fileCommand.ExecuteNonQuery();
        if (chunks is not null)
        {
            using var deleteLive = connection.CreateCommand();
            deleteLive.Transaction = transaction;
            deleteLive.CommandText = "DELETE FROM ingest_chunk WHERE path = @path AND retired = @retired";
            deleteLive.Parameters.AddWithValue("@path", path);
            deleteLive.Parameters.AddWithValue("@retired", 0);
            deleteLive.ExecuteNonQuery();
            foreach (var chunk in chunks)
            {
                using var chunkCommand = connection.CreateCommand();
                chunkCommand.Transaction = transaction;
                chunkCommand.CommandText = "INSERT INTO ingest_chunk(content_id, path, ordinal, missing, retired) VALUES(@id, @path, @ordinal, @missing, @retired)";
                chunkCommand.Parameters.AddWithValue("@id", chunk.ContentId);
                chunkCommand.Parameters.AddWithValue("@path", path);
                chunkCommand.Parameters.AddWithValue("@ordinal", chunk.Ordinal);
                chunkCommand.Parameters.AddWithValue("@missing", 0);
                chunkCommand.Parameters.AddWithValue("@retired", 0);
                chunkCommand.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    public void RenamePath(string previousPath, string nextPath)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        string? hash = null;
        int state = (int)IngestCursorState.Reading;
        string? grant = null;
        using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText = "SELECT content_hash, state, grant_path FROM ingest_file WHERE path = @path";
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
        }

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO ingest_file(path, content_hash, state, grant_path) VALUES(@path, @hash, @state, @grant) ON CONFLICT(path) DO UPDATE SET content_hash = @hash, state = @state, grant_path = @grant";
            insert.Parameters.AddWithValue("@path", nextPath);
            insert.Parameters.AddWithValue("@hash", hash);
            insert.Parameters.AddWithValue("@state", state);
            insert.Parameters.AddWithValue("@grant", grant);
            insert.ExecuteNonQuery();
        }

        var liveIds = new List<string>();
        using (var selectChunks = connection.CreateCommand())
        {
            selectChunks.Transaction = transaction;
            selectChunks.CommandText = "SELECT content_id FROM ingest_chunk WHERE path = @path AND retired = @retired";
            selectChunks.Parameters.AddWithValue("@path", previousPath);
            selectChunks.Parameters.AddWithValue("@retired", 0);
            using var reader = selectChunks.ExecuteReader();
            while (reader.Read())
            {
                liveIds.Add(reader.GetString(0));
            }
        }

        foreach (var id in liveIds)
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE ingest_chunk SET path = @path WHERE content_id = @id";
            update.Parameters.AddWithValue("@path", nextPath);
            update.Parameters.AddWithValue("@id", id);
            update.ExecuteNonQuery();
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
            chunkCommand.CommandText = "UPDATE ingest_chunk SET missing = @missing WHERE path = @path";
            chunkCommand.Parameters.AddWithValue("@missing", 1);
            chunkCommand.Parameters.AddWithValue("@path", path);
            chunkCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void RetirePath(string path)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        EnsureSchema(connection, transaction);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE ingest_chunk SET retired = @retired WHERE path = @path";
        command.Parameters.AddWithValue("@retired", 1);
        command.Parameters.AddWithValue("@path", path);
        command.ExecuteNonQuery();
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
    }
}
