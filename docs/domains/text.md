Status: in-code

## In words

Text turns the bytes of one allowed file into chunk ids. It wraps the model prefixes, splits at the 8192-token budget, hashes the content id, embeds, and cuts the vector to 512. A code file uses this same path. The ZVec collection that stores the row belongs to the vector index, not to this domain.

## Owns

Text owns prefixes, the 8192 split, content ids, and ingest of granted text. Types: `EmbeddingTaskPrefixes`, `TextChunkPlanner`, `ContentIdBuilder`, `TextIngestEngine`. A granted code file is ingested as text.

## Does not own

It does not own the `items_text` collection.

## Use cases

Use case: [ingest-granted-text](../use-cases/ingest-granted-text.md).
