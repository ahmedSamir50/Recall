Status: in-code

## Owns

The vector index owns `items_text`, plain HNSW, the full-text field, and RRF rank constant 60. Types: `ZVecTextVectorIndex`, `RecallIndexPolicy`, `HybridFusion`.

## Does not own

It does not own the query string.

## Use cases

Use cases: [ingest-granted-text](../use-cases/ingest-granted-text.md), [find-text](../use-cases/find-text.md).
