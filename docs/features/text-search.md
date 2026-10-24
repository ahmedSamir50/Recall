Status: in-code

## Find

`FindAsync` returns an empty list on blank query or when no Text, Code, or media grant is enabled. Otherwise it reconciles up to 64 pending vector rows when the checkpoints write lock is free, wraps the search prefix, embeds once, cuts to 512, and searches text with a ZVec filter on `path_live`, `encoder_hash`, and `grant_path`. Raw list cap is 64; returned file cap is 20. One card per live path after `Collapse.MaxRrf`. Cards show `RawNote` from the hit. A Code grant is in the grant-path set. The query is not prefixed with `code:`.

When text hits do not fill the file cap, Find appends BM25 hits from `MediaCollections.WordCollections` (`items_image`, `items_video`, `items_audio`, and their `_old` partners). Those lists fuse with `AgeRrf.Fuse` at the same rank constant as hybrid text search. A wordless picture does not appear. OCR words, sibling transcript text, and keywords on the media row are searchable through that words field; they are not copied into `items_text`. Find does not dense-search media collections on a text query.

## Fusion

`HybridFusion.RrfRankConstant` is 60. Dense and FTS use in-database RRF when there are no `bm25_only` rows; otherwise `HybridFusion.Fuse` merges dense and merged BM25 lists. `KeywordFold` drives FTS keys; the card shows the raw note. Picture and audio queries embed once, query long and `_old` collections with the matching cut width, and fuse with `AgeRrf.Fuse`.

## Drop

`SearchHitMarks.Apply` drops retired chunks and hits whose `GrantPath` is not in the active grant-path set. Missing alias paths keep `SourceMissing`. Find also shows skip lines for cloud-only files and generated dataset folders when those reports are non-empty.
