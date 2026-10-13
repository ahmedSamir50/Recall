# Recall — Implementation Plan

> **On-device multimodal second brain.** EmbeddingGemma 2 (ONNX) + ZVec.NET (in-process) on a single .NET MAUI Blazor Hybrid codebase that ships natively to **Android, iOS, Windows, and macOS Catalyst**.
> v1 = **local-only**; device-to-device sync and a web client are **explicitly deferred** (sync design is included; web is replaced by the MAUI desktop shell).

---

## Document Control

| Field | Value |
|---|---|
| Document | Recall App — Implementation Plan |
| Version | 1.2 (revised — tiny LLM sidecar killed; replaced with zero-LLM deterministic cluster labeler; all other v1.1 scope refinements retained) |
| Status | Living document — owned by the Recall Tech Lead |
| Author | Recall Architecture (see RACI §13) |
| Audience | Engineering, ML, QA, Product |
| Source | Derived from `IdeaTank_Embeddinggemma_2_ZVecNET.md` §3 (the flagship `Recall` super-app) |
| Companion doc | `Recall_App_Technical_Details.md` |
| Last reviewed | 2026-10-09 (v1.2) |

---

## Table of Contents

1. Executive Summary
2. Goals & Non-Goals
3. Architecture Overview
4. Platform Strategy — MAUI Blazor Hybrid Unified
5. Project Structure
6. Phase Plan & Milestones (Gantt)
7. Performance Budget
8. Make / Buy Matrix
9. Risk Register
10. Test Strategy
11. Data Corpus Specification
12. Sync Design (Deferred — Design Only)
13. RACI & Engineering Org
14. Exit Criteria
15. Appendix: Glossary & References

---

## 1. Executive Summary

`Recall` is the on-device multimodal "second brain" application from the IdeaTank portfolio (§3 of the source brief). It captures text, images, audio, and video frames from a user's digital life, embeds them locally with **EmbeddingGemma 2** (the 740M modular multimodal encoder from Google DeepMind), indexes them in **ZVec.NET** (the .NET SDK for Alibaba's in-process ZVec vector database), and answers cross-modal, multilingual queries with sub-50 ms latency on a mid-tier mobile device — completely offline, with zero network egress and zero PII exposure.

The strategic thesis (IdeaTank §2) is that this pairing is a **moat**: hosted vector databases cannot compete on the privacy / latency / cost axes that Recall lives on, because by design they require a network round-trip and a server-side embedder. Recall collapses both halves of the retrieval stack onto the device: the embedder is an ONNX Runtime session loaded in-process, and the vector DB is a ZVec.NET instance living inside the same .NET process. No server, no daemon, no network call, no Pinecone bill, no PII egress.

### 1.1 What changed from the original IdeaTank brief

The original brief (§3.2) proposed **two separate code paths**: a MAUI Blazor Hybrid app for mobile, and a Native AOT single-binary for desktop. After scope clarification with the user, v1 collapses these into **one MAUI Blazor Hybrid codebase** that ships natively to four targets: `android-arm64`, `ios-arm64`, `win-x64`, and `maccatalyst-arm64`. The Razor UI is shared across all four; the native shell handles windowing, lifecycle, and platform integration (filesystem scope watcher, secure storage, idle detection).

This is a stronger architecture than the original two-path proposal because:

1. **One codebase, four native shells** — UI, capture pipeline, query pipeline, MRL tiering policy, and the ZVec.NET collection schema are written once. Platform-specific code is reduced to ~5% of the codebase (the platform integration services behind interfaces).
2. **Proven path on mobile** — the existing ZVec.NET MAUI Blazor Hybrid sample already demonstrates the offline-RAG path on `android-arm64` and `ios-arm64` RIDs. Reusing this path de-risks the most fragile part of the original brief (mobile vector search at scale).
3. **No second build pipeline** — Native AOT for desktop was attractive for cold-start but added a second publish profile, second trimmer config, second set of AOT-compatibility warnings to chase. MAUI Blazor Hybrid's desktop host on Windows and macOS Catalyst already gives us a single-binary desktop app with millisecond startup, which is the property we actually wanted.
4. **No web client** — the originally proposed Blazor Server admin dashboard is replaced by the MAUI Blazor Hybrid desktop shell. ZVec.NET has no Blazor WebAssembly RID (IdeaTank §1.2), so a web client was always going to be server-side Blazor; that is strictly worse than a desktop shell for a privacy-first local app, because it requires a localhost HTTP listener.

### 1.1.1 What changed in v1.1 (this revision)

Six user-driven clarifications reshaped v1 scope after the original v1.0 draft:

1. **Scope-only capture model (replaces ambient capture)** — the original v1.0 draft proposed background capture of clipboard, camera roll, screen OCR, and audio. The user rejected this as a "spying" UX failure. v1 now uses a **user-scoped filesystem watcher model**: the user explicitly grants the app access to specific paths + per-path file types; the app indexes within that scope using FS watchers. No scheduled scans; no OS-signal capture (clipboard/camera/screen/mic). Ambient capture is deferred to a separate Epic (§15.5).
2. **.NET 10 LTS** (replaces .NET 9 STS) — v1 targets `net10.0` everywhere; .NET 9 goes EOL May 2026, before v1 GA.
3. **BM25 explicit** in the hybrid query (the design already had dense ANN + FTS + RRF; the docs now call out BM25 by name).
4. **740M full multimodal encoder dropped from mobile v1** — Android LMKill + 1.5 GB RSS budget. Mobile v1 ships text (270M) + text+vision (440M) only. Audio ingestion and audio queries are desktop-only for v1; mobile audio waits for an INT8-quantized ONNX export of the 740M encoder and 8 GB baseline devices (v1.5+).
5. **Modality × tier sharding** — instead of 3 collections (hot/warm/cold of mixed modality), v1 uses 4 × 3 = 12 collections (modality × tier). Per-modality index tuning (HNSW `M` differs for text vs image). Time-sharding deferred to v1.1.
6. **ONNX assets = downloadable, not bundled** — the app ships small (~50 MB binary). First-run downloads the 270M text encoder (~540 MB compressed to ~270 MB on the wire). The 440M vision encoder is on-demand (download when user first queries with an image). The 740M full encoder is desktop-only, on-demand.
7. **SQLite `checkpoints.db`** added for ingestion cursors + background job state + crash-recovery cursor.
8. **Tiny LLM sidecar KILLED in v1.2** — the original v1.1 plan called for a ≥1B-param LLM sidecar (Granite 3B-06L or Qwen2.5-1.5B, INT4, ~600 MB) for one use case: nightly cluster title + summary generation. After user pressure-test ("1.5B / 600 MB for a single feature is wasteful — find a lightweight way or kill it"), this is **replaced with a zero-LLM deterministic cluster labeler** (TF-IDF + temporal + count, formatted in the cluster's natural language + user's locale). ~30 lines of C#, no model, no download, no opt-in. The cluster browser UI (Epic 2) still ships in v1.5; the labels it displays are computed by the deterministic labeler. See Tech §16 Appendix D.

### 1.2 What is explicitly deferred

Two scopes from the original IdeaTank §3.2 brief are deferred from v1:

1. **Device-to-device sync** (originally: "peer-to-peer or end-to-end-encrypted CRDT between your devices"). v1 ships **sync-disabled**. The full sync design — CRDT semantics for vector IDs, end-to-end-encrypted key hierarchy, and the transport options (BLE, Wi-Fi Direct, LAN mDNS, Tor-like overlay) — is included in **§12 of this document as a design reference**. v1.5 will implement the design as written, without architectural rework, because the v1 schema (content-addressable vector IDs + append-only op-log) is engineered today to be sync-ready.

2. **Web version** — none. The MAUI Blazor Hybrid desktop shell replaces it. The original brief's web dashboard was the weakest of the three deployment targets (it required a server-side Blazor host, which contradicts the local-only thesis), and it has been dropped.

### 1.3 v1 scope, concretely

- **One MAUI Blazor Hybrid codebase** → four native shells (`android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64`); targets `net10.0`.
- **Scope-only capture model**: user explicitly grants the app access to specific paths + per-path file types; app uses filesystem watchers within scope. No OS-signal capture (clipboard/camera/screen/mic) in v1. **iOS caveat**: iOS sandbox forbids true background FS watching; iOS v1 uses "scan on app foreground" (when the user opens Recall, the app diffs scoped paths against the last-seen list and ingests new/changed items).
- **Modality coverage in v1**: text + images + sampled video frames share the same 768-d space (uses 270M text encoder + 440M text+vision encoder). **Audio is desktop-only in v1** (needs the 740M full encoder; deferred on mobile pending INT8 quantized ONNX export).
- **Modular encoder loading**: text-only 270M resident for ingestion of text streams; 440M text+vision on-demand for image ingestion and image queries.
- **Hybrid MRL tiering** (recency tiers + user pinning + auto-promote-on-access).
- **Modality × tier sharding**: 4 modalities × 3 tiers = 12 ZVec.NET collections; per-modality HNSW tuning.
- **Hybrid BM25 + dense + scalar + in-DB RRF** in one query.
- **ONNX assets = downloadable, not bundled**. First-run downloads the 270M text encoder; the 440M vision encoder downloads on-demand when the user first invokes image search.
- **SQLite `checkpoints.db`** for ingestion cursors, background job state, crash-recovery cursor.
- **Zero network egress. No server. No cloud. No telemetry. No accounts.**
- **Crash-safe in-process DB** (ZVec.NET `SafeZvecHandle` lifecycle).
- **Three milestones over 90 days**: M1 (text+image POC on Android, days 0–30), M2 (iOS + Windows + macOS Catalyst bring-up + video frames, days 30–60), M3 (production hardening, days 60–90).

The plan that follows is sequenced to ship M1 in 30 days on Android only, then graduate the same codebase to iOS + Windows + macOS Catalyst in M2, then harden for production in M3. The 90-day exit is a v1.0 release candidate that runs on all four targets, handles 100k mixed-modal items per device (text + image + video frames on mobile; + audio on desktop), and answers p95 queries in under 50 ms on a Pixel 6a-class Android device and under 20 ms on a 2024-era x86 laptop.

---

## 2. Goals & Non-Goals

### 2.1 v1 Goals

| ID | Goal | Measurable success criterion |
|---|---|---|
| G1 | **One codebase → four native shells** | ≥ 90% of source shared across `android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64`; targets `net10.0` |
| G2 | **Sub-50 ms p95 query latency at 100k vectors on mid-tier mobile** | Pixel 6a-class device, 100k mixed-modal items (text + image + video frames), p95 < 50 ms for the golden query set (§11) |
| G3 | **Sub-20 ms p95 on desktop** | 2024-era x86 laptop (8-core, 16 GB), same 100k corpus + audio on desktop, p95 < 20 ms |
| G4 | **Scope-only capture model** | User explicitly grants paths + file types; app uses FS watchers within scope; no OS-signal capture (clipboard/camera/screen/mic); zero ambient capture in v1 |
| G5 | **Modular encoder loading (mobile)** | Text-only ingestion (270M, always resident) and text+vision query (440M, on-demand) selectable at runtime; measured RSS delta confirms the load boundary. **740M full multimodal encoder is desktop-only in v1** (mobile deferred pending INT8 export) |
| G6 | **Hybrid MRL tiering + modality × tier shards** | Recency tiers + user pinning + auto-promote-on-access all live by end of M2; 12 ZVec.NET collections (4 modalities × 3 tiers); storage math at 100k items ≤ 150 MB on mobile (HNSW), ≤ 60 MB on desktop (RaBitQ) |
| G7 | **Zero network egress** | Network traffic capture during a 60-minute dogfood session = 0 bytes to non-local IPs (allow OS push only if user opts in; first-run ONNX download is the only allowed egress) |
| G8 | **Crash-safe DB** | Power-cut test (kill -9 mid-insert) on a 1k-item batch produces zero corruption; DB reopens cleanly; SQLite `checkpoints.db` resumes ingestion cursor |
| G9 | **Multilingual query parity** | Golden query set (§11) issued in EN, AR, ZH, HI, FR; recall@10 within ±5 percentage points across languages |
| G10 | **Sync-ready schema** | Vector IDs are content-addressable; op-log is append-only; sync interface exists; sync is disabled at the feature flag level only |
| G11 | **Hybrid BM25 + dense + scalar + in-DB RRF** | FTS uses BM25 explicitly; in-DB RRF reranker fuses dense + FTS ranks; bypassing RRF is forbidden by code review gate |

### 2.2 v1 Non-Goals (parked for v1.5+)

- **Ambient OS-signal capture (clipboard / camera roll / screen OCR / microphone)** — deferred to a separate Epic (§15.5). v1 uses **scope-only FS watcher** capture. Ambient capture lands only behind explicit user opt-in, post-v1.5.
- **Mobile audio ingestion and queries** — deferred pending (a) INT8-quantized ONNX export of the 740M full multimodal encoder, and (b) 8 GB baseline mobile devices. v1 mobile = text + image + video frames only. Audio is desktop-only in v1.
- **Device-to-device sync** — full design included (§12), no code in v1. v1.5 implements the design as written.
- **Web client** — none. The MAUI Blazor Hybrid desktop shell replaces it.
- **LLM side-car / chat interface** — `Recall.Chat` is parked per IdeaTank §7. The retrieval layer (v1) supports a future chat layer with no changes.
- **Tiny LLM sidecar (any size, for any use case)** — **KILLED in v1.2** (anti-goal). Cluster labels are produced by a zero-LLM deterministic labeler (TF-IDF + temporal + count). Query rewriting, NL→filter, snippet extraction, auto-tagging, and intent parsing are handled by EmbeddingGemma 2 + small multilingual NER + rule-based parser. No LLM is shipped, downloaded, or invoked at any point in v1 or v1.5. See Tech §16 Appendix D for the deterministic labeler design.
- **Obsidian-style knowledge graph view** — v1.5 pure UX. Data (cluster IDs, similarity edges) computed in v1; viz UI ships in v1.5.
- **Time-based sharding** (monthly buckets) — v1.1. v1 uses modality × tier shards only.
- **Generative summarization** — parked. v1 returns ranked artifacts, not generated text.
- **Server / cloud / multi-tenant deployment** — out of scope; this is a local-first application.
- **Real-time streaming classification / moderation** — deliberately NOT using ZVec.NET for this per IdeaTank §6; a vector DB adds latency you don't need.
- **Cross-tenant shared search** — hosted vector DB still wins here; partner instead of competing per IdeaTank §6.
- **Blazor WebAssembly target** — ZVec.NET does not ship a WASM RID (IdeaTank §1.2).
- **DiskANN on Windows / macOS** — Linux-only upstream; HNSW or Vamana only on these targets in v1.
- **HNSW-RaBitQ on ARM** — upstream ISA is x86_64 + AVX2 only; plain HNSW on mobile.

