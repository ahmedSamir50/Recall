Status: implemented, 2 November 2026. See [ADR 0016](../adr/0016-ingest-memory-ceiling.md) and [Ingest memory budget](ingest-memory-budget.md).

A per-document stop and a per-file read guard are in the tree. The numbers below are the ones that shipped.

## Problem

One granted file could keep producing patches, frames, or windows until ingest finished the whole file. A desktop indexing pass showed working set **10,275 MB** then **41,450 MB** while the text meter still read **34/41** finished files.

The cause was not a per-file formula. `DocumentPatchPlanner.PlanCore` had no forward-progress guarantee: `OverlapStart` fell back to a sentence start older than the overlap window, `startChar` moved backwards, and the same patch was produced forever with a new content id each time. A 5,007 character document allocated 1,057 MB inside the planner in five seconds, before any database or encoder work.

## What the tree does now

`DocumentPatchPlanner` cuts one patch at target **2048** tokens and hard-caps one patch at **7680** tokens, less a **32**-token context guard so the title prefix and the tokenizer seam cannot push a patch past the 7680-token encoder context. It advances `startChar` strictly, never returns a sentence start older than the 256-384 token overlap window, and stops at **512** patches per document. When it stops there, the plan reports `Truncated`, the file finishes normally, and the tail is not processed.

| Guard | Value | Where |
|---|---|---|
| Patches per document | 512 | `DocumentPatchPlanner.MaxPatchesPerDocument` |
| Text file read | 33,554,432 bytes | `LocalContentPolicy.MaxTextFileBytes` |
| Whole-file media read | headroom under the 50% ceiling | `IMemoryBudget.MayLoadFile` |
| Video frames per file | 600 | `FrameCadence.MaxFramesPerFile` |

Stored vectors are unchanged:

| Kind | Width | Bytes |
|---|---|---|
| Text (Matryoshka stored) | 512 floats | 2,048 |
| Image, video frame, audio window | 768 floats | 3,072 |

## What is still open

The 512 cap is one constant and was not derived from a corpus measurement. A later run may tune it. A file that hits the cap is searchable only in its first part, and the note says so.
