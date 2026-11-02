# Plan: ingest memory hardening — no path above half the machine

Status: implemented 2 November 2026, wave by wave. Open items are listed at the bottom of this
file. This file is the plan; it is not the implementation. Do not code from it until the user
accepts a change to it.

Date: 2 November 2026 (revision 2, after the floor/cooldown correction). Owner turn: the one
that wrote this file. Next turn implements, or the user rejects a line and this file is
corrected first.

## 1. What was measured

All numbers below were produced on this machine with a scratch console project that references
`Recall.Core` (Release, net10.0). Nothing in this repo was modified to measure them.

### 1.1 The planner loop, alone

`DocumentPatchPlanner.Plan("Notes", "Intro. " + new string('X', 5000), null, 0, identityEnds)`
where `identityEnds = Enumerable.Range(0, length + 1)`:

| Probe | Input | Completed | Patches | Elapsed | Allocated inside `Plan` |
|---|---|---|---|---|---|
| A: one sentence, then 5000 chars with no boundary | 5,007 chars | **no** | — | 5,012 ms (killed) | **1,057 MB** |
| B: heading, then 400 table lines | 19,103 chars | yes | 10 | 2 ms | 0.6 MB |
| C: same shape as A, under one patch target | 207 chars | yes | 1 | 0 ms | 0.0 MB |

Probe A grows a `List<PdfPatch>` at roughly **210 MB/s with no database, no embedder, and no
vector index involved**. Probe B and C are the same code on ordinary text and they terminate.

### 1.2 The cursor store, one append, as the table grows

`SqliteIngestCursorStore.AppendLiveChunk` on a fresh database:

| Rows before the call | Time | Allocated |
|---|---|---|
| 200 | 2.53 ms | 0.11 MB |
| 1,000 | 3.37 ms | 0.48 MB |
| 3,000 | 5.07 ms | 1.40 MB |
| 7,000 | 11.04 ms | 3.16 MB |
| 11,000 | 11.90 ms | **5.27 MB** |

The whole 11,000-row database is 1 MB on disk. One insert allocates **5× the size of the
database** because `EnsureSchema` rebuilds a `HashSet` of every `(content_id, path)` pair on
every call (`SqliteIngestCursorStore.cs:1572` → `CopyChunkPairsToChunkPath` `:1634`).

### 1.3 What is **not** measured yet

The resident cost of the text encoder. The architecture in §3 depends on one number that no
file in this repo states: the post-load working-set delta of `embeddinggemma-2-Q8_0.gguf` plus
its 7680-token context. The weights file length is read at runtime, so half the floor is exact;
the compute scratch is a provisional constant until measurement **M1** in §6 fills it. No byte
figure for the gguf is invented here.

### 1.4 The fix, executed against a copy of the real planner

I copied `DocumentPatchPlanner.cs` out of the repo into the scratch project, applied the four
W1 edits, and ran it. Nothing in `src/` was touched. Identity token ends, `prefixTokenCount = 0`:

| Probe | Input | Result | Overlap between adjacent patches |
|---|---|---|---|
| A | `"Intro. "` + 5,000 `X` | **1 patch** `[0,5007)`, 4 ms, not truncated | — |
| A2 | `"Intro. "` + 20,000 `X` | 3 patches `[0,7681) [7681,15362) [15362,20007)` | `0, 0` |
| B | heading + 400 table lines | 10 patches, ranges `[0,2025) [2025,4046) …` — identical to today | `0, 0, 0, 0, 0` |
| C | `"Intro. "` + 200 `X` | 1 patch `[0,207)` | — |
| D | `"Hello world."`, empty token ends | 1 patch `[0,12)` | — |
| E | 40 sentences, ~2,200 chars each | 40 patches, 3 ms | `0, …` |
| F | Arabic sentences, 8 patches | 8 patches, 0 ms | `0, …` |
| G | 600 sentences, 1,328,889 chars | 512 patches, **truncated**, 27 ms | `0, …` |

Three facts this settles:

1. **A terminates.** The old code never returned on A; the fixed code returns in 4 ms.
2. **B is byte-identical to the control.** The unfixed and fixed planners both return 10 patches
   with the exact same ranges (`[0,2025) … [18144,19103)`), so W1 is a progress fix, not a
   re-chunking.
3. **A boundary-less file is cut at the 7680 hard cap, not at the 2048 target** (`CutPiece`
   already extends `reach` to `HardCapTokens`), and every such cut has **zero** overlap because
   there is no sentence start inside the 256–384 window. Both are existing behaviour.

A fourth check, because item 3 alone would look like the overlap died: on 60 real sentences the
unfixed and fixed planners both return 2 patches with identical ranges and a first overlap of
**263 characters** — inside the 256–384 window, unchanged. The zero overlaps in A2, B, E, F and
G are a property of those synthetic documents, not of the fix.

Corrections this pass forced, already applied below: test 1 asserted 2 patches (wrong, it is 1),
test 2 asserted a 384-token overlap (wrong for a boundary-less cut, it is 0), and the file list
omitted 21 call sites that the signature changes break.

## 2. Root cause, ranked, with confidence

| # | Cause | Confidence | Evidence |
|---|---|---|---|
| R1 | `PlanCore` has no forward-progress guarantee, so one file loops forever | **92** | Probe A; `startChar` regresses to 7 and never passes `cutChar` |
| R2 | Every store call re-runs `EnsureSchema`, which re-runs the full `CopyChunkPairsToChunkPath` | **88** | Table 1.2; linear allocation in table size |
| R3 | Whole-file loads with no size gate (`ReadAllTextAsync`, `ReadAllBytesAsync`, `IAudioSource.Read`) | **80** | `DirectoryGrantedTextReader.cs:323`, `VisionFileIngest.cs:61,106`, `AudioFileIngest.cs:76` |
| R4 | `ForegroundScanService.ScanAsync` materializes every document's full text before diffing | **75** | `ForegroundScanService.cs:36-47` |
| R5 | `LocalOpLogWriter.entries` is unbounded and every insert appends | **72** | `LocalOpLogWriter.cs:13,49` |
| R6 | `ingest_chunk_path` has no index on `path`, and path-filtered scans exist | **62** | `CheckpointSql.cs:22-23` |
| R7 | One transaction per chunk per file | **60** | `TextIngestEngine.RememberChunk` → `AppendLiveChunk` per patch |
| R8 | The embedder or the HNSW index is the cause of the climb | **15** | Rejected: probe A shows the blow-up with no embedder call at all |

### R1 in one paragraph

`PlanCore` (`DocumentPatchPlanner.cs:152`) ends each iteration with
`startChar = OverlapStart(sentenceStarts, cutToken, cutChar)` (`:180`). `OverlapStart`
(`:405`) first looks for a sentence start inside the 256–384 token overlap window; when it
finds none it falls back to **any** sentence start older than `cutToken` (`:430-437`). That
fallback ignores the window. With one sentence at the top of the document and no boundary after
it, `CutPiece` returns the same `cutChar` every iteration and `OverlapStart` returns position 7
every time, so `startChar` moves **backwards** and the loop never terminates.

Every iteration mints a unique `contentId` (`ContentIdBuilder.Build(..., ordinal)`), so
`AnyLivePath` never short-circuits it. The loop embeds, inserts into ZVec, appends to the
op-log, appends to two SQLite tables, and grows `patches`, `resultChunks`, `entries` and the
GC heap. `FinishPath` and `MarkGrantFinished` never run, so `PassRunning` stays true and the
text meter freezes while RAM climbs. That is exactly the reported symptom.

## 3. The resource model (replaces a bare percentage gate)

A percentage gate alone is wrong, exactly as the user said: on a 3 GB device 50% is 1.5 GB,
which the model can consume by itself. The gate must compare against a **measured floor**, not
against zero.

### 3.1 The three quantities

| Quantity | Source | Notes |
|---|---|---|
| `CapacityBytes` | `GC.GetGCMemoryInfo().TotalAvailableMemoryBytes` | cgroup-aware; on Android this is the GC heap limit, so the ceiling is stricter than half the phone |
| `FloorBytes` | `ModelWeightsBytes + ComputeScratchBytes + ReserveBytes + FrameworkBaseBytes` | the cost of having the app open at all |
| `WorkingSetBytes` | `Environment.WorkingSet` | live process cost |

