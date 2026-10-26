Status: in-code for the mmproj download, `items_image`, `items_video`, and the two-second cadence. OCR preprocess is in-code.

## Decision

The vision file is mmproj-embeddinggemma-2-Q8_0.gguf through in-process llama.cpp, loaded by `mtmd_init_from_file` after the text model is open. The download is hash-pinned and separate from the text GGUF. Vision unloads in `finally`; it is never kept resident with the text encoder or an audio encoder. PP-OCRv6 stays ONNX. llama.cpp does not run that detector.

## Pictures

Pictures are embedded at 768 and stored in `items_image`. Rows past the age cutoff would move to `items_image_old` at 512 when the recall gate passes; production keeps 768 until `AgeCutCorpus` loads a passing set. The row has `embedding`, `words`, `path_live`, `encoder_hash`, `grant_path`, and empty `start_seconds` and `end_seconds`. A picture vector is never written into `items_text`. Find can take a JPEG inside a grant: `PictureQueryGate` reads the vision download phase, `PictureSearchService` embeds once, and dense search stays in `items_image` and `items_video`. A text query does not embed vision. The control is `Search with a picture` / `ابحث بصورة`; waiting is `The picture model is still downloading.` / `نموذج الصور ما زال يُنزَّل.`; failure is `The picture model did not download.` / `لم يكتمل تنزيل نموذج الصور.`

## Video frames

Android, iOS, and Mac Catalyst emit one JPEG every two seconds with `FrameCadence.Starts` and `FrameCadence.End`. On Windows build 22000 or newer, `WindowsVideoFrameSampler` uses `MediaComposition.GetThumbnailsAsync` at a two-second interval. On an older build it throws `NotSupportedException` with `Windows frame thumbnails need Windows 11.` Frame rows go to `items_video` at 768 with that start and end. Frames of one file collapse to one card with `Collapse.EarliestStart`.

## OCR

Status: in-code

Task 6.3.1 names `WindowsPdfPageRenderer`, `AndroidPdfPageRenderer`, and `ApplePdfPageRenderer` for PDF page PNG at 300 dpi. A physical Apple run is not required for those renderers.

OCR preprocess is in-code. Three ONNX sessions, the pinned PP-OCRv6 small detector, the pinned PP-OCRv6 small English reader, and the pinned Arabic PP-OCRv5 mobile reader, load together for one ingest pass and dispose in `finally`. The detector leaves a page alone when its short side is already at least 64 pixels, and it will not send a side longer than 4000. The reader keeps each line's aspect ratio at height 48, pads on the right with normalized gray, and uses a width from 320 through 3200. CTC blank stays class 0. The class after the yaml dictionary is a space. Arabic reader text is put into logical order with the PaddleOCR Arabic group rule, and the page router joins lines with a space. U5 stays closed. The five fixture pages are accepted. Their word-error ceilings, in edits over reference tokens, are English invoice 35/82, Arabic invoice 43/89, mixed invoice 73/116, English book 74/162, and Arabic book 29/153, with the Latin and Arabic splits in `OcrFixtureReference`. A later change that goes above a ceiling fails that page's test. These pages stay on the pinned readers. `OcrDetectorResize`, `OcrReaderResize`, `OcrBilinear`, `ArabicVisualOrder`, the CTC space class, and the space between routed lines stay as they are. Task 6.3.1 stays not-started. Task 6.3.3 stays not-started.
