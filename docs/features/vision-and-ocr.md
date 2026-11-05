Status: in-code for the mmproj download, `items_image`, `items_video`, and the two-second cadence. OCR preprocess is in-code.

## Decision

The vision file is mmproj-embeddinggemma-2-Q8_0.gguf through in-process llama.cpp, loaded by `mtmd_init_from_file` after the text model is open. The download is hash-pinned and separate from the text GGUF. Vision unloads in `finally`; it is never kept resident with the text encoder or an audio encoder. PP-OCRv6 stays ONNX. llama.cpp does not run that detector.

Photos, voice, and the helper row on Models each have their own download bar and snapshot. The helper row does not name OCR files on screen. OCR progress is stored on `GetOcrDownloadSnapshot` and does not write the text snapshot. A verified text model triggers window-open ingest for every grant; the helper download does not run OCR on pictures by itself.

## Pictures

Pictures are embedded at 768 and stored in `items_image`. Rows past the age cutoff would move to `items_image_old` at 512 when the recall gate passes; production keeps 768 until `AgeCutCorpus` loads a passing set. The row has `embedding`, `words`, `path_live`, `encoder_hash`, `grant_path`, and empty `start_seconds` and `end_seconds`. A picture vector is never written into `items_text`. Find can take a JPEG inside a grant: `PictureQueryGate` reads the vision download phase, `PictureSearchService` embeds once, and dense search stays in `items_image` and `items_video`. A text query does not embed vision. The control is `Search with a picture` / `ابحث بصورة`; waiting is `The picture model is still downloading.` / `نموذج الصور ما زال يُنزَّل.`; failure is `The picture model did not download.` / `لم يكتمل تنزيل نموذج الصور.`

## Video frames

Android, iOS, and Mac Catalyst emit one JPEG every two seconds with `FrameCadence.Starts` and `FrameCadence.End`. On Windows build 22000 or newer, `WindowsVideoFrameSampler` uses `MediaComposition.GetThumbnailsAsync` at a two-second interval. On an older build it throws `NotSupportedException` with `Windows frame thumbnails need Windows 10 or newer (build 22000 or later).` Frame rows go to `items_video` at 768 with that start and end. Frames of one file collapse to one card with `Collapse.EarliestStart`.

## OCR

Status: in-code; the 80-page real-scan gate does not pass yet.

Task 6.3.1 names `WindowsPdfPageRenderer`, `AndroidPdfPageRenderer`, and `ApplePdfPageRenderer` for PDF page PNG at 300 dpi. A physical Apple run is not required for those renderers.

The per-page path is: one `WindowsPdfDocument` open per document, one 300 DPI render per page, one detector run, one reader run per line, and results joined by page number. Pages run on `Parallel.ForEachAsync` with `OcrPageWorkers = 2` (`RECALL_OCR_PAGE_WORKERS`), reduced to `1` when `IMemoryBudget.Pressure == Hard`.

The detector leaves a page alone when its short side is already at least 64 pixels, and it will not send a side longer than 4000. Capping the long side at 1280 was measured and reverted: it kept four of five WER fixtures green and broke the Arabic invoice at 50.0% Arabic WER (14/28) against an accepted 12/28. `RECALL_OCR_DETECTOR_SIDE` is not shipped. The reader keeps each line's aspect ratio at height 48, pads on the right with normalized gray, and uses a width from 320 through 3200.

Line routing reads each line once. `ScriptOf` counts Arabic letters against Latin letters; an Arabic majority goes to the Arabic reader, otherwise English. The other reader is used only below `LowConfidence = 0.6`, and the higher confidence wins. CTC blank stays class 0. The class after the yaml dictionary is a space. Arabic reader text is put into logical order with the PaddleOCR Arabic group rule, and the page router joins lines with a space.

The three ONNX sessions live in `OcrSessionSet`, load together, survive a pass, and unload after two idle minutes or on `Dispose`. The provider ladder is CPU by default; `RECALL_OCR_EXECUTION_PROVIDER` pins `auto | cpu | cuda | directml | nnapi | coreml`. On this machine the shipped `Microsoft.ML.OnnxRuntime` 1.31 native DLL has no DirectML entry point (`EntryPointNotFoundException`), so Windows falls back to CPU. A Windows ML 2.1.70 probe binds DirectML: adapter 1 (NVIDIA RTX A4500) ran the 960x960 detector in 18.1 ms mean against 161.4 ms on CPU, while adapter 0 (Intel UHD) took 209.9 ms. Adapter choice, not the provider name, decides the speed. See [ADR 0017](../adr/0017-ocr-throughput.md).

Search preempts OCR: `IIngestPreemption` cancels the pass token while a search is in flight, the runner re-enqueues the cancelled slice, and the pass loop uses slices of 64 text, 32 media, and 64 OCR jobs inside a 120 s budget instead of draining one queue.

U5 stays closed. The five fixture pages are accepted. Their word-error ceilings, in edits over reference tokens, are English invoice 35/82, Arabic invoice 43/89, mixed invoice 73/116, English book 74/162, and Arabic book 29/153, with the Latin and Arabic splits in `OcrFixtureReference`. A later change that goes above a ceiling fails that page's test. These pages stay on the pinned readers. `OcrDetectorResize`, `OcrReaderResize`, `OcrBilinear`, `ArabicVisualOrder`, the CTC space class, and the space between routed lines stay as they are.

The golden corpus is local, not committed: `tools/fetch-ocr-golden.ps1` fetches 80 validated Wikisource scans (30 Arabic, 30 English, 7 French, 7 German, 6 Spanish) plus the four demo phone captures into `artifacts/ocr-golden/` with `manifest.csv` (`path,language,isPhone,mixed,sourceUrl,sha256`). `OcrGoldenCatalog` reads the manifest, verifies every sha256, and throws with the fetch command named when the corpus is missing. No OCR proof skips.

Measured 4 November 2026 on the demo English page at 300 DPI: render 272 ms, detector 1,472 ms, reader 588 ms, infer 2,105 ms, 153 characters. The three-PDF proof runs in 16.2 s against a 2-minute ceiling. The 80-page gate exceeded its 5-minute ceiling at 48 of 84 pages (297.7 s), so task 6.3.3 stays `partial`.

