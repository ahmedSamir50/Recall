Status: accepted, 15 Oct 2026.

Context: the host and libraries were locking the solution shape.

Decision: `net10.0`, MAUI Blazor Hybrid, Tailwind, four RIDs `android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64`, `Microsoft.ML.OnnxRuntime`, NuGet `ZVec.NET` `1.0.0-beta.6`, embedder asset `onnx-community/embeddinggemma-2-ONNX`.

Consequences: `Microsoft.ML.OnnxRuntimeGenAI` is not used.
