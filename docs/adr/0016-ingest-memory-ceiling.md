# ADR 0016: Ingest memory ceiling

Status: accepted, 2 November 2026.

## Context

A text ingest pass on a table-heavy corpus froze at 34 of 41 files while the working set climbed from 10 GB to 41 GB. The planner looped: `OverlapStart` fell back to any older sentence start, so `startChar` moved backwards and the same patch was produced forever. Every iteration minted a new content id, so the store never recognised it, and the pass never reached `FinishPath`.

The store made it worse. Every call rebuilt a hash set of all `(content_id, path)` pairs, so one append allocated five times the size of the database at eleven thousand rows.

## Decision

1. **Forward progress is a contract.** `PlanCore` advances `startChar` strictly, clamps the overlap to the 256-384 token window, and stops at `MaxPatchesPerDocument = 512`. The planner returns `PatchPlan` so truncation is visible.
2. **The schema runs once.** `EnsureReady` replaces the per-call `EnsureSchema`, the alias backfill is one SQL statement behind `PRAGMA user_version`, and chunks are written in one transaction per 64.
3. **The ceiling is half of capacity, over a measured floor.** `FloorBytes = weights + scratch + reserve + framework base`. Scratch is derived from context geometry (about 425 MB of float16 K and V for 7680 tokens over 768 dimensions and 18 layers) plus headroom.
4. **The verdict is decided before the model opens.** A device whose floor is above half its capacity never loads the encoder and never claims to index.
5. **Pressure cools down; it does not throw.** A throw would land in `FailedPaths` and unwind without releasing anything. The gate returns state, resumes at 35%, and the scheduler re-triggers the pass.
6. **The text encoder is never unloaded.** Not for the cooldown, not for a budget number.

## Why W1 does not change chunk boundaries

The fix edits one assignment, `startChar = next`. It differs from the old code only when `next <= startChar` or `next >= cutChar`, which is exactly when the old planner produced a patch that started at or before the previous one, or beyond its own cut. Terminating documents keep their boundaries: measured identical on the 400-line table control (10 patches, same ranges) and on 60 sentences (2 patches, first overlap 263 characters, unchanged). A recorded-range test locks both.

## What this ADR does not decide

- Native thread count and `n_ubatch` are untuned. Changing batching order needs a checked-in fixture and a cosine baseline, which does not exist yet. The follow-up is on [open decisions](../decisions/open.md).
- Recall@10 after a boundary change is unmeasured. The boundary argument above stands in for it until a golden corpus run exists.
- Android reports the GC heap limit rather than device RAM, so the ceiling there is stricter than half the phone. That is the safe direction and is left as a platform read.

## Consequences

A small device loses search with a visible reason instead of degrading. A document over 512 patches is partially indexed with a note. The op-log keeps ten thousand entries in memory and the day file stays durable. A file refused by the byte guard is recorded and not read.

## Measured consequence (added 2 November 2026)

The floor was measured on the real encoder: 295.5 MB of weights, a 24.3 MB framework base, and 1,125.18 MiB of device compute buffers plus 285.23 MiB of host buffers, which gives a declared floor of 1.81 GB. Under a job-object memory cap that .NET reports as capacity, a 2 GB process reported 1.50 GB and a 4 GB process reported 3.00 GB; both were `BelowMinimum` and neither opened the encoder. An 8 GB process reported 6.00 GB and opened normally. The minimum is therefore 3.6 GB of reported capacity and the recommended size is 6.0 GB, both derived from the measured floor rather than assumed.

## The context guard (added 2 November 2026)

A patch at the hard cap used to overshoot it. `CutPiece` extended `reach` in 512-token steps and only checked the cap before each step, so a boundary-free document could produce a patch 512 tokens past `HardCapTokens`; measured, a 20,007 character document cut at 7,681 tokens against a 7,680-token context. The engine then adds the `title: … | text: ` prefix after tokenization, so even an exact cap has no room for the seam. `CutPiece` now clamps `reach` to `startToken + HardCapTokens - prefixTokenCount - ContextGuardTokens`, and a patch that used to be 7,681 tokens is 7,649. The same document also keeps its three patches and its ranges, so the guard costs nothing measurable.

## The token map cap (added 2 November 2026)

The guard alone was not enough. `recall_embed_token_ends` returns at most `RECALL_EMBED_MAX_TOKENS` entries, so a document longer than 7,680 tokens came back with a map that stopped there. `PlanCore` read the last entry as the end of the document, emitted one final patch holding every remaining character, and `recall_embed_embed` rejected it with code 4. Measured on a 308,006 character file: 4 patches, the largest 56,899 tokens, embed failed. `LlamaTextTokenCounter.TokenEnds` now walks the text in 65,536 character windows and offsets each result, so the map always reaches the end: the same file returns 63,006 ends, 31 patches, and a largest patch of 2,049 tokens that embeds. This is the defect that would have kept every file over roughly thirty kilobytes from finishing, and the original plan did not name it.
