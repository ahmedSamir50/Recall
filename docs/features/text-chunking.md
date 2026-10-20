# Text chunking

The counter is LlamaTextTokenCounter. It calls recall_embed_count_tokens on the loaded GGUF. Blank text throws ArgumentException.

The 8192 budget includes the prefix. One vector when the note fits. Split only past that. Do not average chunks. Later chunks keep the title prefix and do not split a surrogate pair.

`TextChunkPlanner.PlanChunks` takes a delegate that returns token counts. Unit tests use a fake counter.
