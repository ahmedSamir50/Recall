Status: decided-absent

## Decision

The vision file is mmproj-embeddinggemma-2-Q8_0.gguf through in-process llama.cpp. PP-OCRv6 stays ONNX. llama.cpp does not run that detector. Image and video collections, one JPEG every two seconds, PDF text-layer extract, OCR of a scanned page, OCR of words in an image, PP-OCRv6 small English, and a separate Arabic reader are plan phase 4. None of those types are in `src/`. Catalog ids `embeddinggemma-2-vision` and `embeddinggemma-2-audio` are names only. U5 is closed as this decision. The vision encoder is not in src/.
