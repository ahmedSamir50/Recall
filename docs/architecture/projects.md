Status: in-code

## How the projects relate

The explanation of why these projects exist, and what each one is allowed to know, is [How Recall is put together](design.md). The job of each type is [What each piece is for](pieces.md).

`Recall.Core` holds the rules and the ports. It does not reference MAUI, Razor, HTTP, llama.cpp, or ZVec. `Recall.UI` holds the screens and depends only on Core. `Recall.Adapters` holds the in-process llama.cpp embedder and the ZVec indexes, and depends only on Core. `Recall.Maui` is the executable. It references the other three, implements the operating-system ports, and is the composition root.

`Recall.Benchmarks` measures. The four test projects check rules, real libraries, benchmark honesty, and the corpus. `tools/corpus-builder` and `tools/mrl-eval` are offline tools. None of those ship inside the app.

The shared target framework is `net10.0`. The app targets `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, and `net10.0-windows10.0.19041.0`. Buildable runtime identifiers on those heads are `android-arm64`, `ios-arm64`, `win-x64`, and `maccatalyst-arm64`. `linux-x64` is a v1 runtime identifier and has no target framework and no shell in this solution. The vector package is NuGet id `ZVec.NET` version `1.0.0-beta.6`.
