# Ingest memory budget

Status: implemented, 2 November 2026.

## The rule

No ingest path may use half the machine. The ceiling is `HardPercent = 50` of what the runtime reports as available memory, measured against a floor that is the cost of having the app open. Ingest never chases a percentage of zero, because a percentage of zero is how a 2 GB device reaches ninety-nine percent before the first file finishes.

## The floor

`FloorBytes = weights + compute scratch + reserve + framework base`.

- Weights are the encoder file's length on disk, read at runtime.
- Compute scratch is an upper bound from context geometry: the text context is 7680 tokens over 768 dimensions and 18 layers, so a float16 K and V cache is about 425 MB, and the declared 1.25 GiB adds activation and graph headroom.
- The reserve is 256 MiB.
- The framework base is the working set sampled before the encoder opens.

On the first run the declared floor decides. After the first finished file the process records what it actually cost in `memory-floor.json`, and every later run uses the larger of the two. The gate only ever tightens.

## Device verdict, before the model loads

| Verdict | Condition | What happens |
|---|---|---|
| `BelowMinimum` | floor above 50% of capacity | the encoder is not opened, ingest is refused, the strip says the device is below minimum and offers no retry |
| `Tight` | floor above 40% | one file at a time, media skipped |
| `Ok` | otherwise | normal ingest |

The published classes, measured on 2 November 2026 with the real encoder and a job-object memory cap (the cap is what the process reports as capacity, not what the machine has):

| Class | Reported capacity | Floor | Verdict | Behaviour |
|---|---|---|---|---|
| small | 1.50 GB (2 GB cap) | 1.81 GB | `BelowMinimum` | blocked, encoder never opened |
| small | 3.00 GB (4 GB cap) | 1.81 GB | `BelowMinimum` | blocked, encoder never opened |
| mid | 6.00 GB (8 GB cap) | 1.81 GB | `Ok` | encoder opened |
| normal | 63.69 GB (this workstation) | 1.81 GB | `Ok` | encoder opened |

So a 4 GB device is blocked, an 8 GB device runs, and `minimum = floor / 0.5` lands at 3.6 GB of reported capacity while `recommended = floor / 0.3` lands at 6.0 GB. A single workstation is a sample, not a reference: only the framework base and the observed delta are per device, and a device the app has never seen computes its own floor on its first run.

A full multi-patch file measured 2,708.8 MB of working set, so the observed floor after one real file is about 2.68 GB rather than 1.81 GB. The table above is the first-run verdict; a device that records the larger observation moves one class tighter, which is why an 8 GB device settles at `Tight`.

## Pressure and cooldown

| Pressure | Condition | Behaviour |
|---|---|---|
| `Normal` | below 40% | index normally |
| `Soft` | 40% to 50% | finish the file in hand, start no new file |
| `Hard` | 50% or above | stop at the next patch boundary, cool down |

A hard reading does not throw. The pass returns, the gate enters `Cooling` for thirty seconds, and the existing scheduler re-triggers it. Resume needs the pressure at or below 35%, which is why the gate cannot flap. A failed re-probe doubles the backoff to a maximum of five minutes, and a finished file resets it.

## Releasing what is held

In order: flush the pending chunk batch, drop the document string, the token ends, the patch plan and the chunk list, close the audio encoder only if it is loaded, never unload the text encoder, compact the large object heap once when a dropped buffer was above 8 MB, then re-probe.

The text encoder stays resident. Unloading it to make a working-set number look better costs more than the cooldown saves, and canon forbids it.

## Stops a file can carry

| Key | Meaning |
|---|---|
| `Ingest_StoppedMemoryBudget` | the gate closed mid-file; the file stays `Reading` and resumes |
| `Ingest_SkippedTooLarge` | the file is above the read cap (32 MiB of text) or above the headroom |
| `Ingest_DeviceTooSmall` | the device is below minimum; nothing was processed |

A memory stop leaves the stored chunks live, so the next pass skips them instead of embedding again. A cap stop is not shown to the user: the file finishes and its first part is searchable, and the note would repeat on every attempt with nothing the person could do about it.

## Related

- [File size cap](file-size-cap.md) for the per-document stop.
- [Performance](../performance.md) for the measured probes.
- [ADR 0016](../adr/0016-ingest-memory-ceiling.md) for the decision and its limits.
