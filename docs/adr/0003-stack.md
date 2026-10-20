Status: accepted, 15 Oct 2026.

Context: the host and libraries were locking the solution shape.

Decision: net10.0, MAUI Blazor Hybrid, Tailwind, four RIDs android-arm64, ios-arm64, win-x64, maccatalyst-arm64, NuGet ZVec.NET 1.0.0-beta.6. The accepted text runtime is in-process llama.cpp (ADR 0012). The class in the tree is still OnnxTextEmbeddingEncoder.

Consequences: LLamaSharp 0.30.0 is not linked. Microsoft.ML.OnnxRuntimeGenAI is not the text embedder.
