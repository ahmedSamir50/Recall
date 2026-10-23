Status: in-code for the stub and forbidden for transport.

## In words

Sync transport is forbidden in v1. The flag stays off. When a new text row is inserted, or a content id is deleted, a line is appended to a local log so a later design could replay it. Nothing reads that log to send it.

## Owns

Sync owns `SyncFeature.Enabled` false and the local insert log. Types: `SyncFeature`, `LocalOpLogWriter`, empty `ISyncAdapter`.

## Does not own

It does not own a pairing use case. No use case sends the log.
