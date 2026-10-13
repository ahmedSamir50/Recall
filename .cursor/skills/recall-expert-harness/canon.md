# Recall working canon

Working canon for the expert panel until the user accepts a **Latest App Docs** set. `InitDocs/` is the initial idea (plan + technical details marked v1.2, plus the IdeaTank brief). It is **not** the final specification.

When a passage in `InitDocs/` conflicts with this file, flag the passage. Do not silently implement the stale sentence.

Narrative sections that currently carry v1.2 intent: Implementation Plan §1.1.1, §1.2, §1.3, §2. Latest diagrams, the solution tree (§5.1), the make/buy matrix (§8), and parts of the week-by-week plan were not fully rewritten after the scope cuts.

## Product

Recall is an on-device multimodal second brain. One .NET 10 MAUI Blazor Hybrid codebase ships to four native shells. v1 is local-only: no server, no accounts, no cloud embedder, no cloud vector database, no web client.

The moat is three constraints at once:

1. **Privacy** — user content and vectors do not leave the device.
2. **Latency** — embedder and vector database stay in the same process.
3. **Density** — Matryoshka truncation keeps a large personal corpus inside a phone memory budget.

## Stack (locked for v1 unless the user changes it)

| Piece | Choice |
|---|---|
| TFM | `net10.0` (.NET 10 LTS). Not .NET 9. .NET 11 RC1 (8 Sep 2026, go-live) is not the target until ONNX embedding and ZVec search are measured faster on `net11.0`. |
| UI host | MAUI Blazor Hybrid. Razor UI shared. No Blazor WebAssembly. No second Native AOT desktop codebase in v1. |
| RIDs | `android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64` |
| Vector DB | `AdamSystems.ZVec.NET` pinned at `1.0.0-beta.6+zvec.0.7.0` until a GA migration is decided in M3 |
| Embedder | EmbeddingGemma 2 via ONNX Runtime. Asset: `onnx-community/embeddinggemma-2-ONNX`. No fine-tune in v1. No cloud embedding API. |
| Checkpoints | SQLite `checkpoints.db` (ingestion cursors, job state, crash-recovery cursor) |
| Tests | xUnit, FluentAssertions, FsCheck for MRL invariants, BenchmarkDotNet for the perf gate |

EmbeddingGemma 2 facts the panel must not invent past:

- Native width **768**. Stored widths in play: **768 / 512 / 256**. **128 is blocked for every stored vector.** The user closed that on 13 Oct 2026. Text is 256 or 512, and that choice is still open. Image, video, and audio may stay at 512 or 768. A phone may store a shorter legal width than a desktop. One question is embedded once; shorter drawers use a cut of that same vector, then re-normalized.
- Text-only **270M**, text+vision **440M**, full multimodal **740M** (adds the audio encoder).
- Context **8192** tokens. Mean pooling. L2-normalize before cosine search.
- Task prefixes used by the product: `search`, `cluster`, `classify`.

## v1 behavior

- **Scope-only capture.** The user grants paths and per-path file types. Search hits only those paths and types. A parent path overrides any child path in one tree check. Filesystem watchers (or the iOS foreground scan) are the ingest trigger. The user can press Index now, and the search page can offer to index an allowed path that is not indexed yet. No clipboard, camera-roll, screen, or microphone capture in v1.
- **iOS** cannot background-watch the filesystem. v1 scans scoped paths when the app comes to the foreground and diffs them against `checkpoints.db`.
- **Modalities.** Text, images, and sampled video frames. Audio is a separate encoder file, downloaded only if the user enables the audio file type, then unloaded after the job. Do not keep text, vision, and audio loaded together. There is no microphone recorder. Phone audio quality at 4-bit is still unmeasured.
- **Models are downloaded, not bundled.** The installer size is not a hard fail. Weights never ship inside it. Download each enabled ONNX piece into the platform app-data folder and check SHA-256. We may generate and upload our own quants. The published 4-bit files are not the quality target. ONNX Runtime on phone and PC runs float32, int8, and int4. Int8 is the almost-no-loss file we can generate. Q5 and Q6 are GGUF names this runtime will not run. Do not run float16. That download is the only network path v1 may have. The HTTP client lives in the MAUI download adapter, not in `Recall.Core`.
- **One collection per file type** the user enabled (text, image, video, audio). Not one mixed collection. Not twelve age tiers. A collection holds one width.
- **Old image, video, and audio vectors are shortened.** The user chose this on 13 Oct 2026 because a 768→256 float32 cut is about 200 MB at 100,000 items and about 2 GB at 1,000,000. The safe shorter width is **512** until a golden run clears **256**. 128 stays blocked. Delete the long row when the short row is inserted. A pin keeps the long vector. Text stays one width (256 or 512, still open) and does not get this age split. The age cutoff itself is not locked.
- **Query.** Hybrid dense ANN + BM25 full-text + scalar filter, fused with the in-database RRF reranker (`k = 60`). Bypassing RRF is a review block. Embed the question once, cut and re-normalize to each target collection’s width, and search each enabled collection. Do not re-embed the corpus to change a width.
- **Indexes.** Plain float32 HNSW on ARM (Android, iOS, Mac Catalyst). HNSW-RaBitQ only on `win-x64` (AVX2). Refuse RaBitQ on ARM. Refuse DiskANN on Windows and macOS. DiskANN is out of v1 entirely.
- **Zero LLM.** No sidecar, no chat, no generative summary, no model download for a language model, in v1 or v1.5. Cluster labels are deterministic: top TF-IDF term + locale date range + locale modality count. The cluster browser UI is v1.5; the label strings are specified now.
- **Sync is design-only.** Schema stays sync-ready (content-addressable ids, append-only op-log, `ISyncAdapter` stub, feature flag off). Do not implement pairing, transport, or encryption in v1.
- **User-facing copy:** Arabic and English only at the start (`ar`, `en`). This replaces the InitDocs five-locale UI list for product chrome. See `development-rules.md`.
- **Retrieval golden set:** English, Arabic, Chinese, Hindi, French. Cross-lingual recall@10 within ±5 points for the same query. This list is not the UI locale list.

