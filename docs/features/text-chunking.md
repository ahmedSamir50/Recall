Status: in-code

## Budget

`TextChunkPlanner.MaxTokenBudget` is 8192, including the prefix. `PlanChunks` throws on blank content or a null counter. When the whole titled document is within budget it returns one piece, ordinal 0. Past the budget it splits, each piece keeps the title prefix, ordinals increase from 0, and the split snaps to a rune boundary. Chunks are not averaged.

A heading, a table, and a fenced block are not read as structure yet. That rule is open in structure-chunks.md.

## Tokenizer

The in-code counter calls GemmaTextTokenizer on tokenizer.json. IsSupported is true for Windows, Mac Catalyst, Android, iOS, and Unknown. The accepted tokenizer is the GGUF tokenizer inside in-process llama.cpp. Tokenizers.DotNet is not the loader. The emulator does not load Tokenizers.DotNet.runtime.linux-x64.
