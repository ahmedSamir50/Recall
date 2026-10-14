# Recall technical notes

Status: semi-final technical notes, 13 Oct 2026. InitDocs/ is the historical seed and is not these notes.

Recall is an on-device multimodal second brain. One .NET 10 MAUI Blazor Hybrid process ships to four native shells. v1 is local-only: the embedder and the vector database stay in that process. User content and vectors stay on the device. There is no server, no account, and no cloud embedder.

The vector-database facts below were checked in the local `AdamSystems.ZVec.NET` tree.

## 1. Process and projects

Target framework is `net10.0` everywhere, including the MAUI heads `net10.0-android`, `net10.0-ios`, `net10.0-windows10.0.19041.0`, and `net10.0-maccatalyst`. Runtime identifiers are `android-arm64`, `ios-arm64`, `win-x64`, and `maccatalyst-arm64`. `win-arm64` is not a v1 identifier. `ios-arm64` is a beta risk: if that head fails, the other three still ship.

`Microsoft.ML.OnnxRuntime` and NuGet id `ZVec.NET` `1.0.0-beta.6` are in-process. `ZVec.NET` is not on nuget.org; this machine restores it from the local feed. Tokenizer text uses `Tokenizers.DotNet` 1.4.1 over `tokenizer.json`, because that file is BPE. Android and iOS have no tokenizer runtime in that package, so those shells fail closed. Windows uses `win-x64`. Mac Catalyst uses `osx-arm64`.

Solution shape:

- `src/Recall.Core/` — headless. No MAUI, no Razor, no HTTP.
- `src/Recall.UI/` — Razor class library. It depends on Core ports and does not host platform implementations.
- `src/Recall.Maui/` — the only composition root and the only place with an HTTP client.
- `src/Recall.Benchmarks/`, `tests/Recall.UnitTests/`, `tests/Recall.IntegrationTests/`, `tests/Recall.BenchTests/`, `tests/Recall.CorpusTests/`, `tools/corpus-builder/`, `tools/mrl-eval/`.

`Recall.Maui` calls `AddRecallCore` and `AddRecallAdapters`. The text ONNX session and the ZVec collection are created when a text job needs them. The HTTP client is `IHttpClientFactory`, used only by the model-download adapter. `BannedApiAnalyzers` makes `HttpClient`, `Socket`, `TcpListener`, and `UdpClient` a compile error inside `Recall.Core`, including `Recall.Core.Sync`.

Ports live in Core. Implementations live in the MAUI head:

| Port | Role |
|---|---|
| `IFileScopeWatcherService` | Filesystem events while the process is running |
| `IScopeManagerService` | Granted paths and the parent-wins tree check |
| `IVideoFrameSamplerService` | One frame every two seconds |
| `ISecureStorageService` | Present as an interface. v1 stores no sync keys |
| `IIdleDetectorService` | Pauses ingestion. It does not capture |
| `IModelDownloadService` | The only network path |
| `IIndexPolicy` | Stored width, age cutoff, and which modalities are on |

v1 has no clipboard, camera, screen, or microphone interfaces. Modality, stored width, age cutoff, and op-log operation are enums or static classes.

## 2. EmbeddingGemma 2

The embedder is EmbeddingGemma 2 from `onnx-community/embeddinggemma-2-ONNX`, run locally. There is no fine-tune in v1. Text, vision, and audio are separate ONNX files. Download a piece only when that type is enabled. Keep one `InferenceSession` per loaded mode, and do not keep all three loaded. After an audio job, unload the audio encoder. On mobile, serialize `Run` on each session with `SemaphoreSlim(1, 1)`. Pass `ReadOnlyMemory<float>` into ZVec.

File sizes of the published towers: text-only 270M, text plus vision 440M, full multimodal 740M once the audio encoder is added. Execution providers are NNAPI on Android, CoreML on iOS and Mac Catalyst, and DirectML on Windows. Fall back only when that provider is actually missing, and record the fallback. The runtime executes float32, int8, and int4. Int8 is the almost-no-loss file we can generate. The published 4-bit files are not the quality target. Phone audio quality at int8 versus that 4-bit file is unmeasured. Float16 inference is forbidden: it produces NaN.

The text trunk is 512 wide. A projection writes the stored embedding at 768. Mean-pool to that 768. The audio tower's internal projection is 1536 and is not a stored width. L2-normalize the full 768 before cosine. Matryoshka keeps the leading 512 or the leading 256 of that 768, then L2-normalizes the slice again. A second forward pass runs only to grow a vector whose tail was discarded, and that pass re-embeds from the file.

Context is 8192 tokens. Prefer one embedding when a note, memo, or article fits. Chunk only past that budget. Do not average chunk vectors.

Text prefixes come from the model card and are applied once, to text only:

