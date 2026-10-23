Status: in-code

## In words

The vector index is the on-device store for the vectors. Text has one collection, `items_text`, at width 512, with a keyword field beside the vector. Image, video, and audio have their own collections at width 768. A query fuses the dense list and the keyword list inside the database with rank constant 60. Opening a text collection that has no keyword field replaces that collection and clears the text checkpoint rows.

## Owns

The vector index owns `items_text`, plain HNSW, the full-text field `fts_key`, and RRF rank constant 60. Open keeps a collection whose loaded schema has a `fts_key` field. A collection without that field is replaced on open and `ResetTextIngest` clears text checkpoint rows. Types: `ZVecTextVectorIndex`, `TextIndexOpenPolicy`, `RecallIndexPolicy`, `HybridFusion`.

## Does not own

It does not own the query string.

## Use cases

Use cases: [ingest-granted-text](../use-cases/ingest-granted-text.md), [find-text](../use-cases/find-text.md).
