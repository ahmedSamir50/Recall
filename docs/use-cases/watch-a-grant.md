Status: in-code

## Actor

The watcher while Recall runs.

## Trigger

Created, modified, renamed, or deleted inside a granted folder.

## Path

On create and on resume, ForegroundScanService walks IGrantedTextReader and ForegroundGrantDiff compares the hashes with checkpoints.db. A parent directory on Windows, Mac, or a real Android path also reports through one FileSystemWatcher into ScopeChangeBuffer (at most 64 paths). ScopeChangeApplier updates checkpoints.db, keeps the vector, and appends op-log Delete on delete. iPhone and a content:// grant use that same diff.

## Failure

A 65th new path sets a rescan. A throw leaves `Reading` for resume. No timer, no background iPhone watch, no `FileObserver`. The loop does not wait one second.

## Domains

Scope, Text. Feature page: [watchers](../features/watchers.md).
