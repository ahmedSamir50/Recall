Status: in-code

## In words

Search answers one question from the grants the person already allowed. A text question is embedded once, cut to 512, and fused with keywords inside the text collection; matching words on picture, video, and audio rows append through BM25 on those collections when the file cap allows. A picture question embeds once, searches long and `_old` picture and video collections, and fuses with `AgeRrf.Fuse`. An audio question does the same in audio collections. Search drops anything outside the current grants and returns at most 20 files. It does not choose the HNSW parameters.

## Owns

Search owns one embed of the question, the cut to 512 on text, the top 20, and the grant filter. Types: `TextSearchService`, `PictureSearchService`, `AudioSearchService`, `AgeRrf`. Retired hits drop. Missing hits stay with `SourceMissing`. Checkpoint path replaces the stored path before the grant check.

## Does not own

It does not own HNSW parameters or the age move that writes `_old` rows.

## Use cases

Use cases: [find-text](../use-cases/find-text.md), [open-result](../use-cases/open-result.md).
