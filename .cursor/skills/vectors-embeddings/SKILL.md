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
- Modes are separate ONNX files: text, vision, audio. Download a file only when that type is enabled. Do not keep all three loaded. Do not run float16. We may generate and upload our own quants. ONNX Runtime runs float32, int8, and int4. Int8 is the almost-no-loss target. Published q4 is not the default. GGUF Q5 and Q6 do not run here.
- The stored embedding is the mean-pooled 768. The text trunk is 512 and the audio projection is 1536; those are not stored widths. Matryoshka keeps the leading 512 or 256 of that 768, then L2-normalizes the slice. A second forward pass is only for growing a vector whose tail was discarded.
- Stored widths are 768, 512, and 256. 128 is blocked. Text is 512 until a golden run clears 256. Old image, video, and audio move to a second collection at 512. The person picks 2, 5, or 7 years. A pin stays at the long width. Phone and desktop use the same widths.
- One `InferenceSession` per loaded mode. Serialize `Run` per session with a `SemaphoreSlim(1,1)` on mobile. Pass `ReadOnlyMemory<float>` into ZVec; do not copy into extra `float[]` on the hot path.
- Execution providers: NNAPI on Android, CoreML on iOS and Mac Catalyst, DirectML on Windows. Fall back only when the EP is actually missing, and record it.

## Index

- One width per collection. Text is one collection. Image, video, and audio may have a long collection and an old collection. Example: `items_image` and `items_image_old`. Do not leave both copies of the same item on disk.
- All four RIDs use plain HNSW with `QuantizeType` left `Undefined`. When it is set, `segment.cc` builds a float index and a second quantized index. Leave it off unless a benchmark shows query time about 2× or 3× faster. Do not create `ZVecHnswRabitqIndexParam`. Read `D:\A_S\AdamSystems.ZVec.NET` before claiming an index feature. DiskANN and Vamana stay out of v1.
- Starting HNSW knobs from the plan, not eternal law: `M=16`, `efConstruction=200`, `efSearch=64`. Changing `efSearch` requires a golden-query recall check from `retrieval-qa`.
- Hybrid query is dense + BM25 FTS + scalar filter + in-DB RRF with `k=60`. Merge the long and old collections with the same RRF. Do not add a learned ranker or an external reranker in v1. Weighted RRF is a later switch, not the default.
- Scalar payload includes modality, source path, file mtime, mime, dim tier, pinned flag, and the content-addressable id: SHA-256 over modality, source-content hash, and dim tier.
- Inserts and the op-log go through `SafeZvecHandle` dispose paths. A kill-9 mid-insert must reopen cleanly; that test belongs to `retrieval-qa`.

## Policy

- The age cutoff is 2, 5, or 7 years, chosen in the app, starting at 2. When an image, video, or audio item crosses it, cut the stored 768 to 512, re-normalize, delete the long row, and insert the short row. Pin stays long. Growing it back re-embeds from the file.
- Do not mix dimensions inside one HNSW graph.
- Query path: prefix on text only (see `nlp`) → one pooled 768 → cut and re-normalize per target collection → search each live collection → RRF → top 20 with score components and provenance.

## Review stance

Block a stored width of 128 or below, a single mixed-type collection, loading text plus vision plus audio at once, cloud embedders, and any path that skips RRF. Amend the 80 MB disk line (canon U1) and the 740M-on-phone latency rows (U9) instead of treating them as targets. Hand text payload quality to `nlp` and RSS arithmetic to `performance`.

## Panel duty

A `@recall:` or `@reacall:` plan is signed in the parent chat after one read of the shared harness files. Do not launch a subagent. Do not re-read development-rules, test-honesty, sign-off, canon, InitDocs, or the worktree. Apply this checklist to the plan already in context. Push back when the plan is wrong, inconsistent, or missing a risk. Agree only after a checked list. Return the sign-off. Do not rubber-stamp.