- `ModelWeightsBytes` = `new FileInfo(modelPath).Length`. Exact, no constant.
- `ComputeScratchBytes` = provisional 1.25 GiB until measurement **M1** replaces it.
- `ReserveBytes` = 256 MiB safety margin.
- `FrameworkBaseBytes` = `Environment.WorkingSet` sampled once, before the model is opened.

### 3.2 Device verdict, decided **before** the model is loaded

`MemoryBudgetPolicy.Verdict(capacity, floor)`:

| Verdict | Condition | Behaviour |
|---|---|---|
| `BelowMinimum` | `floor > 50% of capacity` | **hard gate at startup.** The embedder is not opened, ingest is refused, and the strip says the device is below minimum. No retry, no cooldown. |
| `Tight` | `floor > 40% of capacity` | ingest runs one file at a time, media skipped, no prefetch |
| `Ok` | otherwise | normal |

A 2 GB or 3 GB device therefore gets `BelowMinimum` **without ever loading the model** — the
harmful case the user named. The rule is checked on the projection, not after the damage.

Derived minimum and recommended, from the formula, not from taste:
`minimum ≈ FloorBytes / 0.50`, `recommended ≈ FloorBytes / 0.30` (headroom to index). With the
provisional floor near 3.3 GB that reads minimum 8 GB, recommended 16 GB. Those two numbers are
**provisional until M1** and are written into the docs with their derivation, not as magic.

### 3.3 Pressure, with hysteresis

`MemoryBudgetPolicy.Classify(capacity, workingSet, floor)` uses
`effective = Math.Max(workingSet, floor)`:

| Pressure | Condition | Behaviour |
|---|---|---|
| `Normal` | below 40% | index normally |
| `Soft` | 40% to 50% | finish the file in hand; start no new file |
| `Hard` | 50% or above | stop the file at the next batch boundary, release, cool down |

### 3.4 Cooldown, not a thrown exception

`ThrowIfHard` is the wrong shape and is dropped. Reasons: a pressure event is routine, not a
programmer error; an exception would land in `FailedPaths` and read as a broken file; and a
throw unwinds without releasing anything. The gate returns a **state**, and the pass returns.

`MemoryGate` is a pure state machine (`src/Recall.Core/Runtime/MemoryGate.cs`):

| From | Event | To | Effect |
|---|---|---|---|
| `Open` | `Hard` | `Cooling` | `NextAttemptAt = now + 30 s`, `Backoff = 30 s` |
| `Cooling` | `now < NextAttemptAt` | `Cooling` | work stays closed |
| `Cooling` | `now >= NextAttemptAt` and pressure at or below 35% | `Open` | resume |
| `Cooling` | `now >= NextAttemptAt` and pressure above 35% | `Cooling` | `Backoff = min(Backoff × 2, 300 s)` |
| `Open` | a file finishes | `Open` | `Backoff = 30 s` |
| any | `BelowMinimum` | `Blocked` | terminal for the process |

`ResumePercent = 35` sits below `SoftPercent = 40` so the gate cannot flap. There is no
`Thread.Sleep` in the ingest loop: the pass **returns**, and the existing `IngestScheduler` /
idle trigger re-runs it. Resume is a re-trigger, and the file resumes because its cursor is
`Reading` with a status key and its stored chunks are still live.

### 3.5 Releasing what is held, during the cooldown

In this order, and only in this order:

1. **Flush the pending batch** — `AppendLiveChunks` for chunks already embedded. No data loss.
2. **Drop the file's buffers** — the document string, the `int[] ends`, the `PatchPlan`, and the
   `List<TextIngestChunk>` for the file in hand. These are the actual held megabytes.
3. **Close the optional encoders only** — `IAudioEmbeddingEncoder.CloseAudio()` when it is
   loaded and no audio work is queued. Vision needs nothing: `VisionModelRuntime.EmbedJpeg`
   already closes its context after every call.
4. **Do not unload the text encoder.** Canon and the `performance` checklist forbid unloading
   it to make an RSS number look good, and a reload costs more than the cooldown saves.
5. **One LOH compaction**, only when a dropped buffer exceeded 8 MB:
   `GC.Collect(2, GCCollectionMode.Forced, blocking: false, compacting: true)`. Without it the
   working set stays high after a multi-hundred-MB release and the gate never reopens, which is
   a stall dressed as a budget.
6. **Re-probe** after the cooldown. The gate opens only on a measured drop.

### 3.6 Decisions accepted by the user (2 Nov 2026)

| # | Decision | Consequence |
|---|---|---|
| D1 | Publish `minimum ≈ floor/0.5`, `recommended ≈ floor/0.3`; a device below the line is hard-`Blocked` | a 2–3 GB device gets no search, with a reason on screen |
| D2 | 512 patches per document, partial index, visible note | documents over ~26 MB are indexed only in part |
| D3 | `IngestChromePolicy.Status` gets a **required** `DeviceVerdict` parameter | 11 test call sites are edited in W1b; a missed call cannot compile |
| D4 | A memory-stopped file stays `Reading`; that pass's meter stays below 100% | no false coverage claim |
| D5 | M1 gates the published table, not the code; it is split into a protocol (M1a) and one run per device class (M1b) | the table is measured on representative hardware, never on this workstation alone |

`canon.md` is updated only when the plan itself is accepted, not on this record.

## 4. Waves

- **W1 — forward progress and per-file caps.** `PlanCore` advances or stops; `PatchPlan` carries
  `Truncated`; `MaxPatchesPerDocument = 512`; stop reasons reach the cursor store.
- **W2 — one schema pass, one transaction per batch.** `EnsureReady` once per store instance,
  migration via `PRAGMA user_version`, backfill as one SQL statement, `AppendLiveChunks`,
  `ix_ingest_chunk_path_path`, `MarkStopped`, `FinishPath` with a stop key.
- **W3 — the floor, the verdict, the gate.** `MemoryBudgetPolicy` (pure), `ProcessMemoryBudget`,
  `MemoryGate`, the reclaimer, enforcement at load/scan/read/per-batch, and the blocked state in
  the resource strip.
- **W3b — the floor measures itself.** `MemoryFloorStore`, `RecordObserved`, and
  `EffectiveFloor = max(declared, observed)`, so the gate is exact from the second run onward.
- **W4 — bounded op-log.** In-memory mirror capped at 10,000 entries; the day file stays durable.
- **W5 — media guards.** File-size guard before every whole-file read, frame cap 600, Windows
  thumbnail batches of 64.
- **M1 — measurement (manual, not a test).** **M1a** is the protocol: base working set, post-load
  working set, and peak working set during a 512-chunk file, written so any device can run it.
  **M1b** is one run per device class in §6 (4, 8, 16 GB) on hardware that is **not** this
  workstation. This laptop's numbers are a sample only and are labelled as such. Fills
  `docs/performance.md` and the profile table; it does not change the declared floor, which the
  runtime corrects by itself.

Not in this plan, with a named reason: the native embedder's `n_threads`/`n_ubatch` tuning
(`native/recall_embed.c:169-171`). It changes vector math and the repo has no golden-vector
gate to prove it safe. It is listed under Alternatives with the gate it needs.

## 6. Blocker resolutions (third pass)

The user asked for a solution to each blocker before approving. None of the three needs a new
product decision; each is closed by a named mechanism below.

### B1 — the floor is unmeasured → the floor measures itself

**Solution.** The declared floor stays as the *first-run* bound, and the process records what it
actually paid, then uses the larger of the two on every later run.

1. `ComputeScratchBytes` is no longer a bare guess. It is an upper bound from context geometry:
   the context is `n_ctx = 7680`, `n_embd = 768`, 18 layers (`native/recall_embed.c:169`), so a
   f16 K/V cache is `2 × 7680 × 768 × 18 × 2 B ≈ 425 MB`; the declared 1.25 GiB adds activation
   and graph headroom on top of that. The derivation goes in ADR 0016 beside the number.
2. After the first file of the process completes, `IMemoryBudget.RecordObserved()` stores
   `Environment.WorkingSet - FrameworkBaseBytes` in `memory-floor.json` under app data.
3. Every later run uses `EffectiveFloor = Math.Max(declared, observed)`. It can only **tighten**
   the gate, never loosen it below the declared bound.
4. Corrupt or missing file → declared floor, no throw.

**What this buys.** The very first run on a small device is conservative (may over-block); run 2
onwards uses the machine's real cost. M1 becomes a documentation step that fills the published
table, not a code dependency.