### 2.3 Anti-Goals (explicitly forbidden)

These are not "deferred" — they are forbidden in v1 and likely in all future versions because they violate the local-first thesis.

- **Any code path that sends raw vectors or PII to a server.** The embedder and the DB live in-process. There is no client–server split. (The only allowed egress is the first-run ONNX model download, which fetches model weights from Hugging Face CDN; no user data, no vectors, no PII leaves the device.)
- **Any cloud embedding API call** (OpenAI, Cohere, Voyage, etc.). The embedder is EmbeddingGemma 2 via ONNX Runtime, full stop.
- **Any cloud vector DB dependency.** Pinecone, Weaviate, Qdrant Cloud, etc. are out.
- **Ambient OS-signal capture in v1** (clipboard watcher, camera roll observer, screen OCR background agent, microphone background recording). v1 is **scope-only FS watcher**; ambient capture is a separate Epic (§15.5) gated behind explicit user opt-in.
- **Scheduled / nightly re-scan jobs in v1.** The FS watcher is the only ingest trigger. (Nightly tier sweep + nightly clustering are separate background jobs that touch only in-DB vectors, not the filesystem.)
- **Bundling ONNX assets in the app binary.** Model weights are downloadable on first run + on-demand; the base APK / app bundle is ≤ 50 MB.
- **Truncation of the encoder output below 128d.** MRL floor is 128d; below this, MTEB scores collapse non-linearly and the recall budget is broken. The MRL tiering policy (§6 of Technical Details) refuses to truncate below this floor.
- **HNSW-RaBitQ on ARM devices.** The upstream ISA is x86_64 + AVX2 only; using it on ARM silently falls back to a slow path. We refuse the configuration explicitly.
- **DiskANN on Windows or macOS.** Linux-only upstream; we refuse the configuration explicitly.
- **Storing vectors at full 768d for archival data.** This defeats the MRL storage density win that makes 100k-item mobile recall feasible. The MRL policy demotes archival data to 128d or 256d.
- **Background ingestion at 740M full multimodal encoder mode on mobile.** v1 mobile does not load the 740M encoder at all. Desktop may use 740M for user-initiated audio queries only; never resident.
- **Tiny LLM sidecar of any size, for any use case.** KILLED in v1.2 — replaced with a zero-LLM deterministic cluster labeler (TF-IDF + temporal + count, formatted in the cluster's natural language + user's locale). The original v1.1 plan called for a ≥1B-param LLM (~600 MB INT4) for one nightly batch feature; the user's pressure-test correctly identified this as wasteful. No LLM is shipped, downloaded, or invoked at any point in v1 or v1.5.

### 2.4 Why these anti-goals matter

Recall's defensibility rests on three properties: privacy (no PII egress), latency (in-process DB), and density (MRL storage). Any of the anti-goals above breaks one of these. The strategic insight in IdeaTank §2 is that hosted vector DBs are blocked by these axes by design — they cannot sell into regulated verticals (health, legal, finance, personal) because their architecture requires PII to leave the device. Recall's architecture is the inverse: PII is structurally incapable of leaving the device because there is no egress path. The anti-goals above are the enforcement mechanism for that thesis.

---

## 3. Architecture Overview

### 3.1 Component diagram (logical)

```
+-------------------------------------------------------------------------+
|                            MAUI Blazor Hybrid Shell                     |
|   (android-arm64 | ios-arm64 | win-x64 | maccatalyst-arm64)             |
|                                                                         |
|   +-----------------------------+      +-----------------------------+  |
|   |  Razor UI (Blazor WebView)  |      |  Platform Integration Svc  |  |
|   |  - Search view               |      |  - IClipboardService       |  |
|   |  - Capture view              |      |  - ICameraRollService      |  |
|   |  - Result cards              |      |  - IScreenCaptureService   |  |
|   |  - Pin / tier manager        |      |  - IAudioRecorderService    |  |
|   |  - Sync panel (disabled v1)  |      |  - IVideoFrameSamplerSvc   |  |
|   +-----------------------------+      +-----------------------------+  |
|                  |                                     |
|                  v                                     v
|   +----------------------------------------------------------------------+
|   |                    Recall Core (shared .NET library)                 |
|   |                                                                      |
|   |   +------------------+      +------------------+                    |
|   |   | Capture Pipeline |      |  Query Pipeline  |                    |
|   |   |  - modality route|      |  - parse + prefix|                    |
|   |   |  - encoder mode  |      |  - hybrid search |                    |
|   |   |  - MRL tiering   |      |  - RRF rerank    |                    |
|   |   |  - op-log append |      |  - rank + provenance                  |
|   |   +------------------+      +------------------+                    |
|   |              |                       ^                              |
|   |              v                       |                              |
|   |   +--------------------------------------------+                    |
|   |   |   MRL Tiering Policy                      |                    |
|   |   |   (recency decay + user pin + promote)    |                    |
|   |   +--------------------------------------------+                    |
|   |              |                       |                              |
|   |              v                       v                              |
|   |   +------------------+      +------------------+                    |
|   |   | ONNX Runtime     |      | ZVec.NET        |                    |
|   |   | InferenceSession |      | Collection      |                    |
|   |   | - text 270M      |      | - HNSW / RaBitQ |                    |
|   |   | - +vision 440M   |      | - FTS            |                    |
|   |   | - +audio 740M    |      | - scalar filter |                    |
|   |   +------------------+      +------------------+                    |
|   |                                                                      |
|   +----------------------------------------------------------------------+
|                                  |
|                                  v
|   +----------------------------------------------------------------------+
|   |   Local Storage (in-process, in-app sandbox)                        |
|   |   - ZVec.NET DB file (durable, crash-safe)                          |
|   |   - ONNX model assets (text/vision/audio encoders, select-loadable) |
|   |   - Op-log (append-only, sync-ready)                                |
|   |   - Original media (referenced by path, not re-stored)              |
|   +----------------------------------------------------------------------+
+-------------------------------------------------------------------------+
                                  |
                                  v
                          [ NO NETWORK EGRESS ]
                          (sync transport disabled in v1;
                           design in §12)
```

### 3.2 Layered view

| Layer | Responsibility | Lives in |
|---|---|---|
| L0 — Native OS | Android / iOS / Windows / macOS kernel + native APIs | OS |
| L1 — .NET 9 runtime + MAUI shell | Process host, GC, AOT where applicable, MAUI native host, Blazor WebView | `Microsoft.NET.Runtime` + `Microsoft.Maui` |
| L2 — ZVec.NET in-process vector DB | Persistent storage, schema, HNSW/RaBitQ/FTS, hybrid search, RRF reranker | `AdamSystems.ZVec.NET` NuGet |
| L3 — ONNX Runtime + EmbeddingGemma 2 ONNX assets | Modular encoder inference (270M / 440M / 740M) | `Microsoft.ML.OnnxRuntime` + `onnx-community/embeddinggemma-2-ONNX` |
| L4 — Recall Core | Capture pipeline, query pipeline, MRL tiering, op-log | `Recall.Core` (shared library) |
| L5 — Recall UI | Razor components rendered in MAUI Blazor Hybrid WebView | `Recall.UI` (Razor class library) |

The key property of this stack is that L2 and L3 are **both in-process**. There is no IPC between the embedder and the DB, no localhost HTTP, no UDS, no shared memory shim. The capture pipeline can hand a `ReadOnlyMemory<float>` directly to a ZVec.NET `Collection.Insert` call without crossing a process boundary. This is the architectural property that makes sub-50 ms latency at 100k vectors achievable on mobile; the moment either L2 or L3 leaves the process, the latency budget doubles.

### 3.3 Data flow — scope-watcher capture (write path)

v1 capture is **scope-driven**, not ambient. The user explicitly configures the scope (paths + per-path file types) in onboarding; the app uses filesystem watchers within that scope.

1. **Scope manager** loads the user's configured scope (paths + per-path file-type filters) on startup; normalizes overlapping paths (e.g., `D:\` and `D:\Photos\` → keeps `D:\` only, warns the user).
2. **Filesystem watcher** (per-platform — see §4.3) emits a `FileEvent` for each change within scope:
   - `Created` → new file in scope → enqueue for ingestion
   - `Modified` → file changed → re-embed (content-addressable ID dedupes if bytes are identical)
   - `Renamed` → source path metadata updated; if content also changed, treat as Created+Deleted
   - `Deleted` → tombstone (DELETE op-log entry; vector retained for sync reconciliation)
3. **iOS fallback**: iOS sandbox forbids true background FS watching. iOS v1 uses "scan on app foreground" — when the user opens Recall, the app walks the scoped paths, diffs against the SQLite `checkpoints.db` last-seen file list, and enqueues new/changed items. macOS variant uses real `FSEvents`.
4. **Modality router** inspects MIME type / extension and routes to the appropriate encoder mode:
   - text (`*.txt`, `*.md`, `*.pdf` text layer) → 270M text-only encoder (always resident)
   - image (`*.jpg`, `*.png`, `*.heic`) → 440M text+vision encoder (on-demand, lazy-loaded)
   - video frame (`*.mp4`, `*.mov` → frame sampler extracts JPEGs at 1 frame / 2 s) → 440M text+vision encoder (batched)
   - audio (`*.wav`, `*.mp3`, `*.m4a`) → 740M full multimodal encoder — **DESKTOP ONLY in v1**. Mobile v1 ignores audio files in scope (logged in `checkpoints.db` as `skipped_modality=audio`)
5. **ONNX Runtime inference session** runs the chosen encoder on the artifact; emits a 768-d `float[]` embedding.
6. **MRL tiering policy** applies a truncation mask:
   - Hot (last 30 days, default) → 768-d (no truncation)
   - Warm (30–180 days) → 256-d (truncate last 512 dims)
   - Cold (>180 days) → 128-d (truncate last 640 dims)
   - User-pinned → 768-d regardless of age
   - Auto-promote on access → 256-d → 768-d (lazy re-embed from source if available, else marked as "needs upgrade")
