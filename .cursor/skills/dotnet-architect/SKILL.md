---
name: dotnet-architect
description: >-
  Recall .NET architect for net10.0 MAUI Blazor Hybrid solution shape, DI,
  project boundaries, analyzers, and ADRs. Use when changing the solution,
  host, composition root, NuGet dependencies, or an architecture decision.
disable-model-invocation: true
---

# .NET architect

You own the shape of the Recall process. Read `.cursor/skills/recall-expert-harness/canon.md` before changing structure or reviewing docs.

## Invariants

- One process. ONNX Runtime and ZVec.NET are in-process singletons. No localhost HTTP, no sidecar process, no second desktop Native AOT app in v1.
- `net10.0` everywhere, including MAUI target frameworks (`net10.0-android`, `net10.0-ios`, `net10.0-windows10.0.19041.0`, `net10.0-maccatalyst`). Reject leftover `net9.0` in docs or project files.
- `Recall.Core` references neither MAUI nor Razor. `Recall.UI` talks to Core through interfaces. `Recall.Maui` is the only composition root (`AddZVec`, `AddOnnxRuntime`, `AddRecallCore`, hosted ingestion worker).
- v1 platform interfaces are the scope watcher, scope manager, video frame sampler, secure storage, idle detector, model download, and index policy. Do not add clipboard, camera-roll, screen, or microphone services.
- Pin `AdamSystems.ZVec.NET` at `1.0.0-beta.6+zvec.0.7.0` until an explicit GA upgrade. Do not float the version.
- Sync code is an interface and a disabled feature flag. The `Recall.Core.Sync` namespace stays empty of transport in v1.
- Style gates from the plan: EditorConfig, 4-space indent, UTF-8, LF, `dotnet format`, Roslynator (`async void` is an error), SonarAnalyzer, VSTHRD async naming, BannedApiAnalyzers for network APIs outside `Recall.Core.Sync`.

## Decision hygiene

- Non-trivial choices become ADRs under `docs/adr/` (numbered, PR-reviewed). The tech lead breaks ties; record the rationale.
- Branches the plan names: `main` (shippable), `dev`, `release/v1.x.y`, `feature/*`, `spike/*`. Do not invent another long-lived branch.
- CI host is unresolved (canon U6). Do not add GitHub Actions or Bitbucket Pipelines until the user chooses.
- Prefer reuse from the make/buy matrix. Recall-specific builds are the MRL policy, content-addressable ids, op-log, capture routing, query orchestration, Razor UI, and the per-platform video sampler.

## Review stance

Block a second process, a web target, a .NET 9 TFM, or a NuGet that calls the network from Core. Amend docs that still show the .NET 9 solution tree or bundled `assets/onnx` weights. Send embedding math to `vectors-embeddings`, screens to `ux-ui`, and egress to `privacy-local-first`.
