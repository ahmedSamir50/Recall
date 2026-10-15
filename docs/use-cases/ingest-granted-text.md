Status: in-code

## Actor

The ingest call.

## Trigger

`IngestGrantedTextAsync`, watcher, foreground scan, and Index now.

## Path

Skip a grant only when both Text and Code are off, `MarkGrantReading`, read each document, `IngestDocumentAsync`, then `MarkGrantFinished`. `IngestDocumentAsync` skips embed when `IngestCursorPolicy.Decide` returns `Skip`; otherwise `MarkReading`, embed, then `MarkFinished`.

## Failure

Blank content throws, cancellation throws, a repeated content id skips insert and skips the log, and a throw after `MarkReading` leaves `Reading` for resume. Finished same hash skips embed.

## Domains

Scope, Text, Vector index, Sync. Feature page: [text-ingest](../features/text-ingest.md).