## Solution shape

```
Recall.sln
  src/Recall.Core/          # no MAUI, no UI
  src/Recall.UI/            # Razor class library, depends on Core abstractions
  src/Recall.Maui/          # composition root and platform heads
  src/Recall.Benchmarks/
  tests/Recall.UnitTests/
  tests/Recall.IntegrationTests/
  tests/Recall.BenchTests/
  tests/Recall.CorpusTests/
  tools/corpus-builder/
  tools/mrl-eval/
  docs/                     # future Latest App Docs (MkDocs), not created yet
  InitDocs/                 # historical seed, kept
```

`Recall.Core` is headless-testable. Platform services (`IFileScopeWatcherService`, `IScopeManagerService`, `IVideoFrameSamplerService`, `ISecureStorageService`, `IIdleDetectorService`, `IModelDownloadService`, `IIndexPolicy`) are interfaces in Core and implementations in the MAUI head. v1 does **not** include `IClipboardService`, `ICameraRollService`, `IScreenCaptureService`, or `IAudioRecorderService`.

## Non-negotiables (block if proposed)

- Sending vectors, file contents, or other user data off the device.
- A cloud embedding API or a hosted vector database.
- An LLM of any size for any feature.
- Ambient OS capture in v1.
- Bundling ONNX weights in the app binary.
- Storing any vector at 128 dimensions or below.
- RaBitQ on ARM, or DiskANN on Windows/macOS.
- Keeping text, vision, and audio encoders resident at the same time on a phone.
- Blazor WebAssembly as a target.
- A second desktop codebase beside MAUI in v1.
- Treating IdeaTank ambient-capture / web-dashboard / in-v1 P2P sync as the thing to build.
- Shipping user-facing copy that exists in only one language. Initial locales are `ar` and `en`.
- Calling an interface covered when any of its methods, or any `if` / `else` / `switch` / `try` / `catch` path, has no test. See `test-honesty.md`.

## Unresolved — do not pick a side silently

Record these as open questions in any doc review. They are real contradictions inside InitDocs, not permission to choose.

| ID | Conflict |
|---|---|
| U1 | Mobile disk at 100k items: the perf table says ≤ 80 MB; the storage math and goal G6 say ship at about **150 MB** (risk R-7). |
| U2 | RSS after warmup ≤ 400 MB on a Pixel, while the 270M text encoder is described as ~540 MB in float16 and is always resident. |
| U3 | Goal G7 is zero telemetry. M3 and the make/buy matrix add an opt-in crash reporter (Sentry or App Center) and usage counters. |
| U4 | Closed 13 Oct 2026: missed files are caught by Index now, and the search page offers to index an allowed path that is not indexed yet. A background timer is not required. |
| U5 | Closed 13 Oct 2026: no OCR in v1. Image full-text is empty. Audio transcripts are empty. |
| U6 | CI is written as GitHub Actions. The project remote is Bitbucket. Do not scaffold a CI host until the user picks one. |
| U7 | Week-by-week M1 still says clipboard and camera-roll capture. M2 still puts the 740M audio encoder and a voice-memo recorder on Android and iOS. Those weeks lose to the v1.2 scope cuts. |
| U8 | Several diagrams and the solution tree still say .NET 9 and still list clipboard platform files and ONNX files bundled under `assets/onnx/`. |
| U9 | Phone 740M latency rows in InitDocs are stale. Audio on a phone, if enabled, uses the separate audio encoder file. Quality at int8 versus q4 is unmeasured. |
| U10 | Text stored at 256 or at 512. Open until a golden recall run. |
| U11 | Age policy is chosen: old image, video, and audio move from the long width to 512. 256 waits on a golden pass. The cutoff age is not chosen. |
| U12 | Browser history is not a v1 feature. Phones cannot read it. A desktop switch is a later explicit connector. |

## Source files

- `InitDocs/IdeaTank_Embeddinggemma_2_ ZVecNET.md` — portfolio brief. Recall is the flagship. Many sibling product ideas are out of scope.
- `InitDocs/Recall_App_Implementation_Plan.md` — v1.2 plan.
- `InitDocs/Recall_App_Technical_Details.md` — v1.2 engineering notes, including Appendix D (labeler) and Appendix E (watchers). The table of contents lags the appendices.
