Status: in-code

## Collection

Collection name `items_text` under the vector root. A missing directory is created. A directory whose vectors include `fts_key` is opened. A directory without that vector is destroyed and created again with the current schema, then `IIngestCursorStore.ResetTextIngest` clears `checkpoints.db` so the foreground diff reads granted files again. Source files stay on disk.

Vector `embedding`, `VectorFp32`, dimension 512, cosine HNSW, `M` 16, `EfConstruction` 200, `QuantizeType` `Undefined`. Full-text vector `fts_key`, tokenizer `Standard`, filter `Lowercase`. Scalar fields `source_path`, `file_name`, `modified_ms` (unix milliseconds invariant culture), `path_live`, `encoder_hash`, `grant_path`, `ordinal`, and `raw_text`. Doc id is the content id. `EfSearch` 64 is set on the query because the SDK default is 300. `TryInsert` returns false when `knownIds` has the id, when `Fetch` finds it, or when `Insert` is not success. Restart does not keep `knownIds`; `Fetch` is the disk check.

## Columns

Vector `embedding`. Full-text vector `fts_key`. Scalar fields `source_path`, `file_name`, `modified_ms`, `path_live`, `encoder_hash`, `grant_path`, `ordinal`, `raw_text`.

## Absent columns

Absent columns, decided and not stored: modality, mime, pinned, stored width. A second quantized index is not created. `raw_text` is not the full-text vector.
