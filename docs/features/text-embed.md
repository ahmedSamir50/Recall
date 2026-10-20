Accepted runtime: open. ADR 0012. The running class is LlamaTextEmbeddingEncoder.

`MatryoshkaVectorMath.CutAndNormalize` keeps the leading stored width and L2-normalizes that slice. Search and ingest each embed once, then cut to 512.

`LlamaTextEmbeddingEncoder.EmbedPrefixedTextAsync` throws on blank text. A cancelled token throws OperationCanceledException before the native call. recall_embed embed return 0 is 768 floats then L2-normalized. Any other code throws InvalidOperationException. The gate is SemaphoreSlim(1, 1).
