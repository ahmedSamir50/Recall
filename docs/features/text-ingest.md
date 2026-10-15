Status: in-code

## Walk

`IngestGrantedTextAsync` skips a grant only when both `Text` and `Code` are off. For each other grant it reads `GrantedTextDocument` values and calls `IngestFileAsync`.

## File

That method checks the storable width, hashes the raw text, plans chunks, and for each chunk throws if cancellation is requested, embeds the prefixed text, cuts to 512, builds the content id, and inserts. Op-log append runs only when `TryInsert` returns true.

## Extensions

Extensions in appsettings `TextIngest`: `.txt,.md,.markdown,.csv,.json,.xml,.html,.htm`. Code files use `IsSafeCodeText`. There is no watcher. A file added later is not picked up until something calls ingest again. That gap is phase 3, status decided-absent.
