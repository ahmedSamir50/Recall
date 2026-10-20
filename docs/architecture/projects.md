Status: in-code

## Projects

- `src/Recall.Core` headless.
- `src/Recall.UI` Razor, depends on Core ports.
- `src/Recall.Maui` composition root.
- `src/Recall.Adapters` in-process llama.cpp and ZVec.
- `src/Recall.Benchmarks`.
- `tests/Recall.UnitTests`.
- `tests/Recall.IntegrationTests`.
- `tests/Recall.BenchTests`.
- `tests/Recall.CorpusTests`.
- `tools/corpus-builder`.
- `tools/mrl-eval`.

TFM `net10.0`. Buildable RIDs `android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64`. v1 RID `linux-x64` has no target framework and no shell. NuGet id `ZVec.NET` version `1.0.0-beta.6`.
