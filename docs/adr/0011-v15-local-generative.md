Status: accepted, 15 Oct 2026.

Context: ADR 0002 bans a language model. The user reopened that ban for v1.5 on 15 Oct 2026.

Decision: v1 still has no language model. v1.5 may download one opt-in local generative ONNX model through the model-download path. The source weights to convert are Qwen/Qwen3-0.6B after that pinned commit’s license file says Apache-2.0. The graph is the one we generate for ONNX Runtime GenAI. iPhone stays off while GenAI lists iOS as under development. LiteGraph is not a dependency.

Consequences: search still works while notes are off. Group labels stay deterministic. The v1 text embedder is ADR 0012 and is not this generative file.
