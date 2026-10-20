Status: accepted, 15 Oct 2026.

Context: prefix strings and the tokenizer package were easy to copy wrong.

Decision: the four prefix strings in EmbeddingTaskPrefixes, applied once, text only. Budget 8192. The accepted tokenizer is the one inside embeddinggemma-2-Q8_0.gguf, called by in-process llama.cpp through recall_embed_count_tokens. GemmaTextTokenizer and Tokenizers.DotNet are not the loader. Android and iOS do not fail closed.

Consequences: semantic and search brackets are not prefixes. Microsoft.ML.Tokenizers is not the loader. The two tokenizer plans are not executed.
