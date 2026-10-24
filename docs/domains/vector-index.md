Status: in-code

## In words

The vector index is the on-device store for the vectors. Text has one collection, `items_text`, at width 512, with a keyword field beside the vector. Image, video, and audio have long collections at width 768 and matching `_old` collections at 512 for aged media. A query fuses the dense list and the keyword list inside the database with rank constant 60. Opening a text collection that has no keyword field replaces that collection and clears the text checkpoint rows.

## Owns

The vector index owns `items_text`, plain HNSW, the full-text field `fts_key`, and RRF rank constant 60. Media owns `items_image`, `items_video`, `items_audio`, `items_image_old`, `items_video_old`, and `items_audio_old`. Open keeps a collection whose loaded schema has a `fts_key` field. A collection without that field is replaced on open and `ResetTextIngest` clears text checkpoint rows. Types: `ZVecTextVectorIndex`, `ZVecMediaVectorIndex`, `TextIndexOpenPolicy`, `RecallIndexPolicy`, `HybridFusion`, `MediaCollections`.

## Does not own

It does not own the query string or the recall gate that decides when a row moves to `_old`.

## Use cases

Use cases: [ingest-granted-text](../use-cases/ingest-granted-text.md), [find-text](../use-cases/find-text.md).
