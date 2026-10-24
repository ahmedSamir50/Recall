Status: in-code for the mmproj download, `items_image`, `items_video`, and the two-second cadence. OCR is decided-absent.

## Decision

The vision file is mmproj-embeddinggemma-2-Q8_0.gguf through in-process llama.cpp, loaded by `mtmd_init_from_file` after the text model is open. The download is hash-pinned and separate from the text GGUF. Vision unloads in `finally`; it is never kept resident with the text encoder or an audio encoder. PP-OCRv6 stays ONNX. llama.cpp does not run that detector.

## Pictures

Pictures are embedded at 768 and stored in `items_image`. Rows past the age cutoff would move to `items_image_old` at 512 when the recall gate passes; production keeps 768 until `AgeCutCorpus` loads a passing set. The row has `embedding`, `words`, `path_live`, `encoder_hash`, `grant_path`, and empty `start_seconds` and `end_seconds`. A picture vector is never written into `items_text`. Find can take a JPEG inside a grant: `PictureQueryGate` reads the vision download phase, `PictureSearchService` embeds once, and dense search stays in `items_image` and `items_video`. A text query does not embed vision. The control is `Search with a picture` / `ابحث بصورة`; waiting is `The picture model is still downloading.` / `نموذج الصور ما زال يُنزَّل.`; failure is `The picture model did not download.` / `لم يكتمل تنزيل نموذج الصور.`

## Video frames

Android, iOS, and Mac Catalyst emit one JPEG every two seconds with `FrameCadence.Starts` and `FrameCadence.End`. On Windows build 22000 or newer, `WindowsVideoFrameSampler` uses `MediaComposition.GetThumbnailsAsync` at a two-second interval. On an older build it throws `NotSupportedException` with `Windows frame thumbnails need Windows 11.` Frame rows go to `items_video` at 768 with that start and end. Frames of one file collapse to one card with `Collapse.EarliestStart`.

## OCR

Status: decided-absent

OCR stays decided-absent: no ONNX reader is constructed and no scanned page is read. The three model files are pinned in `OcrModelFiles`: PP-OCRv6 small detector, PP-OCRv6 small English reader, and the Arabic PP-OCRv5 mobile reader. A missing hash does not load a reader. `OcrLineChoice` keeps the higher reader score and keeps English on a tie within `1e-6`. `OcrSessionSet` is three booleans for Epic 6.3. Pages `BrokenArabic.NeedsOcr` flags are stored in `ocr_page` and ingest pass 3 runs `OcrPassStub` once per pass; that queue does not block audio. Real OCR is Epic 6.3 task 6.3.1 (`Microsoft.ML.OnnxRuntime` 1.31.0 and three `InferenceSession`s). The audio catalog id `embeddinggemma-2-audio` uses the same mmproj file as vision. U5 stays closed.