**This workstation is not a reference.** The 64 GB machine that produced §1.1-1.4 is a sample, not
a baseline, and the docs must say so. Only two inputs to the floor are per-device: the framework
base (measured) and the observed delta (measured). The weights are a file length and the scratch
is KV geometry (`2 × n_ctx × n_embd × layers × 2 B` plus headroom), so the *rule* is portable
while the *sample* is not.

The published table therefore has three rows derived from the formula for device classes, and a
column that says what the app does there. With the declared floor near 3.3 GiB:

| Class | Capacity | 50% line | Verdict | What the user sees |
|---|---|---|---|---|
| small | 4 GB | 2.0 GB | `BelowMinimum` | blocked, reason shown, no retry |
| mid | 8 GB | 4.0 GB | `Tight` | one file at a time, media skipped |
| normal | 16 GB | 8.0 GB | `Ok` | normal ingest |

The rows are re-checked against `MemoryBudgetPolicy` by a test, so the page cannot drift from the
code. M1 is split to match: **M1a** is the protocol (base, post-load, peak; runnable on any
device), **M1b** is one run per class on hardware that is not this laptop. A device the app has
never seen computes its own floor on its first run and tightens it on the second.

### B2 — no golden-vector gate for native tuning → remove it from this plan, by scope

**Solution.** No native change lands here. The 50% ceiling is enforced on the process, so a fat
constant cannot exceed it; the thread count does not affect ingest memory, only CPU share.

- `native/recall_embed.c` is untouched, and §4's "not in this plan" line stays.
- ADR 0016 records the reason in one sentence: `n_ubatch` changes batching order, the repo has no
  golden-vector fixture, and an unverifiable embed change is not shippable.
- The prerequisite is named: a checked-in fixture (one mixed Arabic/English text, one JPEG, one
  audio window) plus a cosine ≥ 0.9999 comparison against a recorded baseline, running only when
  the model file exists. That is its own plan, not a rider on this one.
- The follow-up is written down, not implied: `docs/decisions/open.md` gets one line naming the
  fixture, the cosine floor, and the fact that `n_threads` and `n_ubatch` are both untuned.

**What this buys.** No unverifiable change, and the ceiling still holds. CPU share stays with the
existing idle-gated scheduler.

### B3 — recall@10 unmeasured → prove the boundaries instead of sampling them

**Solution.** The fix edits exactly one assignment, `startChar = next`. It now differs from the
old code only when `next <= startChar` or `next >= cutChar` — that is, only when the old planner
produced a patch that started at or before the previous one, or beyond its own cut. Those are the
non-terminating and backwards-overlapping cases. So:

1. Add `Plan_BoundariesAreStableForTerminatingDocuments`: three recorded fixtures (prose, a
   table-heavy CA‑like block, Arabic sentences) with the current patch ranges written as literals;
   assert the planner still returns those exact ranges.
2. Record the argument above in ADR 0016, next to the two measured cases (control 10 patches
   identical; 60 sentences 2 patches identical with a 263-character overlap).
3. recall@10 stays a monitoring condition in `nlp`'s sign-off, not a release blocker.
4. The follow-up is written down: `docs/decisions/open.md` gets one line naming the golden corpus,
   the query set, and the fact that no recall@10 run exists after a chunk-boundary change.

**What this buys.** A test that goes red if any terminating document's boundaries shift, and no
claim that rests on a corpus run that does not exist yet.

## 5. Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| `ComputeScratchBytes` is wrong until M1, so the verdict is wrong on small devices | medium | the declared value is an upper bound from KV geometry, and the observed floor tightens it from run two |
| The 8 GB minimum reads as a product cut | medium | the number is derived and shown with its formula; `Tight` lets 8 GB devices index one file at a time |
| The patch cap hides a real index gap on a huge document | medium | the file finishes with a visible note |
| Batching cursor writes widens the orphan-vector window | medium | flush every 64 chunks; ids are deterministic, so retry is idempotent |
| A forced GC after a big release becomes a habit | medium | it is allowed only above 8 MB and only inside the cooldown step |
| A composition root misses the new ctor argument | medium | required parameters, so a miss is a compile error |

## Expert sign-offs

Direct: performance, dotnet-architect, vectors-embeddings, nlp, maui-platform, ux-ui

```yaml
- expert: performance
  verdict: approve-with-conditions
  confidence: 90
  checked: "The W1 edits were executed against a copy of the real planner: probe A terminates in 4 ms, the control keeps its 10 patches, a 1.33 MB document stops at 512 patches in 27 ms — .cursor/plans/ingest-memory-hardening.md:1.4"
  pushback: "The first run on a small device uses the declared floor, so it can over-block until the observed value lands on run two."
  mitigation: "RecordObserved only tightens the gate, and the published table is three device classes, never this workstation."
  concerns:
    - "No BenchmarkDotNet RSS cell exists; M1 is a manual measurement and can drift."
    - "A forced GC in the release step can mask a leak if it is ever called outside the cooldown."
  recommendation: "Land M1 with W3, and record the three working-set numbers with their probe."

- expert: dotnet-architect
  verdict: approve-with-conditions
  confidence: 83
  checked: "The verdict is decided before the model opens, so the gate lives at composition and at both ingest entry points — src/Recall.Core/DependencyInjection/RecallCoreServiceCollectionExtensions.cs:44"
  pushback: "IMemoryBudget is now constructed with the model path, which AddRecallCore does not know, and the floor adds a persisted file."
  mitigation: "One registration in the MAUI root, one default for headless tests, and a missing or corrupt floor file falls back to the declared value."
  concerns:
    - "No new project and no new dependency; the gate is pure Core plus one port and one small store."
    - "MemoryGate is a state machine, not a service with a timer, so time is passed in and tests stay deterministic."
  recommendation: "Register IMemoryBudget once in the MAUI root and keep the floor store failure-tolerant."

- expert: vectors-embeddings
  verdict: approve-with-conditions
  confidence: 82
  checked: "Insert order stays vector-then-cursor and TryInsert is idempotent by content id — src/Recall.Adapters/Indexing/ZVecTextVectorIndex.cs:112"
  pushback: "The floor counts the text model but nothing accounts for the ZVec collection's resident index on a large corpus."
  mitigation: "The floor is a projection plus the live working set, so a large and growing index still shows up in the measured half."
  concerns:
    - "No dimension, width, collection, or HNSW parameter changes."
    - "Releasing the text encoder is explicitly forbidden, so the gate must refuse work rather than reclaim it."
  recommendation: "Keep the 64-chunk flush and add the index residency observation to M1."

- expert: nlp
  verdict: approve-with-conditions
  confidence: 91
  checked: "The unbounded fallback in OverlapStart is what defeats the 256-384 token overlap, and the change is provably boundary-stable for every terminating document — src/Recall.Core/Text/DocumentPatchPlanner.cs:430"
  pushback: "Only documents that used to loop change, and recall@10 is still unmeasured, so the claim rests on identical boundaries, not on retrieval numbers."
  mitigation: "Boundaries are identical on 60 sentences (2 patches, first overlap 263 chars) and on the 400-line table control (10 patches, same ranges); test 39 records three fixtures."
  concerns:
    - "Chunk boundaries decide BM25 payload and embedding context, so an identical output is the argument, not a corpus run."
    - "Arabic and English boundary marks are unchanged."
  recommendation: "Run the golden corpus after W1 and compare recall@10."

- expert: maui-platform
  verdict: approve-with-conditions
  confidence: 81
  checked: "Text reads use ReadAllTextAsync with no byte cap, and Android reports the GC heap limit rather than device RAM — src/Recall.Core/Ingest/DirectoryGrantedTextReader.cs:323"
  pushback: "On a 3 GB phone the heap limit is far below 3 GB, so the verdict will block a device that has the RAM."
  mitigation: "Blocking is the safe direction, the copy names the limit, and the small class row makes it visible instead of surprising."
  concerns:
    - "Windows-only frame batching leaves the Android and Apple samplers returning every frame at once."
    - "The iOS foreground scan stops on pressure with only the status key to explain it."
  recommendation: "Cap frames in FrameCadence so every sampler inherits it, and record the Android reading in ADR 0016."

- expert: ux-ui
  verdict: approve-with-conditions
  confidence: 80
  checked: "IngestChromePolicy.Status has no blocked arm and the strip has no line for a below-minimum device — src/Recall.Core/Ingest/IngestChromePolicy.cs:14"
  pushback: "A hard startup gate with no visible reason looks like a broken app on a small device."
  mitigation: "One new chrome status plus one resx pair, rendered where ingest status already renders."
  concerns:
    - "Four new keys need Arabic review and RTL check."
    - "Blocked must not offer a retry button, because retrying cannot succeed."
  recommendation: "Render Blocked without the retry button and route the Arabic copy through review."

- expert: unit-tests
  verdict: approve-with-conditions
  confidence: 86
  checked: "MemoryGate takes time as a parameter, the W1 call sites are enumerated with line numbers, and the manifest now carries 40 named test rows — src/Recall.Core/Runtime/MemoryGate.cs:1"
  pushback: "The allocation ratio assertion remains timing-adjacent in a unit suite."
  mitigation: "The bound is 3x against a current 13x ratio."
  concerns:
    - "PortFakes must implement AppendLiveChunks, MarkStopped, and a fake IMemoryBudget with a settable verdict and floor."
    - "Twenty-one existing call sites break on the new signatures; each one is listed in W1b."
  recommendation: "Land the fake, the W1b call-site edits, and the gate transition tests with their waves."

- expert: retrieval-qa
  verdict: approve-with-conditions
  confidence: 76
  checked: "Cursor writes move from per-chunk to per-64-chunk and no kill-between-flush test exists — src/Recall.Core/Text/TextIngestEngine.cs:240"
  pushback: "A kill inside the 64-chunk window is untested and recall@10 after the boundary change is unmeasured."
  mitigation: "Ids are deterministic and TryInsert is idempotent, so the named crash test can assert a clean reopen."
  concerns:
    - "The golden corpus has no table-heavy PDF fixture that used to hang the planner."
    - "Resume after a cooldown is unproven end to end."
  recommendation: "Add the kill-between-flush test and one hanging-shape fixture before W1 ships."

- expert: privacy-local-first
  verdict: approve
  confidence: 95
  checked: "The budget reads GC, process, and file-length values only and adds no network, telemetry, or identifier — src/Recall.Core/Runtime/ProcessMemoryBudget.cs:1"
  pushback: "none"
  mitigation: "none"
  concerns:
    - "New status keys travel through the local cursor row."
    - "No new path leaves the device."
  recommendation: "none"

- expert: docs-mkdocs
  verdict: approve-with-conditions
  confidence: 84
  checked: "The page states Status: open and no per-file limit exists, which this plan makes false — docs/features/file-size-cap.md:1"
  pushback: "A minimum-spec table without its derivation would read as a marketing number."
  mitigation: "The table is three derived device classes, locked against the policy by test 41, and the workstation sample is labelled as a sample."
  concerns:
    - "ADR 0016 must not read as a rewrite of ADR 0004 or 0005."
    - "No docs/plan.md row is added."
  recommendation: "Publish the requirement table with its formula and update it in the M1 commit."

```