- Search: `task: search result | query: {query}`
- Cluster: `task: clustering | query: {content}`
- Classify: `task: classification | query: {content}`
- A titled document: `title: {title} | text: {content}`

Pictures, video, and audio have no prefix.

Video is one frame every two seconds, sent through the vision encoder. The platform sampler is `MediaMetadataRetriever` on Android, `AVAssetImageGenerator` on iOS and Mac Catalyst, and `FFmpegInterop` on Windows. Batch those frames through the vision session. There is no microphone. If the person leaves the audio file type off, record `skipped_modality=audio` and skip the audio file.

## 3. Collections and age

Stored widths are 768, 512, and 256. A width of 128 is rejected for every stored vector. Phone and desktop use the same widths. One width per HNSW graph, so a collection never mixes widths or modalities.

Text is one collection and stays at 512 until a golden run clears 256. Text does not age. Image, video, and audio each have a long collection at 768 and an old collection at 512. Example names: `items_image` and `items_image_old`. The old width stays 512 until a golden run clears 256.

The cutoff is a user setting of 2, 5, or 7 years, starting at 2, measured by file mtime. When an image, video, or audio item crosses it, cut the stored 768 to the leading 512, L2-normalize that slice, insert the short row, and delete the long row in the same step. One item has one row. A pin stays in the long collection. Growing a shortened vector back re-embeds from the file.

The content id is the SHA-256 of modality, source-content hash, and width. Width is part of the id, so the short row is a new id. Insert of an existing id is a no-op. Scalar payload on the row is modality, source path, file mtime, mime, width, pinned flag, and that content id.

## 4. Index

Every collection on all four runtime identifiers uses plain HNSW through `ZVecHnswIndexParam`. Metric is cosine. `M` is 16. `EfConstruction` is 200. Those two match `ZVecDefaults.Hnsw`. Query `EfSearch` is 64 on `ZVecHnswQueryParams`. The SDK's query default, `ZVecDefaults.Query.HnswEfSearch`, is 300, so the app sets 64 itself. Changing 64 requires a golden-query recall check.

`ZVecHnswIndexParam.QuantizeType` defaults to `ZVecQuantizeType.Undefined` (`ZVecDefaults.Hnsw.QuantizeType`). Leave it there until a benchmark shows query time about 2× or 3× faster. The enum is `Undefined` = 0, `Fp16` = 1, `Int8` = 2, `Int4` = 3, `Rabitq` = 4. `Rabitq` = 4 is not a working C API setting.

When quantize is on, `src/Native/ZVec.Native/external/zvec/src/db/index/segment/segment.cc` creates a float index and a second quantized index. In the memory-indexer setup, `QuantizeType::UNDEFINED` builds one float indexer. Any other quantize type first creates that normal float indexer, then a second quantized indexer kept beside it. The persisted path does the same split: a float `VECTOR_INDEX` block and a `VECTOR_INDEX_QUANTIZE` block. Turning quantization on adds the second index. It does not replace the float index and it does not shrink the file. `Int8`, `Int4`, or `Fp16` wait for that query-time benchmark.

v1 does not create HNSW-RaBitQ. Constructing `ZVecHnswRabitqIndexParam` throws `NotSupportedException` in `NativeIndexParamBuilder` before `zvec_index_params_create`. On Arm and Arm64 the platform check throws `PlatformNotSupportedException` even earlier. DiskANN is out of v1 on every runtime identifier.

## 5. Query

Embed the question once: text prefix, one pooled 768, L2-normalize the full vector. For each live collection, take the leading width and L2-normalize that slice. A 768 collection uses the full vector. Do not re-embed the corpus to change a width, and do not re-embed the question per collection.

Search is hybrid: dense HNSW, BM25 full-text, and a scalar filter, fused with the in-database RRF reranker. Use `ZVecRrfReranker` with `RankConstant` 60 (`ZVecDefaults.Rerank.RankConstant`). Search each live collection. Merge the long and old collections with that same RRF. Weighted RRF is a later switch. Return the top 20 with score components and provenance: path, modality, width, and mtime.

## 6. Ingest

