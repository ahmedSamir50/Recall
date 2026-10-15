Status: in-code

## Collection

Collection name `items_text` under the vector root. Open if the directory exists, else `CreateAndOpen`. Vector `embedding`, `VectorFp32`, dimension 512, cosine HNSW, `M` 16, `EfConstruction` 200, `QuantizeType` `Undefined`. FTS field `raw_text`, tokenizer `Standard`, filter `Lowercase`. String fields `source_path`, `file_name`, `modified_ms` (unix milliseconds invariant culture). Doc id is the content id. `EfSearch` 64 is set on the query because the SDK default is 300. `TryInsert` returns false when `knownIds` has the id, when `Fetch` finds it, or when `Insert` is not success. Restart does not keep `knownIds`; `Fetch` is the disk check.

## Columns

Vector `embedding`. FTS field `raw_text`. String fields `source_path`, `file_name`, `modified_ms`.

## Absent columns

Absent columns, decided and not stored: modality, mime, pinned, stored width. A second quantized index is not created.
