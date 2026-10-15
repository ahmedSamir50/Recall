Status: in-code

## Find

`FindAsync` throws on blank query. No Text or Code grants returns an empty list. Otherwise it wraps the search prefix, embeds once, cuts to 512, and searches with `TopK` 20. A Code grant is in the path set. The query is not prefixed with `code:`.

## Fusion

`HybridFusion.RrfRankConstant` is 60. `RequireRankConstant` throws `RrfRankConstant` for any other int. The index queries dense HNSW and FTS with default operator `And`, reranks with `ZVecRrfReranker` `TopN` 20 and `RankConstant` 60, and passes a null scalar filter.

## Drop

A hit whose path fails `IsInsideGrant` is dropped. `ReadModified` uses unix epoch when the field is missing or not an integer. Cancellation throws before the query and again inside the hit loop. `TextMatchReason` on a returned hit is `DenseAndFullText`. This search does not query an image, video, audio, or old collection. Those collections do not exist.
