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
| UI host | MAUI Blazor Hybrid. Razor UI shared. Tailwind owns layout and screen chrome; MudBlazor only where it does not fight Tailwind (palette mapped via CSS variables in `tokens.css`). One `ShellUiStore` snapshot in `Recall.UI` is the only UI state source for theme, locale, route, and grants. No Blazor WebAssembly. No second Native AOT desktop codebase in v1. No web client. |
| RIDs | `android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64` |
| Vector DB | NuGet id ZVec.NET, version 1.0.0-beta.6. Source tree D:\A_S\AdamSystems.ZVec.NET has Version 1.0.0-beta.6+zvec.0.7.0. Do not restore a package named AdamSystems.ZVec.NET. |
| Embedder | EmbeddingGemma 2 via `Microsoft.ML.OnnxRuntime`. Asset: `onnx-community/embeddinggemma-2-ONNX`. No fine-tune in v1. No cloud embedding API. `Microsoft.ML.OnnxRuntimeGenAI` is not used. It is the text-generation loop, and iOS is not a supported OS. |
| Checkpoints | SQLite `checkpoints.db` (ingestion cursors, job state, crash-recovery cursor) |
| Tests | xUnit, FluentAssertions, FsCheck for MRL invariants, BenchmarkDotNet for the perf gate |

EmbeddingGemma 2 facts the panel must not invent past:

- Native width **768**. Stored widths in play: **768 / 512 / 256**. **128 is blocked for every stored vector.** Text is **512** until a golden run clears **256**. Image, video, and audio start at 768 and move to 512 when they pass the age cutoff. Phone and desktop use the same widths. One question is embedded once; shorter drawers use a cut of that same vector, then re-normalized.
- Text-only **270M**, text+vision **440M**, full multimodal **740M** (adds the audio encoder).
- Context **8192** tokens. The text trunk is 512 wide. A projection writes the stored embedding at 768. Mean pooling. The audio tower’s internal projection is 1536 and is not a stored width.
- L2-normalize the full 768 before cosine. After a Matryoshka cut, L2-normalize the slice again. A second model run is only for growing a vector whose tail was discarded.
- Text prefixes follow the model card, and they apply to text only. Search: `task: search result | query: {query}`. Cluster: `task: clustering | query: {content}`. Classify: `task: classification | query: {content}`. A titled document is `title: {title} | text: {content}`. Pictures, video, and audio get no prefix.

## v1 behavior

- **Scope-only capture.** The user grants paths and per-path file types. Search hits only those paths and types. A parent path overrides any child path in one tree check. Filesystem watchers (or the iOS foreground scan) are the ingest trigger. The user can press Index now, and the search page can offer to index an allowed path that is not indexed yet. No clipboard, camera-roll, screen, or microphone capture in v1.
- **iOS** cannot background-watch the filesystem. v1 scans scoped paths when the app comes to the foreground and diffs them against `checkpoints.db`.
- **Modalities.** Text, images, and sampled video frames. Audio is a separate encoder file, downloaded only if the user enables the audio file type, then unloaded after the job. Do not keep text, vision, and audio loaded together. There is no microphone recorder. Phone audio quality at 4-bit is still unmeasured.
- **No ASR, ever.** Do not add a speech-to-text model. If a known transcript file sits next to the media and shares its name, read that text and attach it to the media item. Known types: `.srt`, `.vtt`, `.sbv`, `.ass`, `.ssa`, `.ttml`. Examples: `talk.mp4` with `talk.srt` or `talk.en.vtt`. The words are searchable on that audio or video. The transcript file is not a second, unlinked document. Do not open the media container to pull a hidden caption track.
- **Models are downloaded, not bundled.** The installer size is not a hard fail. Weights never ship inside it. Download each enabled ONNX piece into the platform app-data folder and check SHA-256. We may generate and upload our own quants. The published 4-bit files are not the quality target. ONNX Runtime on phone and PC runs float32, int8, and int4. Int8 is the almost-no-loss file we can generate. Q5 and Q6 are GGUF names this runtime will not run. Do not run float16. That download is the only network path v1 may have. The HTTP client lives in the MAUI download adapter, not in `Recall.Core`.
- **Collections.** Text is one collection at one width. Image, video, and audio may have a long collection and an old collection. One width inside each collection. Do not mix types. Do not keep both copies of the same item.
- **Old image, video, and audio vectors are shortened to 512** until a golden run clears 256. 128 stays blocked. Delete the long row when the short row is inserted. A pin keeps the long vector. Text stays at 512 and does not age. The person chooses the cutoff in the app: **2, 5, or 7 years** of file age. The starting value is 2 years.
- **Query.** Hybrid dense ANN + BM25 full-text + scalar filter, fused with the in-database RRF reranker (`k = 60`). Bypassing RRF is a review block. Embed the question once, cut and re-normalize to each target collection’s width, and search each enabled collection. Do not re-embed the corpus to change a width.
- **Indexes.** Plain HNSW on all four RIDs. `QuantizeType` stays `Undefined` (one float32 index). Turning it on builds a second index beside the float one (`segment.cc` creates both). That does not shrink the file. Enable `Int8`, `Int4`, or `Fp16` only after a benchmark shows query time about 2× or 3× faster. Do not create `ZVecHnswRabitqIndexParam`. DiskANN is out of v1.
- **Zero LLM.** No sidecar, no chat, no generative summary, no model download for a language model, in v1 or v1.5. Cluster labels are deterministic: top TF-IDF term + locale date range + locale modality count. The cluster browser UI is v1.5; the label strings are specified now.
- **Sync is design-only.** Schema stays sync-ready (content-addressable ids, append-only op-log, `ISyncAdapter` stub, feature flag off). Do not implement pairing, transport, or encryption in v1.
- **Device identity from day 0.** First launch mints a random device id and reads the OS device name. Both original values are kept forever. The person can edit a display name. The id is not a hardware id and not an advertising id. It stays on the device and is not sent with the model download.
- **User-facing copy:** Arabic and English only at the start (`ar`, `en`). This replaces the InitDocs five-locale UI list for product chrome. See `development-rules.md`.
- **Retrieval golden set:** English, Arabic, Chinese, Hindi, French. Cross-lingual recall@10 within ±5 points for the same query. This list is not the UI locale list.

