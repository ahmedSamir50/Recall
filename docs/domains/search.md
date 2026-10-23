Status: in-code

## In words

Search answers one question from the grants the person already allowed. A text question is embedded once, cut to 512, and fused with keywords inside the text collection. A picture question is embedded once and stays in the image and video collections. Search drops anything outside the current grants and returns at most 20 files. It does not choose the HNSW parameters.

## Owns

Search owns one embed of the question, the cut to 512, the top 20, and the grant filter. Type: `TextSearchService`. Retired hits drop. Missing hits stay with `SourceMissing`. Checkpoint path replaces the stored path before the grant check.

## Does not own

It does not own HNSW parameters.

## Use cases

Use cases: [find-text](../use-cases/find-text.md), [open-result](../use-cases/open-result.md).
