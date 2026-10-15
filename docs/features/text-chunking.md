Status: in-code

## Budget

`TextChunkPlanner.MaxTokenBudget` is 8192, including the prefix. `PlanChunks` throws on blank content or a null counter. When the whole titled document is within budget it returns one piece, ordinal 0. Past the budget it splits, each piece keeps the title prefix, ordinals increase from 0, and the split snaps to a rune boundary. Chunks are not averaged.

## Tokenizer

The counter is `Tokenizers.DotNet` 1.4.1 over `tokenizer.json`. Windows uses `win-x64`. Mac Catalyst uses `osx-arm64`. Android and iOS have no tokenizer runtime in that package, so `TextTokenizerRuntime.IsSupported` is false and those shells fail closed.
