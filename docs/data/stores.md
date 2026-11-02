Status: in-code

## On device

MAUI Preferences keys `recall.onboarding.complete`, `recall.scope.grants`, `recall.preferences.ageCutoff`, `recall.locale`, `recall.device.record`, `recall.theme`, `recall.skips.user`. App-data `models/text`, `zvec/items_text`, `oplog/yyyy-MM-dd.log`, optional `appsettings.secrets.json`.

## Checkpoints

`checkpoints.db` is on device. It stores paths, hashes, and states, not file text. No copied source archive. `ResetTextIngest` deletes those rows when a stale `items_text` collection is replaced.

## Not on device

No server.

Failure: a missing preferences value uses the store default named on the [preferences](preferences.md) page.

## Schema version and write batching

`EnsureReady` runs the schema once per store instance, and `PRAGMA user_version` guards the one-time backfill that copies `(content_id, path)` pairs from `ingest_chunk` into `ingest_chunk_path`. `ix_ingest_chunk_path_path` indexes `ingest_chunk_path(path)`, so a path-filtered read no longer scans the table.

`AppendLiveChunks` writes a whole batch of chunks and aliases in one transaction. `AppendLiveChunk` delegates to it with a single item, so the two paths cannot drift.