## Plan synthesis

**Trigger.** The freeze at 34/41 files, then the 50% constraint, then this correction: a
percentage gate without a measured floor and a cooldown is not a design.

**Blockers.** None open. B1 is closed by the self-measuring floor (§6), B2 by removing native
tuning from scope, B3 by the boundary-equivalence argument plus a recorded-range fixture. M1 stays
a documentation step for the published table, not a code dependency.

**Push-backs.** Four new. `performance`: the floor constant is provisional. `dotnet-architect`:
the model path lives in the MAUI root, so that root owns the budget registration.
`vectors-embeddings`: index residency is not in the floor and must be observed in M1.
`ux-ui`: a hard gate with no visible reason reads as a broken app.

**Answers to the two questions.**

1. *A percentage is the wrong primitive.* The gate now uses
   `FloorBytes = weights + scratch + reserve + framework base` and refuses below-minimum devices
   **before** the model loads. `minimum ≈ floor/0.5`, `recommended ≈ floor/0.3`; with the
   provisional floor that is 8 GB and 16 GB, published with the formula, not as taste.
2. *Cooldown, not `ThrowIfHard`.* A throw would land in `FailedPaths` and unwind without
   releasing anything. The gate is a state machine with 40/50/35 hysteresis, a 30 s cooldown
   doubling to 300 s, release in six named steps (flush, drop buffers, close the audio encoder
   only, never unload the text encoder, one LOH compaction above 8 MB, re-probe), and resume by
   the existing scheduler re-triggering the pass.

**Conditions.** W1 with its test first. W2 deletes `CopyChunkPairsToChunkPath`. W3 ships with
M1 and with the `Blocked` chrome state, or it is not accepted. Both resx files in the same
commit. `docs/features/file-size-cap.md` rewritten in the W1 commit.

**Conflicts.** `performance` would raise the patch cap; `nlp` and `retrieval-qa` would keep it
low until recall is measured — resolved at 512. `vectors-embeddings` wants the cursor row before
the vector; `performance` wants it after — resolved as insert-then-flush. `maui-platform` says
Android will over-block; `performance` says over-blocking is the safe side — resolved by
recording it in ADR 0016 and leaving the platform read for later.

**Test matrix gaps.** Kill-between-flush. Recall@10 after the boundary change. An RSS cell in
`Recall.Benchmarks`. A test that a `Soft` pass finishes the file in hand. The cooldown table is
covered by unit tests; the *real* cooldown on a loaded machine is not.

**Corrections from the second verification pass.** (1) Test 1 asserted 2 patches; the executed
fix returns 1, so the assertion is now `Patches.Count == 1`, `[0,5007)`. (2) Test 2 asserted a
384-token overlap on a boundary-less document; that cut has **zero** overlap because no sentence
start falls inside the window, so test 2 now locks the exact hard-cap ranges of a 20,007-char
document. (3) The file list omitted 21 call sites that the signature changes break — 12 `Plan(`
uses in four test files, four `TextIngestEngine` constructions, seven `GrantedTextIngestService`
constructions, four `VisionFileIngest`, two `AudioFileIngest`, three `ForegroundScanService`,
two `DirectoryGrantedTextReader`, and eleven `IngestChromePolicy.Status` calls. They are W1b
above. (4) `UpsertFinishedFile` writes no `missing_status_key` today, so the PatchCap note would
have been silently dropped; the plan now names both SQL clauses. (5) The gate check moved ahead
of `MarkGrantReading` so a blocked pass writes no row.

**Recommended plan changes.** (1) W1 plus its test in one commit. (2) W2 before W3. (3) M1
before W3 is accepted. (4) W5 last. (5) Keep native tuning out until a golden-vector gate exists.
(6) Treat W1b as part of W1, not as cleanup.

**Left unresolved on purpose.** Two follow-ups are now written into `docs/decisions/open.md`
instead of being implied: the golden-vector fixture that would let `n_threads`/`n_ubatch` be
retuned, and the golden corpus plus query set that a recall@10 run after a boundary change needs.
Also unresolved: streaming video decode, PDF renderer reuse, a BenchmarkDotNet RSS cell, and
reading device RAM on Android instead of the GC heap limit.

**Alternatives.** (a) A per-modality floor instead of one floor — rejected as more constants for
no measurable gain. (b) `Partial` cursor state instead of resume-via-status-key — rejected for
the meter and policy churn. (c) Throwing on Hard — rejected in §3.4. (d) Unloading the text
encoder during cooldown — forbidden by canon.

**Ready for the user.** Approve or reject. Five decisions were accepted on 2 November (D1–D5,
§3.6) and are recorded, not re-opened. Root-cause confidence **92**. Whole-plan solution
confidence **84**. W1 alone **95** — it was executed against a copy of the real planner and it
terminated on eight document shapes while the control kept its exact patch ranges. The gate is
**87**. No claim in this plan rests on this workstation's memory size.

## Execution manifest

### Files

**W1 — forward progress (fixes the reported freeze)**

