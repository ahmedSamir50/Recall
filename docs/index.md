## What this repo is

Recall is one .NET 10 MAUI Blazor Hybrid process. The accepted text runtime is in-process llama.cpp, and ZVec.NET stays in that same process. The class in the tree is still the ONNX text encoder. v1 does not send user content or vectors off the device.

## How to read

Read this page, then [for-agents.md](for-agents.md), then the domain page or the use-case page for the question. A page marked `in-code` names types that exist. A page marked `decided-absent` is an accepted rule with no implementing class. A page marked `open` must not be implemented by guessing. A page marked `forbidden` must not be built.

## What exists now

In the tree now: shell, Arabic and English chrome, device record, folder grants, text model download, text embed at stored width 512, one `items_text` collection, hybrid Find, local file open, age cutoff stored as 2, 5, or 7, sync flag off, local op-log insert and delete lines, scope watchers, `checkpoints.db`, Code grant flag, skip-folder and secret filters.

Progress numbers live only in [plan.md](plan.md). Domains start at [domains/index.md](domains/index.md). Use cases start at [use-cases/index.md](use-cases/index.md). The file catalog is [code/map.md](code/map.md).

## What is not in the tree

Not in the tree: vision, video sampling, OCR, audio encoder, sibling transcripts, pin storage, vector aging, golden recall report, four-shell release.

```mermaid
flowchart TD
  Index --> ForAgents
  Index --> Domains
  Index --> UseCases
  Index --> InCode
  Index --> DecidedAbsent
  Index --> Open
  Index --> Forbidden
  Index --> Catalog
```
