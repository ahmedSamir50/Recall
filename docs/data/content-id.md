Status: in-code

## Preimage

`ContentIdBuilder.HashSourceContent` is lowercase hex SHA-256 of the UTF-8 raw text. Null text throws `ArgumentNullException`. `Build` preimage is `{int modality}|{sourceHash}|{int width}|{chunkOrdinal}`, then lowercase hex SHA-256. Blank source hash throws `ArgumentException`. Ordinal below 0 throws `ArgumentOutOfRangeException`. Text ingest calls `Build(Modality.Text, sourceHash, StoredWidth.W512, chunk.Ordinal)`. A repeated id skips insert and skips the op-log line.