1. `src/Recall.Core/Text/DocumentPatchPlanner.cs` — edit.
   - `public const int MaxPatchesPerDocument = 512;`
   - `public readonly record struct PatchPlan(IReadOnlyList<PdfPatch> Patches, bool Truncated);`
   - `public static PatchPlan Plan(string title, string joinedDocument, IReadOnlyList<PdfPageText>? pages, int prefixTokenCount, IReadOnlyList<int>? tokenEnds)` — result `PatchPlan`; failure `ArgumentException` on a blank document, `ArgumentNullException` on null `tokenEnds`, both unchanged.
   - `private static PatchPlan PlanCore(string document, IReadOnlyList<PdfPageText>? pages, int prefixTokenCount, int[] ends)`.
   - branch 1: `patches.Count == MaxPatchesPerDocument` at loop top → `return new PatchPlan(patches, true)`.
   - branch 2: `cutChar <= startChar` → `cutChar = documentLength; cutToken = endCount == 0 ? 0 : endCount - 1;`
   - branch 3: `cutChar >= documentLength` → add patch, `ordinal++`, `break` before the overlap line.
   - branch 4: `var next = OverlapStart(...); startChar = next > startChar && next < cutChar ? next : cutChar;`
   - `OverlapStart`: the fallback loop returns `fallback` as soon as `token < cutToken - MaxOverlapTokens`; it never returns a position older than the window.
2. `src/Recall.Core/Text/IngestStopReason.cs` — add. `public enum IngestStopReason { None = 0, MemoryBudget = 1, PatchCap = 2 }`.
3. `src/Recall.Core/Text/TextIngestEngine.cs` — edit.
   - `TextIngestResult` gains `public IngestStopReason Stop { get; init; }`.
   - ctor gains `IMemoryBudget memory` and `MemoryGate gate` as required parameters.
   - `IngestFileAsync` / `IngestOcrPagesAsync` consume `PatchPlan`; `Stop` set from `Truncated`.
   - `InsertPatchesAsync` returns `(List<TextIngestChunk> Chunks, IngestStopReason Stop)`.
   - branch per patch: `!gate.IsOpen(now)` → break with `MemoryBudget`.
   - branch per batch: `pending.Count == IngestBatchChunkCount` → `cursors.AppendLiveChunks(filePath, pending); pending.Clear();`
   - final flush before return on both the normal and stopped paths.
4. `src/Recall.Core/Ingest/IngestStopKeys.cs` — add. `PatchCap = "Ingest_StoppedPatchCap"`, `MemoryBudget = "Ingest_StoppedMemoryBudget"`, `FileTooLarge = "Ingest_SkippedTooLarge"`, `DeviceTooSmall = "Ingest_DeviceTooSmall"`.
5. `src/Recall.Core/Ingest/GrantedTextIngestService.cs` — edit.
   - ctor gains `IMemoryBudget memory` and `MemoryGate gate` before the optional parameters.
   - `IngestGrantAsync` branch before each document: `!gate.IsOpen(now)` → `return` without `MarkGrantFinished`.
   - `IngestDocumentAsync` branch: `PatchCap` → `FinishPath(..., stopStatusKey: IngestStopKeys.PatchCap)`.
   - `IngestDocumentAsync` branch: `MemoryBudget` → `SetStopNow = gate.EnterHard(now)`, `cursors.MarkStopped(...)`, `reclaimer.ReleaseOptionalEncoders()`, `RecordStopped`, no `FinishPath`.
   - `CloseLeftoverReading` branch: skip `State == Reading && MissingStatusKey != ""`.
   - the gate check sits **before** `cursors.MarkGrantReading(grant.Path)` at `:137`, so a closed gate writes no row; `MarkGrantFinished` at `:245-248` is skipped by that return, which is correct.

**W1b — every call site the W1 signature changes break (same commit, or it does not compile)**

- `tests/Recall.UnitTests/Phase5PdfTests.cs` — edit `Plan(` at `47,61,72,83,94,108,122,132,143` to read `.Patches`.
- `tests/Recall.UnitTests/Phase2CloseTests.cs` — edit `Plan(` at `41,50,57`.
- `tests/Recall.UnitTests/ReviewFixTests.cs` — edit `Plan(` at `56`.
- `tests/Recall.IntegrationTests/RealPdfPatchTests.cs` — edit `Plan(` at `18`.
- `tests/Recall.UnitTests/Fakes/PortFakes.cs` — edit `new TextIngestEngine(` at `965`.
- `tests/Recall.IntegrationTests/OcrDemoProofTests.cs:198`, `tests/Recall.IntegrationTests/OcrGoldenReportTests.cs:127`, `tests/Recall.Maui.UnitTests/OcrRuntimeTests.cs:189`, `tests/Recall.UnitTests/CloseOpenOcrTests.cs:174` — edit `new TextIngestEngine(`.
- `tests/Recall.UnitTests/IngestMediaStartTests.cs` — edit seven `new GrantedTextIngestService(` at `25,39,79,111,127,165,183` and `new VisionFileIngest(` at `146`.
- `tests/Recall.UnitTests/Phase4VisionTests.cs:294`, `tests/Recall.UnitTests/Phase6AgePinSearchTests.cs:428,580` — edit `new VisionFileIngest(`.
- `tests/Recall.UnitTests/Phase5AudioTests.cs:1197` — edit `new AudioFileIngest(`.
- `tests/Recall.UnitTests/Phase3WatchTests.cs:626,674,743` — edit `new ForegroundScanService(`.
- `tests/Recall.UnitTests/CloseAddendumGapsTests.cs:436` — edit `new DirectoryGrantedTextReader(`.
- `src/Recall.Adapters/DependencyInjection/AdapterServiceCollectionExtensions.cs:56` — add `IMemoryBudget` to `new VisionFileIngest(`.
- `src/Recall.Maui/Platform/Folders/MauiGrantedTextReader.cs:27` — add `IOversizeFileSkipReport` to `new DirectoryGrantedTextReader(`.
- `tests/Recall.UnitTests/ModelDownloadRowPolicyTests.cs:107,112,117,122` and `tests/Recall.UnitTests/ShellChromeTests.cs:17,38,50,60,75,95,109` — add the `DeviceVerdict` argument to `IngestChromePolicy.Status(`; the parameter is required, not defaulted.

**W2 — one schema pass, one transaction per batch**

6. `src/Recall.Core/Checkpoints/CheckpointSql.cs` — edit: `SchemaVersion = 1`, `CreateChunkPathPathIndex`, `BackfillChunkPath`, `ReadUserVersion`, `WriteUserVersionV1`; append the index to the `EnsureSchema` bundle.
7. `src/Recall.Core/Checkpoints/SqliteIngestCursorStore.cs` — edit: `schemaReady` field, `EnsureReady`, `ApplyMigrations`, delete `CopyChunkPairsToChunkPath` and its call at `:1572`, `AppendLiveChunks`, `AppendLiveChunk` delegating, `MarkStopped`, `FinishPath(..., string? stopStatusKey = null)`. `UpsertFinishedFile` gains the same parameter and writes `missing_status_key = @key` (`''` when null) in **both** the `INSERT` and the `ON CONFLICT(path) DO UPDATE` clause — today it writes neither, so a stale key would survive a finished file.
8. `src/Recall.Core/Checkpoints/IIngestCursorStore.cs` — edit: two members plus the `stopStatusKey` parameter.
9. `tests/Recall.UnitTests/Fakes/PortFakes.cs` — edit: implement both members, the parameter, `FakeMemoryBudget` (settable `CapacityBytes`, `WorkingSetBytes`, `FloorBytes`, `Verdict`), and a reset-able `MemoryGate`.
10. `src/Recall.Core/Ingest/IngestStatusService.cs` — edit: ctor gains `IMemoryBudget memory`; `ListMissingNotes` also returns `Finished` rows with a status key; add `DeviceVerdict Verdict { get; }`.

**W3 — floor, verdict, gate**

11. `src/Recall.Core/Runtime/MemoryPressure.cs` — add. `Normal = 0, Soft = 1, Hard = 2`.
12. `src/Recall.Core/Runtime/DeviceVerdict.cs` — add. `Ok = 0, Tight = 1, BelowMinimum = 2`.
13. `src/Recall.Core/Runtime/MemoryBudgetOptions.cs` — add. `SectionName = "MemoryBudget"`, `ComputeScratchBytes = 1_342_177_280`, `ReserveBytes = 268_435_456`, `ResumePercent = 35`, `CooldownSeconds = 30`, `MaxCooldownSeconds = 300`, `LargeBufferBytes = 8_388_608`.
14. `src/Recall.Core/Runtime/MemoryBudgetPolicy.cs` — add.
    - `SoftPercent = 40`, `HardPercent = 50`, `TextBytesPerChar = 2`, `AudioExpansion = 2`, `VideoExpansion = 1`.
    - `LimitBytes(capacity)`, `HeadroomBytes(capacity, workingSet)`, `FloorBytes(...)`, `Verdict(capacity, floor)`, `Classify(capacity, workingSet, floor)`, `MayLoadFile(fileBytes, expansion, capacity, workingSet)`, `MinimumSupportedBytes(floor)`, `RecommendedBytes(floor)`.
    - failure arms: `capacity <= 0` → `Normal`/`Ok`; `expansion <= 0` → `false`.
