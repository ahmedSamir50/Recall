Status: in-code

## Owns

The vector index owns `items_text`, plain HNSW, the full-text field `fts_key`, and RRF rank constant 60. Open keeps a collection whose loaded schema has a `fts_key` field. A collection without that field is replaced on open and `ResetTextIngest` clears text checkpoint rows. Types: `ZVecTextVectorIndex`, `TextIndexOpenPolicy`, `RecallIndexPolicy`, `HybridFusion`.

## Does not own

It does not own the query string.

## Use cases

Use cases: [ingest-granted-text](../use-cases/ingest-granted-text.md), [find-text](../use-cases/find-text.md).
