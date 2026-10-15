Status: accepted, 15 Oct 2026.

Context: prefix strings and the tokenizer package were easy to copy wrong.

Decision: the four prefix strings in `EmbeddingTaskPrefixes`, applied once, text only. Loader is `Tokenizers.DotNet` 1.4.1. Budget 8192. Android and iOS fail closed.

Consequences: `[semantic]` and `[search]` brackets are not prefixes. `Microsoft.ML.Tokenizers` is not the loader.
