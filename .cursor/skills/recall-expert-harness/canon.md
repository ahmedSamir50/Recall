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
| TFM | `net10.0` (.NET 10 LTS). Not .NET 9. |
| UI host | MAUI Blazor Hybrid. Razor UI shared. No Blazor WebAssembly. No second Native AOT desktop codebase in v1. |
| RIDs | `android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64` |
| Vector DB | `AdamSystems.ZVec.NET` pinned at `1.0.0-beta.6+zvec.0.7.0` until a GA migration is decided in M3 |
| Embedder | EmbeddingGemma 2 via ONNX Runtime. Asset: `onnx-community/embeddinggemma-2-ONNX`. No fine-tune in v1. No cloud embedding API. |
| Checkpoints | SQLite `checkpoints.db` (ingestion cursors, job state, crash-recovery cursor) |
| Tests | xUnit, FluentAssertions, FsCheck for MRL invariants, BenchmarkDotNet for the perf gate |

EmbeddingGemma 2 facts the panel must not invent past:

- Native width **768**. MRL truncations in play: **768 / 256 / 128**. Floor is **128**. 512 exists on the model and is not a v1 storage tier.
- Text-only **270M**, text+vision **440M**, full multimodal **740M** (adds the audio encoder).
- Context **8192** tokens. Mean pooling. L2-normalize before cosine search.
- Task prefixes used by the product: `search`, `cluster`, `classify`.

## v1 behavior

- **Scope-only capture.** The user grants paths and per-path file types. Filesystem watchers (or the iOS foreground scan) are the ingest trigger. No clipboard, camera-roll, screen, or microphone capture in v1. Those belong to a post-v1.5 ambient epic behind an explicit opt-in that does not exist yet.
- **iOS** cannot background-watch the filesystem. v1 scans scoped paths when the app comes to the foreground and diffs them against `checkpoints.db`.
- **Modalities.** Mobile: text, images, sampled video frames (1 frame / 2 s) through the 270M and 440M encoders. Audio files on mobile are skipped and recorded as `skipped_modality=audio`. Desktop may load the 740M encoder on demand for audio, and must not keep it resident.
- **Models are downloaded, not bundled.** Base app bundle ≤ 50 MB. First run downloads the 270M text encoder from the Hugging Face CDN and checks SHA-256. The 440M vision encoder downloads the first time an image is indexed or queried. The 740M encoder is desktop-only and on demand. That download is the only network path v1 may have.
- **Twelve collections:** 4 modalities × 3 tiers (hot / warm / cold). Not one mixed collection.
- **Tiers.** Hot (default last 30 days) and user-pinned items stay 768-d. Warm (30–180 days) is 256-d. Cold (older) is 128-d. Access can promote an item back up. Do not store archival data at 768-d.
- **Query.** Hybrid dense ANN + BM25 full-text + scalar filter, fused with the in-database RRF reranker (`k = 60`). Bypassing RRF is a review block. Run the query once per active tier and fuse. Do not re-embed the corpus to change tiers.
- **Indexes.** Plain float32 HNSW on ARM (Android, iOS, Mac Catalyst). HNSW-RaBitQ only on `win-x64` (AVX2). Refuse RaBitQ on ARM. Refuse DiskANN on Windows and macOS. DiskANN is out of v1 entirely.
- **Zero LLM.** No sidecar, no chat, no generative summary, no model download for a language model, in v1 or v1.5. Cluster labels are deterministic: top TF-IDF term + locale date range + locale modality count. The cluster browser UI is v1.5; the label strings are specified now.
- **Sync is design-only.** Schema stays sync-ready (content-addressable ids, append-only op-log, `ISyncAdapter` stub, feature flag off). Do not implement pairing, transport, or encryption in v1.
- **Languages for the golden set:** English, Arabic, Chinese, Hindi, French. Cross-lingual recall@10 within ±5 points for the same query.

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
- Truncation below 128 dimensions.
- RaBitQ on ARM, or DiskANN on Windows/macOS.
- Loading the 740M encoder on mobile.
- Blazor WebAssembly as a target.
- A second desktop codebase beside MAUI in v1.
- Treating IdeaTank ambient-capture / web-dashboard / in-v1 P2P sync as the thing to build.

## Unresolved — do not pick a side silently

Record these as open questions in any doc review. They are real contradictions inside InitDocs, not permission to choose.

| ID | Conflict |
|---|---|
| U1 | Mobile disk at 100k items: the perf table says ≤ 80 MB; the storage math and goal G6 say ship at about **150 MB** (risk R-7). |
| U2 | RSS after warmup ≤ 400 MB on a Pixel, while the 270M text encoder is described as ~540 MB in float16 and is always resident. |
| U3 | Goal G7 is zero telemetry. M3 and the make/buy matrix add an opt-in crash reporter (Sentry or App Center) and usage counters. |
| U4 | "No scheduled scans" versus a filesystem reconciliation sweep (technical appendix, every 4 hours, tightened to 1 hour if watchers miss events). |
| U5 | Image full-text payload assumes OCR, but the named OCR engines (Tesseract / Apple Vision) sit on the removed screen-capture row. No v1 OCR engine is chosen. Audio transcription is parked, so audio BM25 payload is empty in v1. |
| U6 | CI is written as GitHub Actions. The project remote is Bitbucket. Do not scaffold a CI host until the user picks one. |
| U7 | Week-by-week M1 still says clipboard and camera-roll capture. M2 still puts the 740M audio encoder and a voice-memo recorder on Android and iOS. Those weeks lose to the v1.2 scope cuts. |
| U8 | Several diagrams and the solution tree still say .NET 9 and still list clipboard platform files and ONNX files bundled under `assets/onnx/`. |
| U9 | Perf rows for on-device 740M audio latency are written for phones that v1 will not load that encoder on. |

## Source files

- `InitDocs/IdeaTank_Embeddinggemma_2_ ZVecNET.md` — portfolio brief. Recall is the flagship. Many sibling product ideas are out of scope.
- `InitDocs/Recall_App_Implementation_Plan.md` — v1.2 plan.
- `InitDocs/Recall_App_Technical_Details.md` — v1.2 engineering notes, including Appendix D (labeler) and Appendix E (watchers). The table of contents lags the appendices.