7. **Modality × tier shard selection**: the vector goes into the appropriate ZVec.NET collection — e.g., `items_image_hot`, `items_text_warm`, `items_video_cold`. v1 has 4 × 3 = 12 collections total.
8. **ZVec.NET collection insert**:
   - Dense vector at the chosen dim
   - FTS payload (raw text / OCR'd text — transcription is parked)
   - Scalar metadata: modality, source path, captured_at (= file mtime), mime, dim_tier, pinned, vector_id (content-addressable SHA-256)
9. **Op-log append**: `(vector_id, op_type=INSERT, tier, ts)` → append-only log on local disk. Sync-ready: future sync layer reads this log and ships it to peer devices.
10. **SQLite `checkpoints.db` update**: ingestion cursor for this scope's path + last-processed file ID updated.
11. **UI event**: artifact card appears in the "recently indexed" feed with an "indexed" indicator.

### 3.4 Data flow — query (read path)

1. **User input**: text typed in any language, image dropped, audio recorded, or sample media chosen from the gallery.
2. **Parse + task prefix selection**: based on which UI affordance the user invoked:
   - "Find" button → `search` task prefix
   - "Group" button → `cluster` task prefix
   - "Tag" button → `classify` task prefix
3. **ONNX Runtime inference** with the chosen task prefix prepended → 768-d query vector.
4. **Truncate to match tier**: query vector is truncated to the lowest active tier in the corpus (so it can be compared to all tiers without re-embedding the corpus). For tiered search, the query is run multiple times — once at 768-d against hot, once at 256-d against warm, once at 128-d against cold — and results are fused.
5. **ZVec.NET hybrid query** in a single call:
   - Dense ANN over HNSW (or HNSW-RaBitQ on x64 desktop)
   - FTS BM25 over the text payload
   - Scalar filter (e.g. `modality == 'image'`, `captured_at > 2025-01-01`)
   - In-DB **RRF reranker** fuses the dense + FTS ranks
6. **Return top-K** (default K=20) with provenance: file path, timestamp, modality, tier, score components.
7. **UI**: result cards with deep-link into the source artifact (open the photo, play the audio at the offset, jump to the document page).

### 3.5 Why this architecture is the only one that fits the constraints

Three constraints together force this architecture:

1. **Privacy** (no PII egress) forbids any hosted embedding API and any hosted vector DB.
2. **Latency** (p95 < 50 ms on mobile at 100k vectors) forbids any IPC between the embedder and the DB; both must be in-process.
3. **Density** (100k mixed-modal items in mobile RAM budget) forbids storing everything at 768-d; MRL truncation is required, which means the embedder must produce a model that natively supports truncation (EmbeddingGemma 2's MRL training does this).

Any architecture that violates any of these three constraints fails the v1 goals. A server-side embedder violates (1). A separate embedder microservice violates (2). A non-MRL embedder violates (3). The IdeaTank §2 strategic insight is that this stack is rare because it satisfies all three simultaneously.

---

## 4. Platform Strategy — MAUI Blazor Hybrid Unified

### 4.1 The decision

v1 is built on a **single .NET MAUI Blazor Hybrid codebase** that publishes natively to four targets:

| Target | RID | Shell | Notes |
|---|---|---|---|
| Android | `android-arm64` | MAUI Android | ZVec.NET ships this RID; proven by upstream sample |
| iOS | `ios-arm64` | MAUI iOS | ZVec.NET ships this RID; soft-launched in beta.6 |
| Windows | `win-x64` | MAUI WinUI | ZVec.NET ships; HNSW-RaBitQ available (AVX2) |
| macOS Catalyst | `maccatalyst-arm64` | MAUI Mac Catalyst | ZVec.NET lists this as soft |

This decision supersedes the original IdeaTank §3.2 proposal of two code paths (MAUI mobile + Native AOT desktop). The user clarification — "I will use .NET MAUI Blazor Hybrid so the output natively will be mobile + desktop" — collapses the matrix.

### 4.2 Why MAUI Blazor Hybrid instead of separate paths

| Criterion | MAUI Blazor Hybrid (chosen) | Native AOT + MAUI (original brief) |
|---|---|---|
| Codebase count | 1 | 2 |
| UI code sharing | 100% (Razor) | ~70% (Razor for mobile; XAML/WinUI for desktop) |
| Cold start | ~150 ms (MAUI init + Blazor WebView) | ~50 ms (Native AOT) |
| Trimmer / AOT warnings to chase | One set | Two sets |
| Build pipeline | One `dotnet publish -f net9.0-android -p:RuntimeIdentifier=...` per target | Two pipelines |
| ZVec.NET RID coverage | All 4 targets covered | Same coverage |
| ZVec.NET MAUI Blazor Hybrid sample | Directly reusable | Sample only covers mobile half |
| Cold-start budget headroom | Tight on low-end Android (Pixel 4a class); comfortable on iPhone 12+ and all desktop | Comfortable everywhere |

The trade-off is cold start: MAUI Blazor Hybrid adds ~100 ms of startup over a Native AOT desktop binary, because the Blazor WebView needs to spin up. For a "second brain" app that the user opens a few times a day, this is the right trade-off; for a serverless function call (the `AOT-RAG Edge Function` idea, IdeaTank §5.1.1), it would be the wrong trade-off, and that idea correctly uses Native AOT.

### 4.3 Platform integration services

Each platform-specific integration is hidden behind an interface in `Recall.Core` and implemented in the platform head project. The shared library depends only on the interfaces; the DI container wires the platform implementation at startup.

```csharp
// Recall.Core/Platform/IClipboardService.cs
public interface IClipboardService
{
    ValueTask<string?> GetTextAsync(CancellationToken ct);
    ValueTask SetTextAsync(string text, CancellationToken ct);
    IObservable<ClipboardEvent> WatchAsync(CancellationToken ct);
}

// Recall.Maui/Platforms/Android/ClipboardService.android.cs
internal sealed class AndroidClipboardService : IClipboardService
{
    public ValueTask<string?> GetTextAsync(CancellationToken ct) =>
        new(Clipboard.Default.GetTextAsync()); // MAUI Clipboard
    // ...
}
```

| Service | Android | iOS | Windows | macOS |
|---|---|---|---|---|
| `IFileScopeWatcherService` | `Storage Access Framework` document-tree observer (or `FileObserver` for legacy paths) | **Not supported in background** — uses "scan on app foreground" instead (walks scoped paths on app activation, diffs against `checkpoints.db`) | `FileSystemWatcher` (System.IO) | `FSEventStream` (CoreServices) |
| `IScopeManagerService` | scope config persistence + overlap normalization (cross-platform shared impl behind interface) | same | same | same |
| `IVideoFrameSamplerService` | `MediaMetadataRetriever` | `AVAssetImageGenerator` | `FFmpegInterop` | `AVAssetImageGenerator` |
| `ISecureStorageService` | `AndroidKeystore` | `SecureEnclave` | `DPAPI` | `Keychain` |
| `IIdleDetectorService` | `PowerManager.isInteractive` | `UIApplication.idleTimerDisabled` | `SystemParameters` | `IOKit` PM |
| `IModelDownloadService` | HTTP fetch + SHA-256 verify + resume (shared impl; uses `HttpClient` registered in `Recall.Core.Sync` namespace exception — model download is the **only** network call allowed in v1) | same | same | same |

**Removed from v1 (now part of the Ambient Capture Epic, §15.5):** `IClipboardService`, `ICameraRollService`, `IScreenCaptureService`, `IAudioRecorderService`.

### 4.4 RID-specific ZVec.NET index selection

The index strategy is **platform-aware** because ZVec.NET's index support matrix is RID-conditional (IdeaTank §1.2, §6 anti-goals):

| Target | Dense index | Quantization | FTS | Notes |
|---|---|---|---|---|
| `android-arm64` | HNSW | none (plain float32) | yes | RaBitQ unavailable (x86_64+AVX2 only); DiskANN unavailable (Linux-only) |
| `ios-arm64` | HNSW | none | yes | Same as Android |
| `win-x64` | HNSW-RaBitQ | RaBitQ (1-bit + re-ranking) | yes | AVX2 present; RaBitQ cuts memory ~8–16× |
| `maccatalyst-arm64` | HNSW | none | yes | ARM; no RaBitQ on Apple Silicon yet |

The selection logic lives in the platform head's `IIndexPolicy`:

```csharp
public interface IIndexPolicy
{
    IndexType DenseIndex { get; }
    bool UseQuantization { get; }
    HnswParams Hnsw { get; }
}

// Recall.Maui/Platforms/Android/IndexPolicy.android.cs
internal sealed class AndroidIndexPolicy : IIndexPolicy
{
    public IndexType DenseIndex => IndexType.Hnsw;
    public bool UseQuantization => false;
    public HnswParams Hnsw => new() { M = 16, EfConstruction = 200, EfSearch = 64 };
}

// Recall.Maui/Platforms/Windows/IndexPolicy.windows.cs
internal sealed class WindowsIndexPolicy : IIndexPolicy
{
    public IndexType DenseIndex => IndexType.HnswRaBitQ;
    public bool UseQuantization => true;
    public HnswParams Hnsw => new() { M = 32, EfConstruction = 400, EfSearch = 96 };
}
```

The query path truncates the query vector to the smallest active tier (see §6 of the Technical Details document) and runs the dense search separately per tier; results are fused by the in-DB RRF reranker in a single call.

### 4.5 Build & publish matrix

| Configuration | Targets | Notes |
|---|---|---|
| `Debug` | local dev (each dev's primary device) | One RID per dev; switch in `launchSettings.json` |
| `Release` (Test) | `android-arm64`, `ios-arm64` (TestFlight), `win-x64` (sideload), `maccatalyst-arm64` (sideload) | All four in CI; sign with dev certs |
| `Release` (Store) | same four | Sign with store / prod certs; submit to Play, App Store, Microsoft Store, Mac App Store |

CI builds all four RIDs on every PR. Local builds can target a single RID. The CI matrix takes ~25 minutes wall-clock with 4 parallel jobs (Android + iOS on macOS runner; Windows on Windows runner; macOS Catalyst on macOS runner — Apple platforms must share a macOS runner due to Apple toolchain licensing).

---


## 5. Project Structure

### 5.1 Solution layout

```
Recall.sln
├── src/
│   ├── Recall.Core/                       # Shared library; .NET 9; no UI deps
│   │   ├── Capture/                        # ICaptureSource, CapturePipeline
│   │   ├── Query/                          # QueryPipeline, TaskPrefix
│   │   ├── Embedding/                      # EncoderMode, OnnxEncoderSession
│   │   ├── Indexing/                       # IIndexPolicy, ZVecCollectionManager
│   │   ├── Mrl/                            # MrlTieringPolicy, TruncationMask
│   │   ├── Storage/                        # OpLog, ContentAddressableId
│   │   ├── Sync/                           # ISyncAdapter (interface only — design in §12)
│   │   ├── Platform/                       # IClipboardService, ICameraRollService, etc.
│   │   └── Recall.Core.csproj             # net9.0; references ZVec.NET, OnnxRuntime
│   │
│   ├── Recall.UI/                          # Razor class library; pure Blazor components
│   │   ├── Pages/                          # Search.razor, Capture.razor, PinManager.razor
│   │   ├── Components/                     # ResultCard.razor, ModalityIcon.razor
│   │   ├── ViewModels/                     # SearchViewModel, etc.
│   │   └── Recall.UI.csproj               # net9.0; references Recall.Core
│   │
│   ├── Recall.Maui/                        # MAUI Blazor Hybrid host project
│   │   ├── Platforms/
│   │   │   ├── Android/                    # ClipboardService.android.cs, IndexPolicy.android.cs
│   │   │   ├── iOS/                        # ClipboardService.ios.cs, IndexPolicy.ios.cs
│   │   │   ├── Windows/                    # ClipboardService.windows.cs, IndexPolicy.windows.cs
│   │   │   └── MacCatalyst/                # ClipboardService.maccatalyst.cs, IndexPolicy.maccatalyst.cs
│   │   ├── wwwroot/                        # static assets, fonts
│   │   ├── MainPage.xaml                  # <blazor:BlazorWebView HostPage="wwwroot/index.html" />
│   │   ├── MauiProgram.cs                  # DI wiring; AddZVec(); AddRecallCore()
│   │   └── Recall.Maui.csproj             # net9.0-android; net9.0-ios; net9.0-windows; net9.0-maccatalyst
│   │
│   └── Recall.Benchmarks/                  # BenchmarkDotNet harness for the perf budget (§7)
│       ├── QueryBenchmarks.cs
│       ├── InsertBenchmarks.cs
│       └── Recall.Benchmarks.csproj
│
├── assets/
│   ├── onnx/
│   │   ├── embeddinggemma-2-text.onnx       # 270M text-only
│   │   ├── embeddinggemma-2-text-vision.onnx # 440M text+vision
│   │   └── embeddinggemma-2-full.onnx       # 740M full multimodal
│   ├── tokenizer/                           # SentencePiece or tokenizers.json
│   └── audio-preprocessor/                  # log-mel filterbank config
│
├── corpus/
│   ├── golden-queries.jsonl                 # §11 data corpus spec
│   └── seed-items/                          # 10k mixed-modal seed items (dev only)
│
├── tests/
│   ├── Recall.UnitTests/                    # xUnit; ~70% line coverage target
│   ├── Recall.IntegrationTests/             # xUnit; pipeline-level
│   ├── Recall.BenchTests/                   # xUnit + BenchmarkDotNet; CI gate
│   └── Recall.CorpusTests/                  # golden-query regression harness
│
├── tools/
│   ├── corpus-builder/                      # builds the 10k seed corpus from public datasets
│   └── mrl-eval/                            # MRL truncation quality sweeps
│
├── docs/
│   ├── Recall_App_Implementation_Plan.md
│   └── Recall_App_Technical_Details.md
│
└── Recall.sln
```

### 5.2 Dependency graph (enforced via `ProjectReference`)

```
            Recall.Maui  ───────────────►  Recall.UI  ──────────────►  Recall.Core
                  │                                                       │
                  │                                                       ├──► AdamSystems.ZVec.NET  (NuGet)
                  │                                                       ├──► Microsoft.ML.OnnxRuntime  (NuGet)
                  │                                                       └──► Microsoft.Extensions.*  (DI, logging, opts)
                  │
                  └────► platform head (Android/iOS/Windows/MacCatalyst)
```

`Recall.Core` has no UI dependencies and no MAUI dependencies; it can be unit-tested on a headless CI runner. `Recall.UI` depends on `Recall.Core` only via abstractions (the ViewModels depend on interfaces, not implementations). `Recall.Maui` is the composition root: it registers platform implementations into the DI container at startup.

### 5.3 NuGet dependencies (locked at v1)

| Package | Version | Role |
|---|---|---|
| `AdamSystems.ZVec.NET` | `1.0.0-beta.6+zvec.0.7.0` (pin; will migrate to GA in M3) | In-process vector DB; HNSW / HNSW-RaBitQ / FTS (BM25) / RRF |
| `AdamSystems.ZVec.NET.Mapping` | same | Typed ODM for collections |
| `Microsoft.ML.OnnxRuntime` | `1.20+` | ONNX Runtime; EP selection per platform (NNAPI/Android, CoreML/iOS+macOS, DirectML/Windows) |
| `Microsoft.ML.OnnxRuntime.Extensions` | matching | Custom op support |
| `Microsoft.ML.Tokenizers` | `1.0+` | HuggingFace tokenizer in .NET |
| `Microsoft.Data.Sqlite` | `9.0.x` (LTS-aligned with .NET 10) | SQLite for `checkpoints.db` ingestion cursors + job state |
| `Microsoft.Maui.Controls` | `10.0.x` (.NET 10 MAUI) | MAUI shell |
| `Microsoft.AspNetCore.Components.WebView.Maui` | `10.0.x` | Blazor WebView host |
| `Microsoft.Extensions.DependencyInjection` | `10.0.x` | DI container |
| `Microsoft.Extensions.Logging` | `10.0.x` | Structured logging |
| `Microsoft.Extensions.Hosting` | `10.0.x` | Background services for ingestion |
| `BenchmarkDotNet` | `0.14+` | Perf gate in CI |
| `xUnit` | `2.9+` | Test framework |
| `FluentAssertions` | `7+` | Test assertions |

Target framework everywhere: **`net10.0`** (LTS). .NET 9 STS goes EOL May 2026, before v1 GA. |

### 5.4 Branch & release strategy

- **`main`** — always shippable. Each PR must pass CI on all four RIDs.
- **`dev`** — integration branch; nightly build.
- **`release/v1.x.y`** — release prep; cut from `dev`; only bugfixes land here.
- **`feature/*`** — short-lived; PR-targeted at `dev`.
- **`spike/*`** — exploratory; not merged to `main` without rewrite.
- Tags follow SemVer: `v1.0.0-rc1`, `v1.0.0`, `v1.1.0`, etc.

### 5.5 Code-style & static-analysis gates

| Tool | Configuration | CI gate |
|---|---|---|
| `dotnet format` | `EditorConfig` enforces 4-space indent, UTF-8, LF line endings | Fails PR on diff |
| `Roslynator` | rule set: `default+`, with `RCS1245` (no `async void`) as error | Fails PR on warning |
| `SonarAnalyzer.CSharp` | Quality Profile: `Recall Way`; coverage gate 70% | Fails PR below coverage or on Critical/Blocker |
| `BannedApiAnalyzers` | Ban `HttpClient`, `Socket`, `TcpListener`, `UdpClient` outside `Recall.Core.Sync` namespace | Fails PR on use |
| `Microsoft.VisualStudio.Threading.Analyzers` | `VSTHRD200` etc. for async naming | Fails PR on warning |

The `BannedApiAnalyzers` rule is the **enforcement mechanism for the no-egress anti-goal**: it makes it a compile error to introduce a network call outside the `Sync` namespace, and the `Sync` namespace's interface is designed-but-not-implemented in v1.

---

## 6. Phase Plan & Milestones (Gantt)

### 6.1 Milestone overview

| Milestone | Window | Theme | Exit criteria (full list in §14) |
|---|---|---|---|
| **M1 — Text+Image POC** | Day 0–30 | Prove the path on Android only | Android build green; 1k-item corpus ingested; golden-query p95 < 80 ms; one screenshot returned for "the red scooter" |
| **M2 — Full multimodal** | Day 30–60 | Audio + video frames; cross-platform bring-up | All 4 RIDs build green; 10k-item corpus; audio + video capture live; golden-query p95 < 50 ms on Android, < 20 ms on desktop |
| **M3 — Production hardening** | Day 60–90 | Crash safety, perf, telemetry, store-ready | 100k-item corpus; crash-recovery tests pass; perf gate green; first RC cut |

### 6.2 Gantt (week resolution)

```
Wk 1   2   3   4   5   6   7   8   9   10  11  12  13
─────────────────────────────────────────────────────
M1: Text+Image POC (Android)              ▼
[====][====][====][====]
│    │    │    │
│    │    │    └── M1 exit: golden-query p95 < 80 ms
│    │    └────── capture pipeline (text + image)
│    └─────────── ZVec.NET DI + HNSW on android-arm64
└──────────────── ONNX Runtime text+vision encoder wired

M2: Full multimodal + cross-platform            ▼
                [====][====][====][====]
                     │    │    │    │
                     │    │    │    └── M2 exit: all 4 RIDs green, p95 < 50 ms mobile
                     │    │    └────── audio + video frame capture live
                     │    └────────── iOS / Windows / macOS bring-up
                     └────────────── MRL tiering + in-DB RRF live

M3: Production hardening                              ▼
                                [====][====][====][====]
                                     │    │    │    │
                                     │    │    │    └── v1.0 RC cut
                                     │    │    └────── telemetry & crash reporter (opt-in)
                                     │    └────────── perf gate in CI (BenchmarkDotNet)
                                     └────────────── crash-recovery test (kill -9 + restart)
```

### 6.3 M1 — week-by-week

| Week | Stream | Deliverable | Owner |
|---|---|---|---|
| W1 | Infra | `Recall.sln` skeleton; CI builds an empty MAUI Blazor Hybrid app on android-arm64 | Infra Eng |
| W1 | Embedder | ONNX Runtime loads `embeddinggemma-2-text.onnx` (270M); inference returns 768-d vector from a sample string | ML Eng |
| W1 | DB | ZVec.NET `AddZVecCollection<T>()` wired into DI; can `Insert` + `Knn` 100 vectors in a unit test | Backend Eng |
| W2 | Capture | Clipboard + camera-roll capture sources wired on Android | Android Eng |
| W2 | Embedder | Text encoder mode end-to-end: clipboard text → embed → insert | Backend Eng |
| W2 | Embedder | Vision encoder mode (`embeddinggemma-2-text-vision.onnx`, 440M); image → embed | ML Eng |
| W3 | Query | Hybrid query (dense + FTS) for text and image queries; result cards render in Razor | Full-stack |
| W3 | Corpus | Build 1k-item seed corpus (§11) on dev device | QA |
| W4 | Perf | First perf sweep; identify hotspots; document in §7 perf budget | Perf Eng |
| W4 | Demo | "Red scooter near the Colosseum" demo recorded; M1 exit criteria met | Tech Lead |

**M1 exit criteria (gate):**
- Android `dotnet publish -f net10.0-android -p:RuntimeIdentifier=android-arm64` succeeds.
- 1k-item seed corpus ingested in < 5 minutes wall-clock on Pixel 6a.
- Golden query set (10 queries) p95 < 80 ms on Pixel 6a at 1k items.
- One screenshot returned for the "red scooter" demo query, offline, no telemetry.
- Code coverage ≥ 60% on `Recall.Core`.
- Scope manager UI live: user can add paths + per-path file types; FS watcher fires on file create within scope.
- First-run ONNX download flow works: 270M text encoder downloads from Hugging Face CDN, SHA-256 verified, app ready after download completes.
- SQLite `checkpoints.db` persists ingestion cursor across app restarts.

### 6.4 M2 — week-by-week

| Week | Stream | Deliverable | Owner |
|---|---|---|---|
| W5 | Embedder | Audio encoder (`embeddinggemma-2-full.onnx`, 740M) wired; voice memo → embed | ML Eng |
| W5 | Capture | Voice memo recorder on Android + iOS | Mobile Eng |
| W6 | Capture | Video frame sampler at 1 frame / 2 s; batched embed via vision encoder | Mobile Eng |
| W6 | Platform | iOS build green; Windows build green; macOS Catalyst build green | Infra Eng |
| W7 | MRL | Recency tiering live (hot/warm/cold auto-promote); user pinning UI | Backend Eng |
| W7 | Query | Task prefix selection UI (Find / Group / Tag) | Full-stack |
| W8 | RRF | In-DB RRF reranker in the hybrid query; A/B vs no-RRF baseline | ML Eng |
| W8 | Corpus | 10k-item corpus on dev device | QA |

**M2 exit criteria (gate):**
- All four RIDs build green in CI.
- 10k-item corpus ingested in < 60 minutes on Pixel 6a.
- Golden query set p95 < 50 ms on Pixel 6a; p95 < 20 ms on x86 laptop.
- Mobile: text + image + video frame modalities searchable. **Audio is desktop-only in v1** (mobile audio deferred to v1.5 pending INT8 ONNX export of 740M encoder).
- Modality × tier sharding live: 12 ZVec.NET collections; per-modality HNSW tuning.
- MRL tiering verified: storage at 10k items ≤ 15 MB on mobile (text + image + video frames only).
- RRF ablation: golden-query recall@10 with RRF ≥ recall@10 without RRF (no regression).
- BM25 explicit in FTS path; hybrid query returns both dense + FTS scores in result provenance.
- 5 languages (EN, AR, ZH, HI, FR) render correctly.
- iOS v1 "scan on app foreground" works: open Recall → diffs scoped paths against `checkpoints.db` → ingests new items.
- FS watcher handles rename/move semantics correctly (content-addressable ID dedupes; metadata updates).
- R-1, R-2, R-3, R-4, R-5, R-11, R-12 all de-risked.

### 6.5 M3 — week-by-week

| Week | Stream | Deliverable | Owner |
|---|---|---|---|
| W9 | Robustness | Kill -9 mid-insert test; corruption-recovery code path | Backend Eng |
| W9 | Perf | BenchmarkDotNet harness in CI; perf gate | Perf Eng |
| W10 | Telemetry | Opt-in crash reporter (no PII, no vector payload, lib log + stack trace only) | Infra Eng |
| W10 | UX | Store screenshots, app icons, localization strings (EN, AR, ZH, HI, FR) | Design |
| W11 | Scale | 100k-item corpus on dev device; perf sweep at scale | Perf Eng |
| W11 | Security | Threat model walkthrough; `BannedApiAnalyzers` audit | Tech Lead |
| W12 | Store | Play Store + TestFlight + Microsoft Store + Mac App Store listings drafted | Product |
| W12 | RC | v1.0-rc1 tag; smoke tests on each platform | QA |
| W13 | GA | v1.0.0 tag; ship | Tech Lead |

**M3 exit criteria (gate):**
- 100k-item corpus (text + image + video frames on mobile; + audio on desktop); p95 query latency on Pixel 6a ≤ 50 ms.
- Crash-recovery: kill -9 mid-insert on 1k-batch produces zero corruption across 100 trials.
- Perf gate green in CI on all four RIDs.
- All five localizations render correctly.
- Threat model walkthrough signed off — including the "no ambient capture" guarantee.
- v1.0-rc1 cut and dogfooded for ≥ 5 days by the team.
- App Store / Play Store / Microsoft Store / Mac App Store listings ready.
- Privacy policy reviewed — lead copy is "Your memories never leave your device. You pick which folders Recall watches."
- Telemetry and crash reporter are opt-in with clear UX.
- Scope manager UI is the primary onboarding screen ("Pick which folders Recall should index").
- **v1.0.0 tag and ship.**

### 6.6 Critical path & float

The critical path is **M1's embedder integration → M2's iOS + macOS Catalyst bring-up → M3's store submission**. M2 is the riskiest window because it adds three new RIDs simultaneously. Float is built into M3 (W13 has no new code, only store prep); if M2 slips by a week, M3 absorbs it.

---

## 7. Performance Budget

### 7.1 Targets per platform

| Metric | Pixel 6a (mid Android) | iPhone 13 (mid iOS) | x86 laptop (8c/16GB) | Apple M2 (8c/16GB) |
|---|---|---|---|---|
| Cold start (app open → ready) | ≤ 800 ms | ≤ 500 ms | ≤ 400 ms | ≤ 350 ms |
| Embedder load (text 270M) | ≤ 600 ms | ≤ 400 ms | ≤ 200 ms | ≤ 180 ms |
| Embedder load (full 740M) | ≤ 1800 ms | ≤ 1200 ms | ≤ 500 ms | ≤ 400 ms |
| Single-text embed latency | ≤ 35 ms | ≤ 25 ms | ≤ 8 ms | ≤ 6 ms |
| Single-image embed latency | ≤ 120 ms | ≤ 80 ms | ≤ 35 ms | ≤ 25 ms |
| Single-audio embed latency (8s clip) | ≤ 250 ms | ≤ 180 ms | ≤ 90 ms | ≤ 70 ms |
| Insert latency (1 vector, HNSW) | ≤ 5 ms | ≤ 4 ms | ≤ 2 ms | ≤ 1.5 ms |
| Query p95 latency (100k items, top-20) | ≤ 50 ms | ≤ 40 ms | ≤ 20 ms | ≤ 15 ms |
| Query p99 latency (same) | ≤ 90 ms | ≤ 75 ms | ≤ 35 ms | ≤ 25 ms |
| RSS after warmup | ≤ 400 MB | ≤ 350 MB | ≤ 700 MB | ≤ 600 MB |
| RSS after 100k items indexed | ≤ 600 MB | ≤ 500 MB | ≤ 1.0 GB | ≤ 800 MB |
| Disk: DB file at 100k items (MRL tiered) | ≤ 80 MB | ≤ 80 MB | ≤ 60 MB (RaBitQ) | ≤ 80 MB |
| Disk: ONNX model assets | ≤ 1.5 GB | ≤ 1.5 GB | ≤ 1.5 GB | ≤ 1.5 GB |
| Battery: 30 min background ingestion | ≤ 4% | ≤ 3% | n/a | n/a |
| Battery: 30 min foreground query session | ≤ 6% | ≤ 5% | n/a | n/a |

### 7.2 Storage math (sanity-check against §7.1 disk targets)

At 100k items, MRL tiered (40k hot @ 768d, 35k warm @ 256d, 25k cold @ 128d), float32:

- Hot: 40,000 × 768 × 4 B = **122.9 MB**
- Warm: 35,000 × 256 × 4 B = **35.8 MB**
- Cold: 25,000 × 128 × 4 B = **12.8 MB**
- Total dense vectors: **171.5 MB** (mobile HNSW)
- With HNSW graph overhead (~1.5×): **~257 MB** on mobile

This exceeds the 80 MB target. To hit 80 MB at 100k items on mobile, the tier ratio must shift:

- 10k hot @ 768d = 30.7 MB
- 30k warm @ 256d = 30.7 MB
- 60k cold @ 128d = 30.7 MB
- Total dense: 92.2 MB → ~138 MB with HNSW overhead

Still over. The HNSW-RaBitQ quantization (x86 desktop only) cuts memory by ~8–16×, which is how desktop hits 60 MB. On mobile, the achievable target at 100k items is closer to **150 MB** than 80 MB; the perf budget table reflects 80 MB as an **aspirational target** that requires either (a) extending RaBitQ to ARM in upstream ZVec, or (b) reducing the corpus to 50k items on mobile, or (c) accepting a higher tier ratio (cold = 80% of items at 128d).

The plan: ship at the 150 MB reality for v1; document the gap as a tracked risk (§9, R-7); pursue (a) or (b) as a v1.1 optimization.

### 7.3 Latency decomposition (mobile, 100k items, top-20 query)

| Stage | Budget | Notes |
|---|---|---|
| UI input → ViewModel | 2 ms | Razor → C# event |
| Parse + task prefix | 1 ms | string concat |
| ONNX embed query | 25 ms | text encoder (270M on mobile for text queries; 740M for cross-modal) |
| Truncate query to min active tier | < 1 ms | array slice |
| ZVec.NET hybrid query (dense + FTS + scalar) | 15 ms | HNSW search @ ef=64 + BM25 + scalar filter |
| In-DB RRF reranker | 3 ms | rank fusion of dense + FTS top-K |
| Result marshalling → UI | 4 ms | projection to ResultCard VM |
| Render top-20 cards | 5 ms | Blazor WebView diff |
| **Total p95** | **55 ms** | slightly over the 50 ms target — see §7.4 mitigation |

### 7.4 Closing the 5 ms gap to the 50 ms target

Three options, in priority order:

1. **Lazy render**: render only the first 5 cards synchronously; virtualize the rest. Saves 3 ms in the UI render stage. (Easy win.)
2. **Query-side encoder truncation**: query text encoder at 256-d when the corpus has no hot items (warm+cold only). Saves 8 ms in ONNX embed (less work). Conditional on the MRL tiering policy's hot-set cardinality.
3. **HNSW `ef_search` tuning**: drop from 64 to 48 on mobile. Saves 4 ms. Risk: recall@10 may regress by 1–2 points; gate on golden-query recall.

Combined, options 1 and 2 close the 5 ms gap without touching `ef_search`. Option 3 is held in reserve as a v1.1 lever.

### 7.5 CI perf gate

`BenchmarkDotNet` harness in `Recall.Benchmarks` runs the following on every PR that touches `Recall.Core` or `Recall.Maui`:

- `InsertBenchmarks.InsertSingle_100kCorpus_Mobile` — must be ≤ 5 ms
- `QueryBenchmarks.HybridQueryTop20_100kCorpus_Mobile` — must be ≤ 60 ms (5 ms headroom over the p95 target)
- `QueryBenchmarks.HybridQueryTop20_100kCorpus_Desktop` — must be ≤ 25 ms
- `MrlBenchmarks.TruncateAndInsert_256d` — must be ≤ 3 ms

A regression of > 10% on any of these fails the CI gate. The harness runs on a Pixel 6a connected via USB to a CI runner (or, in cloud CI, an x86_64 emulator at 2 vCPU).

---

## 8. Make / Buy Matrix

For each subsystem, we ask: **build it ourselves, reuse an OSS component, or defer to v1.5+?** The bias is toward reuse, because the stack's defensible value is in the **integration** (EmbeddingGemma 2 ⊕ ZVec.NET ⊕ MAUI Blazor Hybrid), not in any single component.

| Subsystem | Make | Buy / Reuse | Defer | Notes |
|---|---|---|---|---|
| MAUI Blazor Hybrid shell | — | ZVec.NET MAUI Blazor Hybrid sample | — | Sample provides the boilerplate; we extend |
| ZVec.NET DI wiring | — | `AddZVecCollection<T>()` from sample | — | Direct reuse |
| ONNX Runtime session | thin wrapper | `Microsoft.ML.OnnxRuntime` API | — | One thin `OnnxEncoderSession` wrapper |
| EmbeddingGemma 2 ONNX assets | — | `onnx-community/embeddinggemma-2-ONNX` | — | Direct download; no fine-tuning in v1 |
| Tokenizer | thin wrapper | HuggingFace `tokenizers.json` + `Microsoft.ML.Tokenizers` | — | One thin wrapper |
| Audio preprocessor (log-mel) | thin wrapper | ONNX Runtime custom op community build | — | Needed for 740M audio encoder |
| Capture: clipboard | platform impl | MAUI `Clipboard.Default` | — | |
| Capture: camera roll | platform impl | MAUI `MediaPicker.Default` | — | |
| Capture: screen OCR | build | Tesseract .NET port or Apple `Vision` framework | — | Apple path uses native Vision; Android uses Tesseract |
| Capture: voice memo | platform impl | MAUI `AudioManager` | — | |
| Capture: video frame sampler | build | — | — | No good cross-platform library; build per-platform |
| HNSW index | — | ZVec.NET | — | Direct |
| HNSW-RaBitQ (x64 desktop) | — | ZVec.NET | — | Direct |
| FTS index | — | ZVec.NET | — | Direct |
| In-DB RRF reranker | — | ZVec.NET | — | Direct |
| MRL tiering policy | build | — | — | This is a Recall-specific algorithm; no upstream equivalent |
| Content-addressable vector IDs | build | — | — | SHA-256 over (modality, source_hash, dim_tier) |
| Op-log (append-only) | build | — | — | Plain file append; no library needed |
| Sync transport | — | — | **deferred to v1.5** | Design in §12; no code in v1 |
| Sync CRDT | — | — | **deferred to v1.5** | Design in §12; no code in v1 |
| E2EE key hierarchy | — | — | **deferred to v1.5** | Design in §12; no code in v1 |
| Razor UI: search view | build | — | — | |
| Razor UI: capture view | build | — | — | |
| Razor UI: result cards | build | — | — | |
| Razor UI: pin / tier manager | build | — | — | |
| Razor UI: sync panel | build (stub) | — | — | Renders "Sync is not available in v1"; uses the §12 design as the spec |
| Crash reporter | thin wrapper | `Sentry.NET` or `AppCenter` | — | Opt-in; no PII; no vector payload |
| Telemetry | thin wrapper | `AppCenter` or self-host | — | Opt-in; no PII; usage counters only |
| Localization | — | `.resx` + MAUI `CultureInfo` | — | EN, AR, ZH, HI, FR in v1 |
| CI / build | — | GitHub Actions; macOS runner for iOS + macOS Catalyst | — | |
| Unit test framework | — | xUnit | — | |
| Integration test framework | — | xUnit + `WebApplicationFactory`-style harness | — | |
| Perf gate | — | BenchmarkDotNet | — | |
| Corpus builder (§11) | build | — | — | `tools/corpus-builder`; assembles 10k mixed-modal items |
| MRL eval harness | build | — | — | `tools/mrl-eval`; sweeps 128/256/512/768 across golden queries |

### 8.1 What "build" means in this matrix

The "build" rows are the Recall-specific value-add. They are:

1. **MRL tiering policy** — the recency / pinning / promote-on-access algorithm. This is original work; no upstream library does this for a personal vector corpus.
2. **Content-addressable vector IDs** — the SHA-256 scheme that makes vectors sync-safe (see §12 of Technical Details). Required for the deferred sync layer.
3. **Op-log** — the append-only log that sync reads from. Trivial to build; more work to integrate than to write.
4. **Capture pipeline** — the modality router that picks the right encoder for each input. Recall-specific; no library does this.
5. **Query pipeline** — the parse + task prefix + tiered-search + RRF orchestration. Recall-specific.
6. **Razor UI** — the search / capture / pin / sync-panel views. Recall-specific.
7. **Video frame sampler** — no good cross-platform .NET library; we build per-platform (Android `MediaMetadataRetriever`, iOS `AVAssetImageGenerator`, Windows FFmpeg, macOS `AVAssetImageGenerator`).
8. **Screen OCR** — platform split: Apple uses native `Vision` framework (best quality, no library); Android uses Tesseract .NET port (acceptable quality).
9. **Corpus builder** — assembles the 10k mixed-modal seed corpus from public datasets (see §11).
10. **MRL eval harness** — sweeps the truncation quality curve.

Everything else is reused. This is the right shape: the defensible moat is integration + policy, not re-implementation of HNSW or ONNX Runtime.

---

## 9. Risk Register

The top 10 risks for v1, ordered by impact × probability. Each risk has a trigger (how we'll detect it), a mitigation (what we'll do to reduce probability), and a contingency (what we'll do if it fires in production).

### R-1 — ZVec.NET `ios-arm64` RID is unstable in beta

- **Source:** IdeaTank §9 explicitly flags this; ZVec.NET docs list `ios-arm64` as Mobile HARD in beta.4.
- **Impact:** High — iOS is one of four v1 targets; dropping iOS halves the platform matrix.
- **Probability:** Medium — beta stability; could go either way by GA.
- **Trigger:** iOS build fails in CI; or runtime crash on `ZVecCollection.Insert` on iPhone; or memory growth over 30 minutes.
- **Mitigation:** Early smoke test in W6 (M2): stand up an iPhone 13 with a 1k-item corpus and run it for 30 min. If it crashes, escalate to ZVec.NET maintainers (the SDK is Apache 2.0; we can submit issues and patches).
- **Contingency:** If iOS stays broken at end of M2, ship v1 on Android + Windows + macOS Catalyst only, and publish iOS as a beta track on TestFlight with the limitation noted. v1.1 ships iOS once the upstream RID is stable.

### R-2 — 740M full multimodal encoder is unshippable on mobile in v1 (RESOLVED by scope cut)

- **Source:** User clarification + IdeaTank §9. Android LMK (low-memory-killer) will silently kill any app that sustains >1 GB RSS on most devices; MAUI empty app baseline is ~100 MB; the 740M encoder at fp16 is ~1.5 GB → total ~1.9 GB → auto-killed.
- **Impact:** High (had it not been resolved).
- **Probability:** Resolved in v1.1 by **dropping the 740M encoder from mobile v1 entirely**. Mobile v1 ships text (270M, ~540 MB RSS) + text+vision (440M, ~880 MB RSS, on-demand) only.
- **Trigger:** N/A in v1 (no 740M encoder on mobile).
- **Mitigation:** Mobile v1 = text + image + video frames only. Audio ingestion and audio queries are **desktop-only** in v1. Mobile audio waits for v1.5+ when (a) an INT8-quantized ONNX export of the 740M encoder is available (cutting RSS to ~750 MB) and (b) 8 GB baseline mobile devices are the floor.
- **Contingency:** If even the 440M vision encoder proves too aggressive on the lowest-end Android (Pixel 4a class), narrow the mobile target to Pixel 6a+ and iPhone 12+ for v1; document the device floor in store listings.
- **Status:** Closed in v1.1 (scope cut).

### R-3 — MRL truncation quality at 128d is poor for multimodal queries

- **Source:** IdeaTank §9 — published MRL benchmarks focus on text; multimodal MRL quality at 128d is unmeasured.
- **Impact:** Medium — if 128d multimodal is bad, the cold tier hurts recall@10 for image/audio queries on archival data.
- **Probability:** Medium — multimodal embeddings are higher-dimensional in their information content; truncation may hurt more than for text.
- **Trigger:** Golden-query recall@10 on cold-tier items drops > 5 points below hot-tier recall@10 (after normalizing for query difficulty).
- **Mitigation:** Sweep 128/256/512/768 across the golden query set in W4 (M1) for text and W7 (M2) for image/audio. If 128d multimodal is bad, raise the cold-tier floor to 256d (storage doubles, still 3× under hot tier).
- **Contingency:** If 256d is also bad for multimodal, raise cold floor to 512d (1.5× under hot), accept the storage cost, and pursue (a) a fine-tuned MRL head for multimodal in v1.5, or (b) per-modality tiering (text cold at 128d, image cold at 256d, audio cold at 512d).

### R-4 — HNSW recall drops non-linearly when the corpus mixes tiers (768d / 256d / 128d)

- **Source:** This is a known issue with MRL + ANN — HNSW graphs built on full-dim vectors are searched with truncated queries, and the recall/ef_search tradeoff can shift.
- **Impact:** Medium — if the search must run separately per tier, latency goes up.
- **Probability:** Medium — depends on whether ZVec.NET supports multi-tier HNSW in one collection.
- **Trigger:** Recall@10 on tiered-corpus < recall@10 on homogeneous 768d corpus by > 3 points, at the same ef_search.
- **Mitigation:** Build **separate ZVec.NET collections per tier** (hot / warm / cold) rather than mixing tiers in one collection. The query pipeline runs three ANN searches (one per active tier) and fuses via RRF. Latency cost is ~3× the single-tier search; with ef_search=48 per tier on mobile, total is ~45 ms, still under budget.
- **Contingency:** If three-tier search is too slow, drop to two tiers (hot 768d + cold 128d only) and accept a small recall regression on warm data.

### R-5 — Battery drain from background ingestion exceeds 4% / 30 min on Android

- **Source:** Background ingestion uses the 270M text encoder, which is non-trivial on a mobile CPU.
- **Impact:** Medium — 4% / 30 min is the target; if it's 10%, users will uninstall.
- **Probability:** Medium — plausible without aggressive batching.
- **Trigger:** Battery monitor in the dogfood build reports > 6% / 30 min.
- **Mitigation:** Batch ingestion (every 60 s, not on every clipboard event); use Android's `WorkManager` with `BatteryNotLow` constraint; cap ingestion at 100 items / hour; degrade to 128d for ingestion-time embed (the cold-tier target) to halve CPU.
- **Contingency:** If battery is still bad, make background ingestion **opt-in** in v1; default to foreground-only. Users who want background ingestion enable it explicitly.

### R-6 — iOS `ReplayKit` cannot capture screen in background (RESOLVED by scope cut)

- **Source:** Apple restricts screen recording to foreground or extension; `ReplayKit` broadcast picker is the only path.
- **Impact:** None in v1 — screen OCR / capture is removed from v1 scope entirely (Ambient Capture Epic, §15.4).
- **Probability:** N/A — not pursuing in v1.
- **Trigger:** N/A.
- **Mitigation:** v1 ships with no screen-capture feature at all. If/when the Ambient Capture Epic lands (post-v1.5), the iOS implementation will use a Screen Time-style extension and `ReplayKit` foreground-only as fallback.
- **Status:** Closed in v1.1 (scope cut).

### R-7 — Storage at 100k items on mobile exceeds the 80 MB target (REVISED)

- **Source:** §7.2 math shows ~150 MB is more realistic without RaBitQ on ARM.
- **Impact:** Low — 150 MB is still small; users won't notice.
- **Probability:** High — math is clear.
- **Trigger:** M3 perf sweep shows DB file > 80 MB on 100k-item mobile corpus.
- **Mitigation:** Document the 150 MB reality in the v1 release notes. Shift tier ratio further toward cold (e.g., 5% hot / 15% warm / 80% cold) to push the number down.
- **Contingency:** If 150 MB is a UX problem, raise the storage warning threshold in the UI and let the user clear archival data; or pursue RaBitQ on ARM in upstream ZVec (contribute the patch).

### R-8 — ZVec.NET upstream breaking change between beta.6 and GA

- **Source:** SDK is `1.0.0-beta.6+zvec.0.7.0`; beta APIs can change.
- **Impact:** Medium — could force a refactor mid-M2 or M3.
- **Probability:** Low-Medium — beta SDKs change; maintainers have been responsive in the open-source repo.
- **Trigger:** A new ZVec.NET release changes the `Collection` API, the `IHybridQueryBuilder` API, or the RRF signature.
- **Mitigation:** Pin ZVec.NET to `1.0.0-beta.6+zvec.0.7.0` in `Directory.Packages.props`. Watch the upstream repo weekly. When a stable `1.0.0` is released, schedule a one-week migration spike in M3.
- **Contingency:** If upstream breaks hard, freeze on beta.6 for v1 and fork if necessary. The in-process DB file format is stable upstream; we don't lose data.

### R-9 — ONNX Runtime ExecutionProvider (EP) availability on iOS

- **Source:** ONNX Runtime's CoreML EP is available on iOS; NNAPI EP is available on Android; DirectML EP is on Windows; CoreML EP is on macOS. None of this is automatic — each EP needs explicit registration.
- **Impact:** Medium — if EPs are not registered, inference falls back to CPU, which is 5–10× slower and blows the latency budget.
- **Probability:** Low — the EPs are well-documented; the failure mode is forgetting to register them.
- **Trigger:** First-inference latency on a clean install > 500 ms for text encoder on mobile.
- **Mitigation:** CI smoke test measures first-inference latency on each platform; fails the build if EP is not registered.
- **Contingency:** If an EP is broken on a given OS version, fall back to CPU and document the latency regression; prioritize a fix in the next point release.

### R-10 — Apple App Review risk for ambient capture (GREATLY REDUCED in v1.1)

- **Source:** App Review is unpredictable for apps that capture audio or screen in background. v1.0 draft had this as a Medium risk.
- **Impact:** Low in v1.1 — v1 captures nothing ambient; only storage access for user-scoped paths. App Review risk drops to the level of any other file-manager app.
- **Probability:** Low — the permission sheet is just storage access; well-understood by reviewers.
- **Trigger:** App Review rejection with reason "guideline 5.1.1" (audio capture) or "guideline 4.2" (minimum functionality) — should not fire in v1.1.
- **Mitigation:** Privacy policy leads with "Your memories never leave your device. You pick which folders Recall watches." Submit a clear scope-of-access statement.
- **Contingency:** If App Review raises an unrelated concern, ship TestFlight first; resubmit on feedback.
- **Status:** Risk downgraded from Medium to Low in v1.1.

### R-11 — "Spying" perception risk (NEW in v1.1)

- **Source:** User clarification. The original v1.0 draft proposed background capture of clipboard + camera roll + screen OCR + audio. Even with zero egress, the OS-level signals (green camera dot, red mic dot, broadcast banner) plus the permission sheets ("Allow Recall to access your Microphone? All the time") trigger every privacy alarm a user has.
- **Impact:** High — user deletes the app within 24 hours of granting permissions.
- **Probability:** High if v1 had shipped with ambient capture.
- **Trigger:** User churn > 30% within 7 days of install.
- **Mitigation:** v1.1 ships with **scope-only FS watcher** capture. The only permission asked is storage access for user-scoped paths. No mic, no camera, no screen. Store listing leads with "Your memories never leave your device. You pick which folders Recall watches." Ambient capture is a separate Epic (§15.4), gated behind explicit user opt-in.
- **Contingency:** N/A — the scope-only model is the v1 spec.
- **Status:** Mitigated by scope-only model in v1.1.

### R-12 — Filesystem watcher reliability across platforms (NEW in v1.1)

- **Source:** v1.1 introduces FS watchers as the only ingest trigger. FS watchers are notoriously unreliable on Windows (`FileSystemWatcher` misses events under high load), Android (`FileObserver` per-file is expensive for large scopes), and iOS (no background FS watching at all).
- **Impact:** Medium — missed events = silently unindexed files = recall regression.
- **Probability:** Medium — known limitation of FS watcher APIs.
- **Trigger:** Dogfood run shows files in scope that aren't indexed after several hours of normal use.
- **Mitigation:** (a) Windows: use `FileSystemWatcher` with `IncludeSubdirectories=true` + `InternalBufferSize=64KB` + retry-on-error; periodic reconciliation sweep every 4 hours (walks scoped paths, diffs against `checkpoints.db`). (b) Android: use `Storage Access Framework` document-tree observer (modern, scoped-storage-compliant); fall back to periodic reconciliation sweep every 4 hours. (c) iOS: by design, scan on app foreground (no background FS watcher); this is the platform reality, not a bug. (d) macOS: `FSEventStream` is reliable.
- **Contingency:** If reconciliation sweep finds > 1% of scoped files unindexed, run a full re-scan (potentially long; show UX progress). Investigate root cause.

### 9.1 Risk heatmap (impact × probability) — v1.1

| Risk | Impact | Probability | Score (I×P) | Owner | v1.1 status |
|---|---|---|---|---|---|
| R-1 iOS RID unstable | High | Medium | **6** | Tech Lead | Open — de-risk in M2 W6 |
| R-2 740M mobile RSS | — | — | — | — | **Closed** in v1.1 (scope cut) |
| R-3 MRL 128d multimodal | Medium | Medium | **4** | ML Eng | Open — measure in M2 W7 |
| R-4 HNSW multi-tier recall | Medium | Medium | **4** | Backend Eng | Open — addressed by modality × tier shards |
| R-5 Battery ingestion | Medium | Medium | **4** | Android Eng | Open — measure in M2 W5 |
| R-6 iOS ReplayKit | — | — | — | — | **Closed** in v1.1 (scope cut) |
| R-7 Storage 80 MB (now 150 MB) | Low | High | **2** | Backend Eng | Open — accept 150 MB reality |
| R-8 ZVec.NET breaking | Medium | Low-Med | **2.25** | Tech Lead | Open — pin to beta.6; watch upstream |
| R-9 ONNX EP missing | Medium | Low | **2** | ML Eng | Open — CI smoke test |
| R-10 App Review (ambient capture) | Low | Low | **1** | Product | **Downgraded** in v1.1 (no ambient capture in v1) |
| R-11 "Spying" perception | High | (mitigated) | **0** | Tech Lead | **Mitigated** in v1.1 (scope-only model) |
| R-12 FS watcher reliability | Medium | Medium | **4** | Backend Eng | Open — reconciliation sweep every 4h |

### 9.2 Risk-burn-down (v1.1)

R-2 and R-6 are **closed** by v1.1 scope cuts. R-10 is **downgraded** to Low. R-11 is **mitigated** by the scope-only model.

The remaining highest-scored open risks are R-1 (iOS RID stability) and R-3/R-4/R-5/R-12 (each at score 4) — all targeted for **de-risking in M2** (W5–W7). If all four are de-risked by the end of W7, v1 ship confidence is High. If R-1 fires, v1 ships on a reduced platform matrix (Android + Windows + macOS Catalyst; iOS deferred to v1.1). If R-3 fires (MRL 128d multimodal too lossy), the cold-tier floor is raised to 256d (storage doubles, still 3× under hot tier). If R-12 fires (FS watcher unreliable), the reconciliation sweep frequency is increased from every 4 hours to every 1 hour.

---

## 10. Test Strategy

### 10.1 Test pyramid

```
                    ┌────────────────┐
                    │ Manual / Dogfood│  ~5% — weekly dogfood sessions
                    └────────┬───────┘
                  ┌──────────┴───────┐
                  │ End-to-End (UI)  │  ~10% — Playwright-style harness against MAUI Blazor Hybrid
                  └──────────┬───────┘
                ┌────────────┴───────┐
                │ Corpus / Golden    │  ~15% — golden-query regression; 200 queries × 4 langs
                └──────────┬─────────┘
              ┌────────────┴─────────┐
              │ Integration (pipeline)│  ~25% — capture pipeline + query pipeline + MRL
              └──────────┬───────────┘
            ┌────────────┴───────────┐
            │ Performance (CI gate)  │  ~5% — BenchmarkDotNet; CI-blocking
            └──────────┬─────────────┘
          ┌────────────┴─────────────┐
          │ Unit                     │  ~40% — pure-logic; ~70% line coverage
          └──────────────────────────┘
```

### 10.2 Test categories

| Category | Framework | Scope | CI gate? |
|---|---|---|---|
| Unit | xUnit + FluentAssertions | Pure logic in `Recall.Core`; mock ONNX + ZVec | Yes — 70% coverage |
| Integration | xUnit | `Recall.Core` end-to-end with real ONNX + real ZVec.NET on the host machine | Yes — all green |
| Performance | BenchmarkDotNet | Insert + Query + MRL benchmarks; per-platform | Yes — fails on > 10% regression |
| Corpus (golden queries) | xUnit + `Recall.CorpusTests` | 200 queries × 5 langs = 1000 tests; recall@10 vs ground truth | Yes — must not regress |
| UI end-to-end | Playwright against MAUI Blazor Hybrid WebView | Search → click result → assert deep-link; Capture → assert indexed; Pin → assert tier change | Nightly — non-blocking on PR |
| Manual / dogfood | QA + team | Weekly dogfood on each platform; structured test script | Pre-release gate |

### 10.3 Unit test conventions

- **Naming:** `MethodName_StateUnderTest_ExpectedBehavior`. Example: `MrlTieringPolicy_RecencyDecay_PromotesHotItemTo768d`.
- **Arrange-Act-Assert** with explicit `// Arrange`, `// Act`, `// Assert` comments.
- **No `async void`** in tests; use `async Task` and `FluentAssertions` `Should().Be()`.
- **No real ONNX or ZVec in unit tests** — these are mocked via `IOnnxEncoderSession` and `IZVecCollection`. Real ONNX + ZVec are exercised only in integration tests.
- **Property-based tests** for the MRL tiering policy: `FsCheck` generates random (item-age, pinned, last-access) tuples and asserts invariants (tier ∈ {768, 512, 256, 128}; pinned ⇒ 768; promote-on-access ⇒ tier monotonically increases over time).

### 10.4 Integration test fixtures

A `RecallFixture` (xUnit `IAsyncLifetime`) builds the real environment once per test class:

```csharp
public sealed class RecallFixture : IAsyncLifetime
{
    public IOnnxEncoderSession TextEncoder { get; private set; } = null!;
    public IOnnxEncoderSession FullEncoder  { get; private set; } = null!;
    public IZVecCollection<RecallItem> Collection { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddRecallCore();
        services.AddZVec(cfg => cfg.Path = TestPaths.DbFile);
        services.AddSingleton<IOnnxEncoderSession>(sp =>
            OnnxEncoderSession.Load(TestPaths.TextEncoderOnnx));
        var sp = services.BuildServiceProvider();
        TextEncoder = sp.GetRequiredService<IOnnxEncoderSession>();
        // ... FullEncoder + Collection
    }

    public async Task DisposeAsync()
    {
        await Collection.DisposeAsync();
        File.Delete(TestPaths.DbFile);
    }
}
```

Integration tests inherit from `IClassFixture<RecallFixture>` and get the real ONNX + ZVec stack for free.

### 10.5 Golden-query corpus (regression harness)

See §11 for the corpus spec. The regression harness runs:

1. Build a fresh ZVec.NET collection at the canonical corpus (10k items).
2. For each of the 200 golden queries × 5 languages (1000 test cases):
   - Embed the query with the search task prefix
   - Run hybrid search + RRF, top-20
   - Compare top-10 against the ground-truth labels
   - Pass if recall@10 ≥ threshold (default: 0.85 for "easy" queries, 0.65 for "hard")
3. Aggregate pass rate per language; must not regress by > 2 points vs the previous commit.

A non-blocking nightly run generates a recall-quality trend chart. A blocking PR run runs a 50-query subset (10 per language) for fast feedback.

### 10.6 Performance test (CI gate)

The `Recall.Benchmarks` project uses `BenchmarkDotNet` with the `[MemoryDiagnoser]` attribute on each benchmark. CI runs the harness on a fixed-RID machine (Pixel 6a connected via USB, or x86_64 runner) and compares against a stored baseline:

- `master` branch's last green run is the baseline.
- PR run with > 10% regression on any benchmark fails the gate.
- PR run with > 5% improvement prompts a baseline update.

### 10.7 Crash & recovery test

A dedicated test harness:

1. Spawns the Recall process via `dotnet maui` on Android / desktop.
2. Triggers a 1k-item ingestion batch.
3. Mid-batch, sends `kill -9` (or equivalent on each platform).
4. Restarts the process.
5. Asserts: DB opens cleanly; 0 corrupt vectors; op-log replays correctly; no data loss for items that completed pre-kill.
6. Repeats 100× to surface any intermittent corruption.

This is the **single most important test** for a local-only app — there is no server to recover from. It must pass in M3 before v1.0 ships.

### 10.8 Security / privacy test

- **Egress capture:** Run the app for 60 minutes under a `mitmproxy`-style local proxy; assert 0 bytes to non-local IPs (allow OS push only if explicitly opted in).
- **BannedApiAnalyzers audit:** `dotnet build -p:RunBannedApiAnalysis=true` runs in CI; fails on any network call outside the `Recall.Core.Sync` namespace (which is empty in v1).
- **DB file inspection:** Open the ZVec.NET DB file in a hex viewer; assert no plaintext PII is recoverable (vectors are floats; metadata is structured; only the original-media path references are present, and those resolve to local files only).

---

## 11. Data Corpus Specification

### 11.1 Why a canonical corpus

Recall is fundamentally a retrieval system. To prove the system works, to gate CI on regressions, and to A/B MRL truncation strategies, we need a **fixed, version-controlled corpus** with **ground-truth queries and labels**. The corpus is built once at the start of M1 and versioned alongside the code.

### 11.2 Corpus shape

| Property | Value |
|---|---|
| Total items | **10,000** (small enough to fit on a dev device in minutes; large enough to expose scaling issues at M3 scale-up) |
| Modalities | Text (40%), Images (35%), Audio (15%), Video frames (10%) |
| Languages | EN, AR, ZH, HI, FR (each ≥ 15% of items) |
| Time distribution | Spanning 12 months; cluster density around the last 90 days (simulates "hot" tier) |
| Source datasets | MS MARCO (text), COCO captions (image), Common Voice (audio), Kinetics-700 sampled frames (video) |
| Storage on dev device | ~3 GB (mostly original media; vectors are ~12 MB at 128d tier) |
| Format | One JSONL file per modality: `corpus/text.jsonl`, `corpus/image.jsonl`, `corpus/audio.jsonl`, `corpus/video.jsonl` |
| Item schema | `{ "id": "uuid", "modality": "text|image|audio|video", "captured_at": "ISO8601", "lang": "en", "source": "msmarco:1234", "content_ref": "file://.../item_1234.txt", "ground_truth_label": "red scooter" }` |

### 11.3 Golden queries

200 hand-curated queries, each with a labeled top-10 ground truth from the corpus. The queries are split into "easy" (single-modality, single-language, high-recall expected) and "hard" (cross-modal, cross-lingual, low-recall expected).

| Query type | Count | Example |
|---|---|---|
| Text → Text (same lang) | 60 | "the red scooter near the Colosseum" → text note |
| Text → Image (cross-modal) | 40 | "a red scooter" → image of red scooter |
| Text → Audio (cross-modal) | 30 | "voice memo about the Colosseum" → audio file |
| Cross-lingual (text EN → text AR/zh/hi/fr) | 30 | "red scooter" (en) → "scooter أحمر" (ar) |
| Audio → Image (cross-modal) | 20 | whistle recording → image of whistle |
| Image → Text (reverse cross-modal) | 20 | screenshot → text describing it |
| **Total** | **200** | — |

Each query is run in **5 languages** (EN, AR, ZH, HI, FR), giving **1000 test cases**. Pass criteria:

- "Easy" queries: recall@10 ≥ 0.85
- "Hard" queries: recall@10 ≥ 0.65
- Cross-lingual parity: recall@10 within ±5 percentage points across the 5 languages for the same query

### 11.4 Builder script

`tools/corpus-builder/Program.cs` assembles the corpus from public datasets:

1. Download MS MARCO passage subset (1k passages, multilingual via mMARCO).
2. Sample 1k images from COCO 2017 with their captions.
3. Sample 1k audio clips from Common Voice (200 per language).
4. Sample 1k video frames from Kinetics-700 (200 videos × 5 frames each).
5. Hand-label 200 golden queries against the assembled corpus.
6. Serialize to JSONL.
7. Compute SHA-256 of each item's content; embed into the corpus for content-addressable ID generation.

The build is deterministic (pinned dataset versions, fixed seeds). Re-running it produces a byte-identical corpus.

### 11.5 Scale-up corpus (for M3)

A separate **100k-item corpus** built by repeating the 10k corpus 10× with synthetic permutations (timestamp shifts, language remixes). Used only for the M3 perf sweep; not for golden-query regression.

### 11.6 Corpus versioning & evolution

- Corpus v1 = M1 baseline (1k items).
- Corpus v2 = M2 baseline (10k items + 200 golden queries).
- Corpus v3 = M3 baseline (10k items, refined golden queries after dogfood feedback).
- Each version is tagged `corpus-vN` in git. The regression harness pins to a corpus version per branch.

### 11.7 License

All source datasets are licensed for redistribution:
- MS MARCO: MIT-like research use.
- COCO: CC-BY 4.0.
- Common Voice: CC0.
- Kinetics-700: research use; we redistribute frame samples only, not the source videos.

The corpus is stored in a private repo (`recall-corpus`) accessible to CI; dev devices fetch a pinned version on first run.

---

## 12. Sync Design (Deferred — Design Only)

> **This section is a design reference. v1 ships sync-disabled. v1.5 implements this design as written.**

The sync design is included in v1 because the v1 schema must be **sync-ready** — i.e., vector IDs must be content-addressable and an op-log must be append-only — so that v1.5 sync can be added without an architectural rework. The design itself is fully specified here.

### 12.1 Sync requirements

1. **No server.** Sync is peer-to-peer between the user's own devices over LAN (Wi-Fi Direct, mDNS) or, opt-in, over a relayed Tor-like overlay for non-co-located devices.
2. **End-to-end encrypted.** Devices exchange vectors only after encrypting them with a key derived from a shared secret established via a PAKE (e.g., OPAQUE) on first pairing. The relay (if used) sees ciphertext only.
3. **Conflict-free.** Vector IDs are content-addressable, so two devices inserting the same content (e.g., both clipped the same article) produce the same ID and dedupe naturally. Op-log entries are CRDT-style: `INSERT` is idempotent; `DELETE` is a tombstone; `PIN`/`UNPIN` are last-writer-wins with vector clocks.
4. **Tier-aware.** A vector stored at 128d on device A and 256d on device B should sync gracefully: the higher-tier copy wins; the lower-tier copy is upgraded (lazy re-embed from source on the recipient device, if the source media is also synced).
5. **Source-media optional.** Vectors can sync without the original media (e.g., a phone captures a screenshot, embeds it, syncs the vector to a laptop; the laptop cannot render the image unless the source is also synced). The UI shows a "media not on this device" placeholder for these items.

### 12.2 Content-addressable vector IDs

```
vector_id = SHA-256(
    modality            :: "text" | "image" | "audio" | "video",
    source_content_hash :: SHA-256 of the original media bytes,
    dim_tier            :: "768" | "512" | "256" | "128"
)
```

The ID is the SHA-256 over a length-prefixed concatenation. Two devices that capture the same article text and embed it at the 256d tier produce the same `vector_id` byte-for-byte. Dedup is trivial: `INSERT` of an existing `vector_id` is a no-op.

### 12.3 Op-log

Append-only, write-ahead, file-per-device:

```
file: oplog/<device_id>.log
record: { ts: i64, op: "INSERT"|"DELETE"|"PIN"|"UNPIN", vector_id: 32 bytes,
          metadata: { modality, captured_at, lang, source_path, dim_tier },
          vector_ciphertext: bytes (encrypted under the device-pair session key) }
```

The op-log is the source of truth for what to sync. A peer reads the op-log incrementally from the last acknowledged offset, decrypts each entry, and applies the operation to its local ZVec.NET collection. Conflict resolution is trivial because `INSERT` is idempotent (content-addressable) and `DELETE`/`PIN`/`UNPIN` use last-writer-wins with monotonic logical clocks.

### 12.4 Key hierarchy

```
root_secret = PAKE-established on first device pairing (OPAQUE)
pair_key    = HKDF(root_secret, info="recall-pair-key-v1")
session_key = HKDF(pair_key, info="session-" || epoch, salt=nonce)
op_log_key  = HKDF(session_key, info="op-log-encryption")
vector_key  = HKDF(session_key, info="vector-encryption")
metadata_key= HKDF(session_key, info="metadata-encryption")
```

Each layer derives the next via HKDF-SHA256. The op-log entries are encrypted under `op_log_key` with AES-256-GCM (12-byte nonce, 16-byte tag). The vector ciphertext (for vectors that are synced in the clear op-log) uses `vector_key`. Metadata uses `metadata_key`. The session key is rotated on a schedule (default: every 30 days) to limit key-exposure windows.

### 12.5 Transport

| Transport | When | Mechanism |
|---|---|---|
| LAN mDNS | Devices on same Wi-Fi | Bonjour / Avahi; gRPC over TCP |
| Wi-Fi Direct / Apple Wireless Direct | Co-located, no Wi-Fi | Native platform API; gRPC over TCP |
| Bluetooth Low Energy | Co-located, low bandwidth | GATT characteristics; small op-log entries only |
| Relayed overlay (opt-in) | Devices not co-located | A self-hosted Tor-like circuit; the relay sees ciphertext only |

The relayed overlay is **opt-in** in v1.5; users who do not enable it can sync only when co-located. The relay does not have a private key; it forwards ciphertext between paired devices based on a route token derived from the `pair_key`.

### 12.6 Sync flow

```
Device A                                  Device B
   │                                          │
   │ 1. mDNS advertises "recall-sync"         │
   │ <────────────────────────────────────   │
   │                                          │
   │ 2. PAKE pairing (first time)            │
   │ ───────────── OPAQUE flow ───────────►  │
   │ ◄─────────────────────────────────────  │
   │                                          │
   │ 3. Establish session keys                │
   │ ─────────── HKDF + ECDH ────────────►   │
   │ ◄─────────────────────────────────────   │
   │                                          │
   │ 4. Op-log sync (incremental)             │
   │ ── oplog range request (last_offset)─►  │
   │ ◄─── encrypted op-log entries ────────  │
   │                                          │
   │ 5. Apply entries to local ZVec           │
   │ - decrypt under op_log_key              │
   │ - INSERT: idempotent; check dim_tier    │
   │   winner = max tier (768 > 256 > 128)    │
   │   if local is lower, upgrade (lazy re-  │
   │   embed from source if synced)           │
   │ - DELETE: tombstone                     │
   │ - PIN/UNPIN: LWW with vector clock      │
   │                                          │
   │ 6. Ack offset                           │
   │ ───────────── ack offset ───────────►   │
```

### 12.7 Conflict resolution

| Op | Conflict | Resolution |
|---|---|---|
| INSERT vs INSERT (same `vector_id`) | None — idempotent | Higher tier wins; lower tier upgraded lazily |
| INSERT vs DELETE | DELETE wins | The tombstone stays; the INSERT is suppressed |
| PIN vs UNPIN | LWW | Last-writer-wins with logical clock; ties broken by `device_id` |
| DELETE vs DELETE | None — idempotent | Both tombstones coalesce |
| Tier upgrade | Both have the same `vector_id` at different tiers | Higher tier wins; lower is replaced; re-embed source on the lower-tier device if available |

### 12.8 Failure modes

- **Peer offline during sync** — sync resumes from the last acknowledged offset when the peer reconnects; no data loss.
- **Op-log corruption** — the op-log is append-only; a torn write at the tail is detected by a length-prefix check and the tail entry is discarded.
- **Key compromise** — session keys are rotated every 30 days; a compromised session key exposes at most 30 days of op-log entries. The `pair_key` can be rotated by re-running PAKE (requires both devices online).
- **Asymmetric source media** — if device A has the source media for a vector and device B does not, the vector still syncs (the ciphertext is independent of the source). Device B shows a "media not on this device" placeholder. The source media can be transferred separately via the same op-log channel (an additional `INSERT_MEDIA` op type) — this is a v1.5+ extension.

### 12.9 Why this design fits the local-first thesis

The sync layer is the **only** part of Recall that touches the network, and it is engineered so that:

1. **No server ever sees plaintext.** The relay (if used) sees ciphertext only.
2. **No PII ever leaves the device unencrypted.** Metadata, source paths, and vector floats are all encrypted under session keys derived from a PAKE-established shared secret.
3. **The user can opt out.** Sync is opt-in. A user who never pairs a second device has zero network code paths active.
4. **The v1 schema is forward-compatible.** Vector IDs are content-addressable; the op-log is append-only; the `ISyncAdapter` interface exists in v1 code as a stub. v1.5 implements the adapter as designed; no schema migration is required.

This is the **only** way to add multi-device sync to a local-first app without violating the no-egress anti-goal. Any sync design that routes plaintext through a server breaks the thesis; any sync design that requires a server-mediated pairing breaks the no-server goal. The PAKE + E2EE + relayed-overlay pattern is the only one that fits.

---

## 13. RACI & Engineering Org

### 13.1 Roles

| Role | Who (placeholder) | Responsibility |
|---|---|---|
| Tech Lead | TBD | Architecture, blocking decisions, risk owner |
| ML Engineer | TBD | EmbeddingGemma 2 ONNX integration, MRL policy, RRF tuning |
| Backend Engineer | TBD | ZVec.NET collection, op-log, query pipeline, sync design |
| Mobile Engineer (Android) | TBD | Android platform head, capture services, battery |
| Mobile Engineer (iOS) | TBD | iOS platform head, App Review, ReplayKit |
| Desktop Engineer | TBD | Windows + macOS Catalyst platform heads, screen capture |
| Full-stack (UI) | TBD | Razor components, ViewModels, result cards |
| Infra / CI | TBD | GitHub Actions, runners, signing certs |
| Perf Engineer | TBD | BenchmarkDotNet harness, perf budget, regression gate |
| QA Engineer | TBD | Golden-query harness, corpus builder, crash tests |
| Designer | TBD | UI design system, icons, localization |
| Product | TBD | Store listings, privacy policy, App Review responses |

### 13.2 RACI matrix (selected)

| Activity | Tech Lead | ML Eng | Backend | Mobile | Desktop | UI | Infra | Perf | QA | Product |
|---|---|---|---|---|---|---|---|---|---|---|
| Architecture decisions | **R/A** | C | C | C | C | C | I | C | I | I |
| ONNX encoder integration | A | **R** | C | I | I | I | I | C | C | I |
| ZVec.NET collection | A | C | **R** | I | I | I | I | C | C | I |
| Capture services (per-platform) | A | I | I | **R** | **R** | I | I | I | C | I |
| Razor UI | A | I | C | I | I | **R** | I | I | C | C |
| CI / build matrix | A | I | I | I | I | I | **R** | C | C | I |
| Perf budget | A | C | C | C | C | I | I | **R** | C | I |
| Risk register | **R/A** | C | C | C | C | I | C | C | C | C |
| Sync design (deferred) | A | C | **R** | I | I | I | I | I | C | I |
| App Review responses | I | I | I | C | I | I | I | I | I | **R/A** |

**R** = Responsible, **A** = Accountable, **C** = Consulted, **I** = Informed.

### 13.3 Cadence

- **Daily standup** — 15 min; blocker-focused; async in Slack otherwise.
- **Weekly risk review** — 30 min; go through the top-10 risk register; update burn-down.
- **Bi-weekly architecture review** — 60 min; ADRs presented; decisions logged.
- **End-of-milestone retrospective** — 90 min; what worked, what didn't, what to change for the next milestone.

### 13.4 Decision-making

- **ADRs** (Architecture Decision Records) in `docs/adr/`; numbered; PR-reviewed.
- **RFCs** for non-trivial changes; 1-week comment period.
- **Tech Lead is the tiebreaker** on disputed ADRs; rationale logged in the ADR.

---

## 14. Exit Criteria

### 14.1 M1 exit (Day 30)

| Criterion | Verification |
|---|---|
| Android `dotnet publish` succeeds | CI build green on `android-arm64` |
| 1k-item seed corpus ingested on Pixel 6a in < 5 min | Manual run; log captured |
| Golden query p95 < 80 ms on Pixel 6a at 1k items | BenchmarkDotNet result file in CI artifacts |
| "Red scooter" demo recorded | Video in `docs/demos/m1-demo.mp4` |
| Code coverage ≥ 60% on `Recall.Core` | SonarCloud report |
| All v1 anti-goals enforced by `BannedApiAnalyzers` | CI gate green |
| R-1, R-2 de-risking plan written | ADR in `docs/adr/0005-m2-derisking.md` |

### 14.2 M2 exit (Day 60)

| Criterion | Verification |
|---|---|
| All four RIDs build green in CI | CI matrix green |
| 10k-item corpus ingested on Pixel 6a in < 60 min | Manual run; log captured |
| Golden query p95 < 50 ms on Pixel 6a at 10k items | BenchmarkDotNet result |
| Golden query p95 < 20 ms on x86 laptop at 10k items | BenchmarkDotNet result |
| All four modalities (text, image, audio, video frame) searchable | Manual smoke; golden-query results include all 4 modalities |
| MRL tiering verified: storage at 10k items ≤ 12 MB on mobile | DB file size in artifacts |
| RRF ablation: golden-query recall@10 with RRF ≥ without RRF | Regression test report |
| 5 languages (EN, AR, ZH, HI, FR) render correctly | Manual smoke per language |
| R-1, R-2, R-3, R-4, R-5 all de-risked | Risk register updated; burn-down chart |
| Sync design ADR signed off | ADR in `docs/adr/0010-sync-design.md` |

### 14.3 M3 exit (Day 90) — v1.0 RC

| Criterion | Verification |
|---|---|
| 100k-item corpus; p95 query latency on Pixel 6a ≤ 50 ms | BenchmarkDotNet result |
| Crash-recovery: kill -9 mid-insert on 1k-batch produces zero corruption across 100 trials | Crash test report |
| Perf gate green in CI on all four RIDs | CI gate status |
| All five localizations render correctly | Manual smoke + screenshots in `docs/localization/` |
| Threat model walkthrough signed off | Threat model doc in `docs/security/threat-model.md` |
| v1.0-rc1 tag cut and dogfooded for ≥ 5 days | Dogfood session notes |
| App Store / Play Store / Microsoft Store / Mac App Store listings ready | Listing drafts in `docs/store/` |
| Privacy policy reviewed | Legal sign-off |
| Telemetry and crash reporter are opt-in with clear UX | Manual smoke |
| **v1.0.0 tag and ship** | Release notes published |

---

## 15. Appendix: Glossary & References

### 15.1 Glossary

| Term | Definition |
|---|---|
| **ANN** | Approximate Nearest Neighbor. The class of algorithms (HNSW, IVF, Vamana, DiskANN) that find near-neighbors in sub-linear time, trading recall for speed. |
| **AOT** | Ahead-of-Time compilation. .NET Native AOT produces a trimmed, single-file binary with no JIT; cold-start is in milliseconds. |
| **Blazor Hybrid** | A hosting model where Razor components run inside a native WebView hosted by a MAUI shell; UI code is shared across platforms. |
| **CRDT** | Conflict-free Replicated Data Type. A data structure that can be merged across replicas without coordination; semantics are defined per operation type. |
| **E2EE** | End-to-End Encryption. Cryptographic scheme where only the communicating parties (the user's own devices) can read the messages; intermediaries (relays) see ciphertext only. |
| **EmbeddingGemma 2** | Google DeepMind's 740M modular multimodal embedding model; supports text, image, audio, video in one shared 768-d space; native MRL truncation to 128/256/512/768. |
| **FTS** | Full-Text Search. BM25-style ranking over text payloads; complement to dense ANN. |
| **HNSW** | Hierarchical Navigable Small World graph. The default ANN index in ZVec.NET; sub-linear search with tunable recall via `ef_search`. |
| **HNSW-RaBitQ** | Quantized HNSW using 1-bit RaBitQ codes; cuts memory ~8–16× with minimal recall loss; x86_64 + AVX2 only. |
| **HKDF** | HMAC-based Key Derivation Function. RFC 5869; derives one or more keys from a master secret. |
| **MAUI** | .NET Multi-platform App UI. Microsoft's cross-platform native-app framework for Android, iOS, Windows, macOS. |
| **MRL** | Matryoshka Representation Learning. Training technique that produces embeddings where the first N dims are a usable embedding for any N ∈ {128, 256, 512, 768}; enables storage density tiering. |
| **ONNX Runtime** | Cross-platform inference engine for ONNX models; supports CPU + GPU + NPU execution providers per platform. |
| **OPAQUE** | Asymmetric PAKE protocol; resists offline brute-force; used for first-pair device authentication. |
| **PAKE** | Password-Authenticated Key Exchange. Cryptographic protocol that establishes a shared secret from a low-entropy password without exposing it to offline attack. |
| **p95 / p99** | 95th / 99th percentile latency. p95 is the latency that 95% of operations beat. |
| **RaBitQ** | Quantization scheme that uses 1-bit codes plus a learned re-ranking step; rivals full-precision recall at 1/8 the memory. |
| **Recall** | The flagship on-device multimodal "second brain" application; the subject of this document. |
| **RID** | Runtime Identifier. .NET's per-target label (`android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64`). |
| **RRF** | Reciprocal Rank Fusion. A rank-aggregation method that fuses multiple ranked lists into one; `score = Σ 1/(k + rank_i)`. |
| **Task prefix** | A short text instruction prepended to the input to steer the embedding model into a particular behavior mode (search, cluster, classify, semantic similarity). |
| **Vamana** | Static-dataset ANN index optimized for one-time-build, query-forever workloads; ships in ZVec.NET. |
| **ZVec** | Alibaba's open-source in-process vector database; "the SQLite of vector databases". |
| **ZVec.NET** | The .NET SDK for ZVec; provides `SafeZvecHandle` lifecycle, typed ODM, hybrid search, in-DB rerankers, mobile RIDs. |

### 15.2 External references

- **Source brief:** `IdeaTank_Embeddinggemma_2_ZVecNET.md` (uploaded; §3 = Recall).
- **Companion doc:** `Recall_App_Technical_Details.md` (same `download/` directory).
- **EmbeddingGemma 2 model card:** <https://huggingface.co/google/embeddinggemma-2>
- **EmbeddingGemma 2 ONNX export:** <https://huggingface.co/onnx-community/embeddinggemma-2-ONNX>
- **ZVec.NET GitHub:** <https://github.com/ahmedSamir50/AdamSystems.ZVec.NET>
- **ZVec.NET docs:** <https://ahmedsamir50.github.io/AdamSystems.ZVec.NET/>
- **Alibaba ZVec:** <https://github.com/alibaba/zvec>
- **ZVec architecture blog:** <https://zvec.org/en/blog/2026-04-29-zvec-architecture>
- **.NET MAUI docs:** <https://learn.microsoft.com/dotnet/maui>
- **Blazor Hybrid docs:** <https://learn.microsoft.com/aspnet/core/blazor/hybrid>
- **ONNX Runtime C# API:** <https://learn.microsoft.com/dotnet/api/microsoft.ml.onnxruntime>
- **OPAQUE RFC draft:** <https://www.ietf.org/archive/id/draft-irtf-cfrg-opaque-13.html>

### 15.3 Internal references (to be created)

- `docs/adr/` — Architecture Decision Records (numbered).
- `docs/security/threat-model.md` — Threat model for v1.
- `docs/store/` — Store listings per platform.
- `docs/demos/` — Demo recordings per milestone.
- `docs/localization/` — Localization smoke screenshots.
- `recall-corpus/` (private repo) — Canonical corpus and golden queries.

### 15.4 Deferred Epics (post-v1.5)

Three Epics are deliberately deferred from v1. They are tracked here so they are not forgotten. Each lands only behind explicit user opt-in.

#### Epic 1 — Ambient OS-signal capture

- **What:** background capture of clipboard, camera roll, screen OCR (foreground-only on iOS via `ReplayKit`), microphone.
- **Why deferred:** the "spying perception" risk (R-11) is too high for v1 launch. Users need to trust the app first; ambient capture is added later behind explicit opt-in.
- **When:** post-v1.5, gated behind explicit user selection ("Enable ambient capture" toggle, with per-source granularity).
- **Spec:** each source is its own toggle (clipboard, camera roll, screen, mic). Default off. Each toggle shows the OS permission sheet only when the user explicitly enables that source.

#### Epic 2 — Obsidian-style knowledge graph view (relabeled from Epic 3 in v1.2)

- **What:** a force-directed graph UI showing items as nodes and similarity / co-occurrence / cluster edges. Visual exploration of the user's second brain.
- **Why deferred:** pure UX polish. The search-result card view is sufficient for v1. Adding a graph view in M2/M3 steals time from the perf budget and crash-recovery work that gate v1.0.
- **When:** v1.5. The graph data (cluster IDs, similarity edges, cross-modal links) is **computed in v1** as part of the nightly clustering job; the v1.5 work is purely the viz UI (D3.js or vis.js in the Blazor WebView).
- **Spec:** nodes = items; edges = top-k similarity per item + cluster membership + temporal co-occurrence; rendering = force-directed layout in WebGL; interaction = click to expand neighbors, double-click to query.
- **Cluster labels:** computed in v1 by the **zero-LLM deterministic labeler** (Tech §16 Appendix D) — TF-IDF top term + temporal range + modality count, formatted in the cluster's natural language + user's locale. No LLM. The graph view displays these labels next to cluster nodes.

#### (Deprecated in v1.2) — Tiny LLM sidecar Epic

> **Removed in v1.2.** The original v1.1 Epic 2 proposed a ≥1B-param LLM sidecar (Granite 3B-06L or Qwen2.5-1.5B, INT4, ~600 MB) for nightly cluster title + summary generation. After user pressure-test ("1.5B / 600 MB for a single feature is wasteful"), this Epic is **killed**. The cluster label feature survives, but is implemented by the zero-LLM deterministic labeler instead (see Tech §16 Appendix D). The full reasoning behind the originally-proposed LLM use cases (and why each was either dropped or replaced by NER/rules/embedder) is preserved as a v1.1 historical record in Tech §16 Appendix D, §19.3.

### 15.5 Revision history

| Version | Date | Author | Change |
|---|---|---|---|
| 1.0 (draft) | 2026-10-09 | Recall Architecture | Initial draft for engineering review |
| 1.1 (revised) | 2026-10-09 | Recall Architecture | Eight user-driven scope refinements: (1) scope-only FS watcher capture model replaces ambient capture (deferred to Epic); (2) .NET 10 LTS replaces .NET 9 STS; (3) BM25 made explicit in hybrid query; (4) 740M encoder dropped from mobile v1 (audio = desktop-only in v1); (5) modality × tier shards (12 collections) replace single mixed collection; (6) ONNX assets downloadable, not bundled (first-run + on-demand); (7) SQLite `checkpoints.db` for ingestion cursors; (8) tiny LLM sidecar narrowed to one use case (cluster titles only) after user pressure-test. Risk register: R-2 closed, R-6 closed, R-10 downgraded, R-11 added (spying perception), R-12 added (FS watcher reliability). |
| 1.2 (revised) | 2026-10-09 | Recall Architecture | Tiny LLM sidecar **killed**. The v1.1 plan called for a ≥1B-param LLM (~600 MB INT4) for one nightly batch feature (cluster titles). After user pressure-test, replaced with a zero-LLM deterministic cluster labeler (TF-IDF + temporal + count, formatted in cluster's natural language + user's locale). ~30 lines of C#, no model, no download, no opt-in. Added anti-goal: "Tiny LLM sidecar of any size, for any use case" is now forbidden in v1 and v1.5. Plan §15.4 Epic 2 relabeled to "Obsidian-style knowledge graph view" (was Epic 3); the deprecated LLM Epic is preserved as a v1.1 historical record. Tech §16 Appendix D rewritten with the deterministic labeler design. |
