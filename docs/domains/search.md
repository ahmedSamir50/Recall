Status: in-code

## In words

Search answers one question from the grants the person already allowed. A text question is embedded once, cut to 512, and fused with keywords inside the text collection. Matching words on picture and video rows are BM25-only hits (`TextMatchReason.WordsOnMedia`), fetched in a separate bucket capped at `RecallIndexCaps.GroupCap` (20) so a full text list does not crowd them out. A picture question embeds once, searches long and `_old` picture and video collections, fuses with `AgeRrf.Fuse`, then `PictureExactFile.Promote` places the finished ingested image whose bytes match the query JPEG at rank one when the dense index missed it. Card strength uses `MediaSearchHit.Similarity` (raw dense or BM25 score), not the RRF rank term. Text excerpts highlight folded query tokens with `Bm25Highlight`. An audio question embeds once and searches long and `_old` audio collections. Search drops anything outside the current grants. Results group into Text, Images, Videos, and Audio with at most 20 cards per group.

## Owns

Search owns one embed of the question, the cut to 512 on text, per-group caps, grant filtering, exact-byte picture promote, and BM25 excerpt marks. Types: `TextSearchService`, `PictureSearchService`, `AudioSearchService`, `AgeRrf`, `PictureExactFile`, `Bm25Highlight`, `MatchStrength`, `SearchGroups`. Retired hits drop. Missing hits stay with `SourceMissing`. Checkpoint path replaces the stored path before the grant check.

## Does not own

It does not own HNSW parameters or the age move that writes `_old` rows.

## Use cases

Use cases: [find-text](../use-cases/find-text.md), [open-result](../use-cases/open-result.md).