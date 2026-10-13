---
name: vectors-embeddings
description: >-
  Recall vectors and embeddings expert for EmbeddingGemma 2 ONNX, MRL tiering,
  ZVec.NET collections, HNSW, RaBitQ, and in-DB RRF. Use when changing
  encoders, dimensions, shards, insert, hybrid search, or index parameters.
disable-model-invocation: true
---

# Vectors and embeddings

You own representation, storage, and retrieval math. Read `.cursor/skills/recall-expert-harness/canon.md` first.

## Encoder

- EmbeddingGemma 2, ONNX, not a cloud API and not a fine-tune.
- Modes: text 270M (resident for text ingestion), text+vision 440M (lazy, images and video frames), full 740M (desktop, on demand, audio only, never resident). Mobile must not load 740M.
- Output is 768-d, mean-pooled, L2-normalized before cosine. Truncate with a mask; do not re-run the model to change tier.
- Floor is 128. v1 tiers are 768 (hot, pinned), 256 (warm), 128 (cold). 512 is a model capability, not a storage tier.
- One `InferenceSession` per loaded mode. Serialize `Run` per session with a `SemaphoreSlim(1,1)` on mobile. Pass `ReadOnlyMemory<float>` into ZVec; do not copy into extra `float[]` on the hot path.
- Execution providers: NNAPI on Android, CoreML on iOS and Mac Catalyst, DirectML on Windows. Fall back only when the EP is actually missing, and record it.

## Index

- Twelve collections: modality (`text`, `image`, `audio`, `video`) × tier (`hot`, `warm`, `cold`). Example: `items_image_hot`.
- ARM: float32 HNSW. `win-x64`: HNSW-RaBitQ. Refuse RaBitQ on ARM and DiskANN outside a future Linux-only decision. v1 does not use DiskANN or Vamana.
- Starting HNSW knobs from the plan, not eternal law: mobile `M=16`, `efConstruction=200`, `efSearch=64`; Windows RaBitQ `M=32`, `efConstruction=400`, `efSearch=96`. Changing `efSearch` requires a golden-query recall check from `retrieval-qa`.
- Hybrid query is dense + BM25 FTS + scalar filter + in-DB RRF with `k=60`. Outer fusion across tiers stays RRF. Do not add a learned ranker or an external reranker in v1. Weighted RRF is a later switch, not the default.
- Scalar payload includes modality, source path, file mtime, mime, dim tier, pinned flag, and the content-addressable id: SHA-256 over modality, source-content hash, and dim tier.
- Inserts and the op-log go through `SafeZvecHandle` dispose paths. A kill-9 mid-insert must reopen cleanly; that test belongs to `retrieval-qa`.

## Policy

- Hot window defaults to 30 days, warm through 180, then cold. Pin forces 768. Promote-on-access re-embeds from source when the file is still there; otherwise mark needs-upgrade.
- Do not mix dimensions inside one HNSW graph.
- Query path: prefix (see `nlp`) → 768-d → one search per active tier at that tier's width → RRF → top 20 with score components and provenance.

## Review stance

Block sub-128 truncation, a single mixed-dimension collection, mobile 740M, cloud embedders, and any path that skips RRF. Amend the 80 MB disk line (canon U1) and the 740M-on-phone latency rows (U9) instead of treating them as targets. Hand text payload quality to `nlp` and RSS arithmetic to `performance`.
