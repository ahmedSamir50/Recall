Status: in-code

## Walk

`IngestGrantedTextAsync` walks every grant. A grant with only pictures or media still runs the image, video, and audio legs when those kinds are on and the matching model is Ready. Text and code use `ReadAsync`; images use `ListImagesAsync`; video and audio use `ListAudioAndVideoAsync`. One thrown text file is marked Missing and later files and media legs still run.

## File

That method checks the storable width, hashes the raw text, and plans chunks. For each chunk it throws if cancellation is requested. A dense chunk embeds the prefixed text, cuts to 512, builds the content id, and inserts. A non-code span that fails `NumericSpanPolicy.IsDense` is not embedded and is not a zero vector. Its span text is inserted into `bm25_only` inside `FinishPath`. A throw before that commit leaves the file `Reading` and no `bm25_only` row. A code file stays a dense vector. Op-log append runs only when `TryInsert` returns true.

## Extensions

Extensions in appsettings `TextIngest`: `.txt,.md,.markdown,.csv,.json,.xml,.html,.htm`. Code files use `IsSafeCodeText`. Watcher, foreground scan, window-open ingest, and header retry call ingest. Finished same hash skips embed for text and for vision when the bytes hash matches.