## Solution shape

```
Recall.slnx
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

`Recall.Core` is headless-testable. The six platform ports are interfaces in Core. Adapters are written in Recall.Maui when that epic starts. `IIndexPolicy` stays an interface in Core. Width, age cutoff, and modality rules are Core types. The MAUI head does not choose them. v1 does **not** include `IClipboardService`, `ICameraRollService`, `IScreenCaptureService`, or `IAudioRecorderService`.

## Non-negotiables (block if proposed)

- Sending vectors, file contents, or other user data off the device.
- A cloud embedding API or a hosted vector database.
- An LLM of any size for any feature.
- A speech-to-text model. A transcript is used only when that file is already next to the media.
- Ambient OS capture in v1.
- Bundling ONNX weights in the app binary.
- Storing any vector at 128 dimensions or below.
- Creating HNSW-RaBitQ. The local SDK cannot. DiskANN on Windows or macOS.
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
| U1 | No ship disk number. InitDocs 80 MB and 150 MB are stale. QuantizeType stays off, so those cells are not a gate. |
| U2 | No ship RSS number. The 400 MB warmup cap and the 540 MB float16 encoder are both stale. Float16 inference is forbidden. Int8 text weights are about 314 MB. |
| U3 | Goal G7 is zero telemetry. M3 and the make/buy matrix add an opt-in crash reporter (Sentry or App Center) and usage counters. |
| U4 | Closed 13 Oct 2026: missed files are caught by Index now, and the search page offers to index an allowed path that is not indexed yet. A background timer is not required. |
| U5 | Closed 13 Oct 2026, revised the same day. A PDF with a text layer is extracted, not OCR'd. A scanned page is OCR'd. An image is OCR'd only for the words in it. The vision encoder still searches the picture without OCR. v1 readers are English and Arabic. One shared text finder. English uses official PP-OCRv6 small ONNX. Arabic is a separate ONNX reader, about 8 MB, because no official small ONNX model reads both. Other languages are extra downloads the person turns on. No speech-to-text model. A sibling `.srt`, `.vtt`, `.sbv`, `.ass`, `.ssa`, or `.ttml` is attached to the matching audio or video. |
| U6 | Closed 13 Oct 2026: the git remote is Bitbucket. Build and release stay on the developer machine. No cloud build. |
| U7 | Week-by-week M1 still says clipboard and camera-roll capture. M2 still puts the 740M audio encoder and a voice-memo recorder on Android and iOS. Those weeks lose to the v1.2 scope cuts. |
| U8 | Several diagrams and the solution tree still say .NET 9 and still list clipboard platform files and ONNX files bundled under `assets/onnx/`. |
| U9 | Phone 740M latency rows in InitDocs are stale. Audio on a phone, if enabled, uses the separate audio encoder file. Quality at int8 versus q4 is unmeasured. |
| U10 | Closed 13 Oct 2026: text is stored at 512 until a golden run clears 256. |
| U11 | Closed 13 Oct 2026: the person picks 2, 5, or 7 years. Starting value is 2 years. Old image, video, and audio then move to 512. |
| U13 | Closed 13 Oct 2026: `QuantizeType` stays off. A second index is created when it is on. Turn it on only if query time is about 2× or 3× faster. |
| U12 | Browser history is not a v1 feature. Phones cannot read it. A desktop switch is a later explicit connector. |

## Source files

- `InitDocs/IdeaTank_Embeddinggemma_2_ ZVecNET.md` — portfolio brief. Recall is the flagship. Many sibling product ideas are out of scope.
- `InitDocs/Recall_App_Implementation_Plan.md` — v1.2 plan.
- `InitDocs/Recall_App_Technical_Details.md` — v1.2 engineering notes, including Appendix D (labeler) and Appendix E (watchers). The table of contents lags the appendices.