The person grants paths and, per path, file types. Search hits only those paths and types. A parent path wins in one tree check: if a granted parent contains a granted child, the parent is the scope and the UI warns. `D:\` together with `D:\Photos` keeps `D:\`.

The watcher runs while the process runs.

| RID | Watcher |
|---|---|
| `android-arm64` | Storage Access Framework document tree, or `FileObserver` only where that framework does not apply |
| `ios-arm64` | No background watch |
| `win-x64` | `FileSystemWatcher` |
| `maccatalyst-arm64` | `FSEventStream` |

On iOS the grant is a security-scoped folder. When the app comes to the foreground, walk that folder and diff it against SQLite `checkpoints.db`. That database holds ingestion cursors, job state, and the crash-recovery cursor. Cursor updates use parameterized SQL inside a transaction.

Created and modified events enqueue ingest. The person can press Index now, and the search page can offer to index an allowed path that is not indexed yet. There is no timer sweep. `IIdleDetectorService` pauses ingestion.

Rename updates the stored path. Re-embed only when the bytes change. Delete tombstones the item and keeps the vector, with the source marked missing. Recall stores the path and does not copy source media into an app archive.

A sibling subtitle is attached to the matching audio or video. It is not a second document. Known types are `.srt`, `.vtt`, `.sbv`, `.ass`, `.ssa`, and `.ttml`, including a language tag such as `talk.en.vtt` next to `talk.mp4` or `talk.m4a`. The words are searchable on that media item. v1 does not open the media container to pull a caption track.

## 7. OCR

A PDF with a text layer is extracted. A scanned page is OCR. An image is OCR only for the words in the picture, and the vision embedding still runs on that picture. A page that already has text is not sent through OCR.

v1 OCR is one shared detector plus two readers: official PP-OCRv6 small ONNX for English, and a separate Arabic ONNX reader of about 8 MB. Other languages are optional extra downloads the person turns on. There is no speech-to-text model. Spoken words enter search only through a sibling subtitle that is already next to the media.

## 8. Model download

`IModelDownloadService` is the only network path. The text download writes the int8 ONNX, its external data file, and `tokenizer.json` into app data. A `206` appends, a `200` replaces the partial, and a short body stays a `.partial` file. Each complete file is SHA-256 checked before it is moved. Retry starts the download again. The request names the public weight file only. Weights are not committed under `tests/Recall.IntegrationTests/TestAssets/embeddinggemma-2-text/`. Float16 and a 4-bit file are not this download. GGUF Q5 and Q6 names do not run on this runtime.

## 9. Device record

First launch stores the public platform id when that read is ready: Android `ANDROID_ID` (rejecting `9774d56d682e549c`), iOS and Mac Catalyst `identifierForVendor`, Windows `MachineGuid`. A random id is stored only when the candidate is empty, whitespace, or known-bad. The native OS name is stored and stays read-only. The person edits only the display name. The id is not a serial, IMEI, UDID, or advertising id. It stays on the device and is not uploaded, including on the model-download request. See [adr/0001-platform-device-id.md](adr/0001-platform-device-id.md).

## 10. Sync stub

The schema stays sync-ready and the transport stays absent. v1 appends an unsent `INSERT` in memory and as one line in a UTC-day file under `oplog` (`yyyy-MM-dd.log`). The log is never sent. The operations reserved for later are `DELETE`, `PIN`, and `UNPIN`. The sync feature flag defaults to off. `Recall.Core.Sync` has no transport. The UI does not collect a pairing code. v1 does not implement pairing, transport, or encryption. `ISecureStorageService` has no sync keys to hold.

## 11. Tests

A test counts when deleting the behavior makes the test fail. The line-coverage floor is 60% at M1 and 70% later. That floor is not the verdict. A feature under the floor fails, and a feature above the floor still fails when a critical path, a hot path, or an edge case has no test that can go red.

Critical cases:

- Width 128 is rejected for every stored vector.
- Text is stored at 512, and 256 waits for a golden run.
- The age cutoff accepts only 2, 5, or 7 years, and the start value is 2.
- A parent path wins over a child path in one tree check.
- A sibling subtitle is attached to the media item and is not indexed as its own document.
- `QuantizeType` stays `Undefined`. A build that sets `Int8`, `Int4`, `Fp16`, or `Rabitq`, or that constructs `ZVecHnswRabitqIndexParam`, fails this test.
- `Recall.Core` has no HTTP client. `HttpClient`, `Socket`, `TcpListener`, and `UdpClient` stay forbidden there, including Sync. The only `IHttpClientFactory` registration is the MAUI model-download adapter.
- Kill-9 during ingest resumes from `checkpoints.db`.

Pure Core logic, with fakes for ONNX and ZVec, lives in `tests/Recall.UnitTests`. Real ONNX, real ZVec, crash restart, and the egress check live in `tests/Recall.IntegrationTests`. Golden recall lives in `tests/Recall.CorpusTests`. Latency and memory live in `src/Recall.Benchmarks`. A unit test does not download a real model and does not read the developer's home folder.

## 12. Unmeasured

These three items stay open. The notes do not pick a number for them.

- U1, disk. There is no ship disk budget. The old 80 MB and 150 MB figures are not targets. `QuantizeType` stays off, so those figures are not a gate.
- U2, RSS. There is no ship memory budget. The old 400 MB warmup figure is not a target. Float16 inference is forbidden. The int8 text weight file is about 314 MB; that is a file size, not an RSS budget.
- U3, crash reports. Crash reports stay on the device. The product goal is zero telemetry. An opt-in reporter that phones home is unresolved, and v1 does not add one.
