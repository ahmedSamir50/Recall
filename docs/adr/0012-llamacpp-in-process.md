Status: accepted, 20 Oct 2026.

Context: the text embedder was ONNX Runtime. A helper process was considered for llama.cpp.

Decision: the text embedder is llama.cpp in the MAUI process, on android-arm64, ios-arm64, win-x64, and maccatalyst-arm64. The build must contain commit 4fbc76dec51d0add466f0210855c0596589b60d4. The file is embeddinggemma-2-Q8_0.gguf from ggml-org/embeddinggemma-2-GGUF. Pooling is mean. The vector is 768 wide. There is no helper process and no listen port. LLamaSharp 0.30.0 is not that build. The in-code encoder is LlamaTextEmbeddingEncoder through native/recall_embed.c and llama.cpp tag b11455.

Consequences: keep_desktop_tokenizer and trusted_tokenizer_loader are not executed. PP-OCRv6 stays ONNX. Chat UI is absent. Weights stay downloaded, not bundled.