15. `src/Recall.Core/Runtime/IMemoryBudget.cs` — add. `CapacityBytes`, `WorkingSetBytes`, `FloorBytes`, `Verdict`, `Pressure`, `MayLoadFile(long, int)`.
16. `src/Recall.Core/Runtime/ProcessMemoryBudget.cs` — add. ctor `(long modelWeightsBytes, MemoryBudgetOptions options)`; `FrameworkBaseBytes` sampled at construction; values delegate to the policy.
17. `src/Recall.Core/Runtime/MemoryGateState.cs` — add. `Open = 0, Cooling = 1, Blocked = 2`.
18. `src/Recall.Core/Runtime/MemoryGate.cs` — add. Pure, time is a parameter.
    - `bool IsOpen(DateTimeOffset now)`
    - `void Observe(MemoryPressure pressure, DateTimeOffset now)` — the transition table of §3.4.
    - `void EnterHard(DateTimeOffset now)` — `Open → Cooling`, `NextAttemptAt = now + CooldownSeconds`.
    - `void OnFileCompleted()` — `Backoff = CooldownSeconds`.
    - `void Block()` / `MemoryGateState State`.
19. `src/Recall.Core/Runtime/IIngestResourceReclaimer.cs` — add. `void ReleaseOptionalEncoders();`
20. `src/Recall.Core/Runtime/IngestResourceReclaimer.cs` — add. ctor `(IAudioEmbeddingEncoder? audio)`; calls `CloseAudio()` only when `audio is { IsLoaded: true }`.
21. `src/Recall.Core/DependencyInjection/RecallCoreServiceCollectionExtensions.cs` — edit: `MemoryGate` singleton, `IOversizeFileSkipReport` singleton, `MemoryBudgetOptions` from configuration, and a default `IMemoryBudget` for headless tests.
22. `src/Recall.Maui/DependencyInjection/MauiServiceCollectionExtensions.cs` — edit: `Replace` the `IMemoryBudget` singleton with `new ProcessMemoryBudget(new FileInfo(Path.Combine(modelDirectory, textOptions.ModelFileName)).Length, options)`; register `IIngestResourceReclaimer`.
23. `src/Recall.Core/Ingest/ForegroundScanService.cs` — edit: ctor gains `IMemoryBudget memory` and `MemoryGate gate`; one walk for hashes, a `HashSet<string>` of changed paths, a second walk for text; branch `!gate.IsOpen(now)` → return before the next grant.
24. `src/Recall.Core/Ingest/DirectoryGrantedTextReader.cs` — edit: ctor gains `IOversizeFileSkipReport oversize`; branch before `File.ReadAllTextAsync` (`:323`) on `LocalContentPolicy.MaxTextFileBytes` → `oversize.Add(entry); continue;`
25. `src/Recall.Core/Scope/OversizeFileSkipReport.cs` — add. `IOversizeFileSkipReport` plus `OversizeFileSkipReport`, same shape as `DatasetSkipReport`.
26. `src/Recall.Core/Scope/LocalContentPolicy.cs` — edit: `public const long MaxTextFileBytes = 33_554_432;`
27. `src/Recall.Core/Ingest/IngestChromePolicy.cs` — edit: `IngestChromeStatus.Blocked = 3`; `Status(..., DeviceVerdict verdict)` returns `Blocked` first.
28. `src/Recall.UI/Layout/ResourceStrip.razor` — edit: a `Blocked` arm that prints `@T("Ingest_DeviceTooSmall")` and no retry button; `src/Recall.UI/Layout/ResourceStrip.razor.cs` passes `IngestStatus.Verdict`; `src/Recall.UI/Layout/AppShell.razor.cs:100` passes it too; `src/Recall.UI/Layout/ShellStatusLabel.cs:7` gains `IngestChromeStatus.Blocked => "Ingest_DeviceTooSmall"` so the shell label does not fall through to `null`.

**W3b — self-measuring floor (closes B1)**

- `src/Recall.Core/Runtime/MemoryFloorStore.cs` — add. `public const string FileName = "memory-floor.json";` ctor `MemoryFloorStore(string directory)`; `long LoadObserved()` returns `0` when the file is missing, corrupt, or unreadable; `void SaveObserved(long bytes)` is best effort and swallows `IOException` and `JsonException`.
- `src/Recall.Core/Runtime/IMemoryBudget.cs` — edit: add `long EffectiveFloorBytes { get; }` and `void RecordObserved();`
- `src/Recall.Core/Runtime/ProcessMemoryBudget.cs` — edit: ctor gains `MemoryFloorStore floors`; `EffectiveFloorBytes => Math.Max(FloorBytes, floors.LoadObserved())`; `RecordObserved()` runs once per process behind `Interlocked.Exchange`, stores `Environment.WorkingSet - FrameworkBaseBytes` only when it exceeds the stored value.
- `src/Recall.Core/Ingest/GrantedTextIngestService.cs` — edit: call `memory.RecordObserved()` on the `FinishPath` path only, never on a stop path.
- `src/Recall.Core/DependencyInjection/RecallCoreServiceCollectionExtensions.cs` — edit: register `MemoryFloorStore` with the app-data directory.
- `tests/Recall.UnitTests/Fakes/PortFakes.cs` — edit: `EffectiveFloorBytes` and `RecordObserved` on `FakeMemoryBudget`, with a call counter.

**W4 — bounded op-log**

29. `src/Recall.Core/Sync/LocalOpLogWriter.cs` — edit: `Queue<LocalOpLogEntry>`, `MaxEntriesInMemory = 10_000`, `Entries => [.. entries]`, dequeue-before-enqueue branch.

**W5 — media guards**

30. `src/Recall.Core/Ingest/VisionFileIngest.cs` — edit: ctor gains `IMemoryBudget memory`; guards before `File.ReadAllBytesAsync` at `:61` and `:106`; failure arm marks `FileTooLarge` and returns.
31. `src/Recall.Core/Ingest/AudioFileIngest.cs` — edit: ctor gains `IMemoryBudget memory`; guard before `source.Read(path)` at `:76` with `AudioExpansion`.
32. `src/Recall.Core/Ingest/FrameCadence.cs` — edit: `MaxFramesPerFile = 600`; `Starts` stops at the cap.
33. `src/Recall.Maui/Platform/Media/WindowsVideoFrameSampler.cs` — edit: `FrameBatchSize = 64`; `ComposeAsync` loops batches instead of one call over every start.
34. `src/Recall.Maui/MauiProgram.cs` — edit: pass `IMemoryBudget`, `MemoryGate`, and `IIngestResourceReclaimer` at every construction site of `TextIngestEngine`, `GrantedTextIngestService`, `VisionFileIngest`, `AudioFileIngest`, `DirectoryGrantedTextReader`.

**Docs**

35. `docs/features/ingest-memory-budget.md` — add; `mkdocs.yml` — one `Features:` line after `File size cap`.
36. `docs/features/file-size-cap.md` — edit. 37. `docs/performance.md` — edit.
38. `docs/features/text-ingest.md` — edit. 39. `docs/data/stores.md` — edit. 40. `docs/data/oplog.md` — edit.
41. `docs/adr/0016-ingest-memory-ceiling.md` — add; `docs/adr/index.md` — one line after ADR 0015.

### Literals

