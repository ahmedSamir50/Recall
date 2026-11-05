# ADR 0017: OCR throughput and residency

Status: accepted, 4 November 2026.

## Context

The OCR path was CPU-only ONNX with a four-thread cap, a PDF reopened for every page, a dual read of the first lines, one page at a time, three sessions opened and closed per pass, and no arbitration with search. The measured split on the demo English page at 300 DPI was render 272 ms, detector 1,472 ms, reader 588 ms, infer 2,105 ms, and 153 characters of output. Detector time dominated because `OcrDetectorResize.MaxSide = 4000` fed the detector an 8.7 MP page while the model was benchmarked at about 0.92 MP.

## Decision

1. **Provider ladder, CPU always works.** `OcrProviderPolicy` keeps a GPU only when availability, a prerequisite probe, a self-test within `SelfTestTolerance = 1e-3` against CPU, and a 20-run burst agree, and when `gpuMs <= cpuMs / SpeedupThreshold` with `SpeedupThreshold = 1.3`. `RECALL_OCR_EXECUTION_PROVIDER = auto | cpu | cuda | directml | nnapi | coreml` pins it; tests run pinned to CPU. A DirectML session that fails to create retries the same session on CPU and records `Provider = Cpu`.
2. **Threads follow the provider.** GPU providers use `IntraOpNumThreads = 1` and `InterOpNumThreads = 1`; CPU uses `Math.Clamp(cores - 1, 1, 8)`. `RECALL_OCR_INTRA_OP_THREADS` still wins.
3. **The document is opened once.** `WindowsPdfDocument.Open` holds one `PdfDocument`; `Render(pageNumber)` renders each page from that handle at 300 DPI and the handle is disposed in `finally`.
4. **One read per line.** `ScriptOf(box)` counts Arabic letters against Latin letters; an Arabic majority goes to the Arabic reader, otherwise English. The other reader is used only when the chosen reader's confidence is below `LowConfidence = 0.6`, and the higher confidence wins. `ArabicLetterShare = 0.5`. Reading order is unchanged.
5. **Bounded parallel pages.** `Parallel.ForEachAsync` with `OcrPageWorkers = 2` (`RECALL_OCR_PAGE_WORKERS`), results joined by page number so reading order is stable, and the worker count forced to `1` when `IMemoryBudget.Pressure == Hard`.
6. **Sessions survive a pass.** `OcrSessionSet` holds the three `InferenceSession`s, loads them together, and disposes them on `Dispose` or after `OcrIdleUnload = 2 minutes` with no OCR job.
7. **Search preempts OCR, and slices are fair.** `IIngestPreemption` cancels the pass token while a search is in flight; the runner re-enqueues the cancelled slice. `RunReadyPassesAsync` loops over `TextSlice = 64`, `MediaSlice = 32`, `OcrSlice = 64` with `PassBudget = 120 s` instead of draining one queue at a time, so a text backlog cannot starve OCR and a spent budget leaves queued jobs for the next trigger.
8. **The token map is uncapped natively.** `recall_embed_token_ends` uses `cap = n_tokens` instead of `min(n_tokens, RECALL_EMBED_MAX_TOKENS)`. The managed 65,536-character window loop stays as the fallback for a capped library.

## Measured, 4 November 2026

### Detector input cap was tried and reverted

Capping the long side at `DetectorMaxSide = 1280` (with `RECALL_OCR_DETECTOR_SIDE`) kept four of the five WER fixtures green and broke the Arabic invoice: Arabic WER 50.0% (14/28 edits) against an accepted 12/28. The change was reverted; `OcrDetectorResize.MaxSide = 4000` stands.

### Windows ML probe, outside the repository

`Microsoft.Windows.AI.MachineLearning` 2.1.70 (ONNX Runtime 1.24.20260504.1 in the package) binds the detector `ppocrv6-small-det.onnx` and runs a `[1,3,960,960]` tensor. The DirectML entry point exists there, unlike the `Microsoft.ML.OnnxRuntime` 1.31 native DLL in the app, which throws `EntryPointNotFoundException: Unable to find an entry point named 'OrtSessionOptionsAppendExecutionProvider_DML'`.

| Provider | Adapter | 5 runs, ms | Note |
|---|---|---|---|
| CPU (WinML ORT 1.24) | - | 150.0 / 161.4 / 184.3 min/mean/max | baseline |
| DirectML | 0, Intel UHD | 205.7 / 209.9 / 219.9 | slower than CPU, `nvidia-smi` 0% |
| DirectML | 1, NVIDIA RTX A4500 | 16.8 / 18.1 / 21.5 | `nvidia-smi` 14%, +418 MB GPU memory |

The adapter index decides whether the GPU is used at all. `AppendExecutionProvider_DML(0)` selects the integrated adapter on this machine, so a future Windows ML switch must choose the adapter rather than assume index 0.

### Native token map

One call to the real library now returns the whole map for a 204,838-byte document: 39,285 ends, last end 204,838, versus the 7,680-entry cap.

### Proofs

| Proof | Ceiling | Result |
|---|---|---|
| `Ocr_ThreeScannedPdfs_SearchHitsGroundTruthWord` | 2 minutes | passed, 16.2 s |
| `OcrGolden_EightyPages_CerWithinCeiling` | 5 minutes | exceeded: 48 of 84 pages in 297.7 s |

## Consequences

OCR is bounded by inference rather than by document reloads, a second reader, or a four-thread ceiling. A text backlog and a search both get to run: the text pass yields between chunks and the OCR pass yields its slice, which is re-enqueued rather than lost. The 80-page real-scan gate does not pass yet: the corpus is now real Wikisource scans and the pass is slower than five minutes, so task 6.3.3 stays `partial`.

## What this ADR does not decide

- NNAPI and CoreML providers stay out of v1; Android and Apple run CPU.
- The 300 DPI render is a quality decision, not only a cost, and is unchanged.
- OCR text is not re-embedded into `items_text`.
- No benchmark project for OCR.
