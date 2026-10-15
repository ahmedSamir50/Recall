Status: in-code

## Actor

The ingest call.

## Trigger

`IngestGrantedTextAsync`. There is no watcher trigger.

## Path

Skip a grant without text, read each document, plan chunks, embed once, cut to 512, insert, and append the op-log only when insert returns true.

## Failure

Blank content throws, cancellation throws, and a repeated content id skips insert and skips the log.

## Domains

Scope, Text, Vector index, Sync. Feature page: [text-ingest](../features/text-ingest.md).