| Literal | Home | Value |
|---|---|---|
| `MaxPatchesPerDocument` | `DocumentPatchPlanner` | `512` |
| `IngestBatchChunkCount` | `TextIngestEngine` | `64` |
| `MaxEntriesInMemory` | `LocalOpLogWriter` | `10_000` |
| `MaxFramesPerFile` | `FrameCadence` | `600` |
| `MaxTextFileBytes` | `LocalContentPolicy` | `33_554_432` |
| `SoftPercent` / `HardPercent` / `ResumePercent` | `MemoryBudgetPolicy` / `MemoryBudgetOptions` | `40` / `50` / `35` |
| `ComputeScratchBytes` | `MemoryBudgetOptions` | `1_342_177_280` (provisional, replaced by M1) |
| `ReserveBytes` | `MemoryBudgetOptions` | `268_435_456` |
| `CooldownSeconds` / `MaxCooldownSeconds` | `MemoryBudgetOptions` | `30` / `300` |
| `LargeBufferBytes` | `MemoryBudgetOptions` | `8_388_608` |
| `TextBytesPerChar` / `AudioExpansion` / `VideoExpansion` | `MemoryBudgetPolicy` | `2` / `2` / `1` |
| `SchemaVersion` | `CheckpointSql` | `1` |
| `FileName` | `MemoryFloorStore` | `memory-floor.json` |
| `FrameBatchSize` | `WindowsVideoFrameSampler` | `64` |
| Stop keys | `IngestStopKeys` | `Ingest_StoppedPatchCap`, `Ingest_StoppedMemoryBudget`, `Ingest_SkippedTooLarge`, `Ingest_DeviceTooSmall` |

Resx, both `src/Recall.UI/Resources/Strings.resx` and `src/Recall.UI/Resources/Strings.ar.resx`:

| Key | English | Arabic |
|---|---|---|
| `Ingest_StoppedPatchCap` | Only the first part of this file is searchable. It is too long to index whole. | الجزء الأول فقط من هذا الملف قابل للبحث. الملف طويل جدًا لفهرسته كاملًا. |
| `Ingest_StoppedMemoryBudget` | Indexing paused for this file to keep this device responsive. It resumes when there is room. | توقف فهرسة هذا الملف للحفاظ على استجابة الجهاز. يستأنف عندما تتوفر مساحة. |
| `Ingest_SkippedTooLarge` | This file is too large to index on this device. | هذا الملف أكبر من أن يُفهرس على هذا الجهاز. |
| `Ingest_DeviceTooSmall` | This device does not have enough memory for on-device search. Nothing was indexed. | ذاكرة هذا الجهاز لا تكفي للبحث على الجهاز نفسه. لم تتم فهرسة أي ملف. |

### Tests

| # | Test | Method and branch | Assertion |
|---|---|---|---|
| 1 | `PlanProgressTests.Plan_AdvancesWhenNoSentenceBoundaryExists` | `PlanCore` branch 4 + `OverlapStart` fallback | `Task.Wait(5s)` is `true`; `Patches.Count == 1`; `Patches[0].CharStart == 0`; `Patches[0].CharEnd == 5007`; `Truncated == false` |
| 2 | `PlanProgressTests.Plan_HardCapCutHasNoBackwardOverlap` | `PlanCore` branch 4 on a 20,007-char boundary-less document | `Patches.Count == 3`; `Patches[0].CharEnd == 7681`; `Patches[1].CharStart == 7681`; `Patches[2].CharEnd == 20007` |
| 3 | `PlanProgressTests.Plan_LastPatchEndsAtDocumentLength` | `PlanCore` branch 3 | `Patches[^1].CharEnd == control.Length` |
| 4 | `PlanProgressTests.Plan_StopsAtMaxPatchesPerDocument` | `PlanCore` branch 1 | `Patches.Count == 512`; `Truncated == true` |
| 5 | `PlanProgressTests.Plan_EmptyTokenEnds_ReturnsOnePatch` | `endCount == 0` arm | `Patches.Count == 1`; `Patches[0].CharEnd == 12` |
| 6 | `CheckpointStoreScalingTests.AppendLiveChunks_StoresEveryChunkInOneTransaction` | `AppendLiveChunks` happy path | `ListChunks("/g/a").Count == 200`; `ListLiveAliases(id0).Count == 1` |
| 7 | `CheckpointStoreScalingTests.AppendLiveChunk_AllocationDoesNotGrowWithTable` | `EnsureReady` once-per-store | alloc at 2,000 rows / alloc at 200 rows `< 3.0` |
| 8 | `CheckpointStoreScalingTests.MarkStopped_KeepsChunksLive` | `MarkStopped` | `State == Reading`; `MissingStatusKey == IngestStopKeys.MemoryBudget`; `AnyLivePath(id0)` is `true` |
| 9 | `CheckpointStoreScalingTests.FinishPath_WithStopKey_RecordsNote` | `FinishPath` stop-key arm | `State == Finished`; `MissingStatusKey == IngestStopKeys.PatchCap` |
| 10 | `CheckpointStoreScalingTests.Migration_BackfillsAliasesOnce` | `ApplyMigrations` | `ListLiveAliases(id).Count == 1`; `PRAGMA user_version == 1` |
| 11 | `MemoryBudgetTests.Verdict_BelowMinimumWhenFloorAboveHalf` | `Verdict` below-minimum arm | capacity 4 GiB, floor 2 GiB + 1 byte → `BelowMinimum` |
| 12 | `MemoryBudgetTests.Verdict_TightWhenFloorAboveForty` | `Verdict` tight arm | capacity 8 GiB, floor 3.3 GiB → `Tight` |
| 13 | `MemoryBudgetTests.Verdict_OkWhenFloorBelowForty` | `Verdict` ok arm | capacity 32 GiB, floor 3.3 GiB → `Ok` |
| 14 | `MemoryBudgetTests.Classify_UsesFloorWhenHigherThanWorkingSet` | `Classify` `Math.Max` arm | capacity 8 GiB, ws 0, floor 4 GiB → `Hard` |
| 15 | `MemoryBudgetTests.Classify_At40Percent_IsSoft` | `Classify` soft | capacity 1,000, ws 400, floor 0 → `Soft` |
| 16 | `MemoryBudgetTests.Classify_ZeroCapacity_IsNormal` | `Classify` guard | capacity 0, ws 1, floor 0 → `Normal` |
| 17 | `MemoryBudgetTests.MayLoadFile_RejectsAboveHeadroom` | `MayLoadFile` false arm | capacity 1,000, ws 400, file 101, expansion 1 → `false`; 100 → `true` |
| 18 | `MemoryBudgetTests.MinimumAndRecommended_FollowFloor` | `MinimumSupportedBytes` / `RecommendedBytes` | floor 3 GiB → minimum 6 GiB, recommended 10 GiB |
| 19 | `MemoryGateTests.Open_OnHard_CoolsDownForThirtySeconds` | `Observe` hard arm | after `EnterHard(t0)`: `State == Cooling`; `IsOpen(t0 + 29 s)` is `false`; `IsOpen(t0 + 30 s)` is `true` when pressure is `Normal` |
| 20 | `MemoryGateTests.Cooling_DoublesBackoffWhilePressureStaysHigh` | retry arm | two failed re-probes → next attempt at `+120 s`; `State == Cooling` |
| 21 | `MemoryGateTests.Cooling_DoesNotOpenAtFortyPercent` | hysteresis arm | pressure 40% at `NextAttemptAt` → still `Cooling` |
| 22 | `MemoryGateTests.FileCompleted_ResetsBackoff` | reset arm | after two doubles, `OnFileCompleted()` then hard → next attempt `+30 s` |
| 23 | `MemoryGateTests.Blocked_NeverOpens` | blocked arm | `Block()` then `Observe(Normal, t0 + 1 h)` → `IsOpen` is `false` |
| 24 | `Phase3WatchTests.ScanAsync_StopsWhenGateIsClosed` | `ScanAsync` gate branch | gate `Cooling` → `applier.Calls == 0` |
| 25 | `CodeAndSkipTests.Ingest_HardStop_ReleasesThenResumesWithoutReembed` | `InsertPatchesAsync` gate branch + `MarkStopped` resume | pass 1 `Stop == MemoryBudget`; `reclaimer.Calls == 1`; pass 2 `encoder.Calls == 0`; `AnyLivePath(firstId)` is `true` |
| 26 | `Phase3CursorTests.Ingest_PatchCap_FinishesWithNote` | `PatchCap` arm | `State == Finished`; `MissingStatusKey == IngestStopKeys.PatchCap` |
| 27 | `Phase3CursorTests.LocalOpLogWriter_KeepsTenThousandEntries` | dequeue arm | 10,001 appends → `Entries.Count == 10_000`; `Entries[^1].ContentId == "id-10000"` |
| 28 | `Phase4VisionTests.Index_OversizedVideo_MarksTooLarge` | video guard | `MissingStatusKey == IngestStopKeys.FileTooLarge`; `encoder.Calls == 0` |
| 29 | `Phase5AudioTests.Index_OversizedAudio_MarksTooLarge` | audio guard | `MissingStatusKey == IngestStopKeys.FileTooLarge`; `source.ReadCalls == 0` |
| 30 | `Phase4VisionTests.Starts_CapsAtMaxFrames` | `FrameCadence.Starts` cap | `Starts(10_000).Count == 600`; `Starts(0).Count == 0` |
| 31 | `CloseAddendumGapsTests.Reader_SkipsFileAboveTextCap` | `WalkAsync` oversize arm | one 40 MiB and one small file → one document; `oversize.Paths.Count == 1` |
| 32 | `CloseOpenIngestUiTests.Chrome_DeviceBelowMinimum_IsBlocked` | `IngestChromePolicy.Status` blocked arm | `BelowMinimum` → `IngestChromeStatus.Blocked` |
| 33 | `IngestResourceReclaimerTests.ReleasesLoadedAudioOnly` | `ReleaseOptionalEncoders` both arms | loaded audio → `CloseAudio` called once; unloaded audio → zero calls |
| 34 | `MemoryFloorStoreTests.SaveThenLoad_RoundTrips` | `SaveObserved` + `LoadObserved` | save 3 GiB → `LoadObserved() == 3 GiB` |
| 35 | `MemoryFloorStoreTests.MissingOrCorruptFile_ReturnsZero` | both failure arms | missing → `0`; file containing `"{"` → `0`, no throw |
| 36 | `MemoryBudgetTests.ProcessBudget_UsesLargerOfDeclaredAndObserved` | `EffectiveFloorBytes` | declared 2 GiB, observed 3 GiB → `3 GiB` |
| 37 | `MemoryBudgetTests.ProcessBudget_ObservedBelowDeclared_StaysDeclared` | `Math.Max` arm | declared 2 GiB, observed 1 GiB → `2 GiB` |
| 38 | `Phase3CursorTests.Ingest_RecordsObservedFloorOncePerProcess` | `RecordObserved` guard | two finished files → `RecordObserved` called once; a stopped file → zero calls |
| 39 | `PlanProgressTests.Plan_BoundariesAreStableForTerminatingDocuments` | `PlanCore` unchanged-boundary path, three fixtures | prose, table block, and Arabic fixture each return their recorded `(CharStart, CharEnd)` list exactly |
| 40 | `PlanProgressTests.Plan_ArabicFixture_FirstOverlapIs263` | `OverlapStart` window preserved | the prose fixture's first overlap `== 263` |
| 41 | `MemoryBudgetTests.ReferenceProfiles_MatchPublishedTable` | `Verdict` at the three published classes | floor 3.5 GiB: 4 GiB → `BelowMinimum`; 8 GiB → `Tight`; 16 GiB → `Ok` |

### Do not invent

- No new project, NuGet, solution entry, process, or HTTP client.
- No new cursor state value and no new table. Resume is `Reading` + status key; truncation is `Finished` + status key.
- No `ThrowIfHard` and no exception for a pressure event. The gate returns state.
- No unload of the text encoder, ever, including during cooldown.
- No second patch cap, second ceiling, or per-modality floor.
- Do not keep `CopyChunkPairsToChunkPath` behind a flag; delete it.
- Do not change `n_ctx`, `n_batch`, `n_ubatch`, `n_threads`, the mean-pool path, the stored width, `QuantizeType`, `M`/`efConstruction`/`efSearch`, or the RRF constant.
- Do not add a BenchmarkDotNet project or a CI workflow; M1 is a documented manual measurement.
- Do not add a row to the `docs/plan.md` progress table.
- Do not add a Razor page, toast, or onboarding step; the blocked state renders inside `ResourceStrip`.
- Do not claim a byte figure for the gguf that is not `FileInfo.Length`; the scratch constant stays labelled provisional until M1.
- No Arabic-Indic digits, no `Co-authored-by`, no `git commit --trailer`.

### Docs

| Path | Add or edit | Sentences that change |
|---|---|---|
| `docs/features/ingest-memory-budget.md` | add | The floor formula, the verdict table, 40/50/35 hysteresis, the cooldown and release steps, `minimum = floor/0.5` and `recommended = floor/0.3`, the three-class profile table from §6, the self-tightening floor after the first run, and the rule that a single workstation is a sample and never a reference |
| `mkdocs.yml` | edit | One `Features:` line: `Ingest memory budget: features/ingest-memory-budget.md` after `File size cap` |
| `docs/features/file-size-cap.md` | edit | `Status: implemented (W1-W5)`; the 512-patch cap, the 32 MiB read cap, the 600-frame cap |
| `docs/performance.md` | edit | The 1,057 MB / 5 s and 5.27 MB / 11.9 ms probes, the floor formula, and the M1 table marked provisional |
| `docs/features/text-ingest.md` | edit | A file can stop on the budget or the patch cap; a budget stop cools down and resumes; a cap stop finishes with a note |
| `docs/data/stores.md` | edit | `PRAGMA user_version = 1`, `ix_ingest_chunk_path_path`, one transaction per `AppendLiveChunks` batch |
| `docs/data/oplog.md` | edit | At most 10,000 in-memory entries; the day file is the durable record |
| `docs/adr/0016-ingest-memory-ceiling.md` | add | Floor plus 50% ceiling, the verdict before load, cooldown instead of throw, text encoder never unloaded, Android reads the GC heap limit, the KV-geometry derivation of the scratch constant, the boundary-equivalence argument for W1, the golden-vector prerequisite for native tuning, and the rule that one workstation is a sample |
| `docs/decisions/open.md` | edit | Two follow-up lines under "Do not pick": the fixture, cosine floor, and untuned `n_threads`/`n_ubatch`; and the golden corpus plus query set that a recall@10 run needs |
| `docs/adr/index.md` | edit | `- [ADR 0016: Ingest memory ceiling](0016-ingest-memory-ceiling.md)` after ADR 0015 |

## Implementation status (2 November 2026)

Landed: W1 planner forward progress and the patch cap; W1b call sites; W2 store schema-once, batch writes, `MarkStopped`, stop key; W3 policy, budget, gate, reclaimer, scan/read/patched enforcement, blocked chrome; W3b self-measuring floor; W4 bounded op-log; W5 media guards and frame cap; docs page, ADR 0016, nav, catalog sections.

Verified: production `DocumentPatchPlanner` returns 1 patch in 9 ms for the 5,007 character probe that used to allocate 1,057 MB and never return; the 400-line control keeps its 10 patches.

Test results: unit 942 passed, integration 12 passed, MAUI unit 22 passed.

Still open, in the plan's own terms:

- Service-level tests 17, 18, 25, 26 and the audio guard 29 and reader cap 31 are not written. The mechanics they lock are covered at the store, gate, planner and vision-guard level, but the rows are not there yet.
- M1a and M1b need a real device run, so the published profile table is still derived, not measured.
- The Arabic copy of the four new keys has no native review yet.
- The native encoder is untouched, and the two follow-ups are on `docs/decisions/open.md`.

- The patch-cap note is gone by user decision: a truncated file finishes with no user-facing sentence, the key `Ingest_StoppedPatchCap` was removed from both resx files, and `FinishPath` no longer takes a stop key. User-facing copy now says processing, not indexing or ingestion, in both languages.

- M1a is done and M1b is emulated: the real 295.5 MB model and the native library sit in `tests/Recall.IntegrationTests/TestAssets`, so the floor was measured here (weights 295.5 MB, base 24.3 MB, peak 1,370.6 MB, declared floor 1.81 GB). Device classes were then run through a Windows job-object memory cap that .NET honours as capacity: 2 GB cap reported 1.50 GB and 4 GB cap reported 3.00 GB, both `BelowMinimum` with the encoder never opened; 8 GB cap reported 6.00 GB and opened. The published table in `docs/features/ingest-memory-budget.md` and `docs/performance.md` now carries those measured numbers, so the 4 GB class is blocked and the 8 GB class is `Ok`, not `Tight`.

- Second defect found while probing M1: `recall_embed_token_ends` caps the token map at 7,680 entries, so every document longer than the context produced one final over-context patch and `recall_embed_embed` returned code 4, which the service turned into `MarkMissing` and retried forever. `LlamaTextTokenCounter.TokenEnds` now walks 65,536 character windows, a 32-token `ContextGuardTokens` clamps every cut inside the context, and a 308,006 character file goes from 4 patches with a 56,899-token failure to 31 patches with a largest patch of 2,049 tokens that embeds in 6,980 ms.
