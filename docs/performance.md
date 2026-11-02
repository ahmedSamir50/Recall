Status: open for the ship gate. The desktop row is recorded.

## Two sources

Plan task 2.2.5 names insert ≤5 ms, hybrid top-20 ≤200 ms mobile and ≤25 ms desktop, encoder load ≤1000 ms mobile and ≤800 ms desktop, and says encoder load is not added into query time.

The recorded desktop run is `InsertOne` mean 30.66 us, `HybridTop20` mean 2,202.43 us, and `LoadTextEncoder` mean 727.3 ms.

Task 2.2.5 is done on that desktop run. Task A.2.9 is done on adb serial `emulator-5554` (1 GB RAM): `Host: emulator`, `HybridTop20 mean 135.37 ms`, `LoadTextEncoder mean 886.14 ms`. The mobile lines are 200 ms and 1000 ms. Recall.Benchmarks is a Windows net10.0 executable. An emulator label is not a phone number.

The performance skill table names a different query p95, ≤50 ms mobile and ≤20 ms desktop at 100k items, and a different desktop insert, ≤2 ms.

That skill table is not the 2.2.5 run, and this page does not choose between them.

U1 has no ship disk number. The old 80 MB and 150 MB figures are not targets.

U2 has no ship RSS number. The old 400 MB warmup figure is not a target. Float16 inference is forbidden. The int8 text file is about 314 MB on disk, and that is not an RSS budget.

Do not enable `QuantizeType` to chase a size. Turning it on builds a second index and does not shrink the file.

## Ingest memory, measured 2 November 2026

| Probe | Input | Result |
|---|---|---|
| Planner, one sentence then a boundary-free body | 5,007 characters | dead loop before the fix; 1,057 MB allocated in five seconds; one patch after the fix in 4 ms |
| Planner, boundary-free body | 20,007 characters | 3 patches at the 7680 hard cap, zero backward overlap |
| Store append, one row | 200 / 1,000 / 3,000 / 7,000 / 11,000 rows | 0.11 / 0.48 / 1.40 / 3.16 / 5.27 MB allocated per call before the fix |
| Planner, 1,328,889 characters | 600 sentences | 512 patches, truncated, 27 ms |

The compiled-for-Release scratch project that produced these numbers lives outside the repository. The 64 GB machine is a sample, not a reference: the ceiling is computed per device from the formula on [Ingest memory budget](../features/ingest-memory-budget.md).

## Encoder floor, measured 2 November 2026

The probe opens `embeddinggemma-2-Q8_0.gguf` through `TextModelRuntime` and embeds a ~900-token text eight times. Run on this workstation, and then in the same probe under a Windows job-object memory cap, which .NET honours as the process memory limit, so the reported capacity is the constrained one.

| Measure | Value |
|---|---|
| Weights on disk | 295.5 MB |
| Framework base before the model | 24.3 MB |
| Declared floor (weights + 1.25 GiB scratch + 256 MiB reserve + base) | 1.81 GB |
| Working set after open | 618.5 MB |
| Peak working set during embedding | 1,370.6 MB |
| Measured cost above base | 1,346.2 MB |
| llama.cpp compute buffers | 1,125.18 MiB device + 285.23 MiB host |
| First embed, ~900 tokens | 151 ms |
| Warm embed, ~900 tokens | 35-46 ms |

| Job cap | Reported capacity | Verdict | Encoder opened |
|---|---|---|---|
| 2 GB | 1.50 GB | `BelowMinimum` | no |
| 4 GB | 3.00 GB | `BelowMinimum` | no |
| 8 GB | 6.00 GB | `Ok` | yes |

The warm embed is 35-46 ms on the Vulkan backend for a ~900-token text; the desktop cell of 8 ms is a shorter text and is not comparable. The cap runs prove the gate refuses a small device before the encoder is opened, instead of loading and then thrashing.

## Token map and a full multi-patch file, measured 2 November 2026

`recall_embed_token_ends` caps its output at `RECALL_EMBED_MAX_TOKENS`, so a document longer than the context came back with a token map of exactly 7,680 entries. The planner read that as the end of the document and emitted one final patch holding every remaining character: a 308,006 character file produced 4 patches, the last of which tokenized to 56,899 tokens, and `recall_embed_embed` returned code 4. `LlamaTextTokenCounter.TokenEnds` now walks the text in 65,536 character windows so the map always covers the document.

| Measure | Before | After |
|---|---|---|
| Token ends for a 308,006 character file | 7,680 | 63,006 |
| Patches | 4 | 31 |
| Largest patch | 56,899 tokens, embed failed with code 4 | 2,049 tokens |
| Embedding the whole file | fails | 6,980 ms for 31 patches |
| Peak working set during the run | not reached | 2,708.8 MB |

The single-embed peak above is 1,370.6 MB; a full 31-patch file peaked at 2,708.8 MB because the working set keeps pages the OS has not trimmed yet. The gate uses the larger of the declared floor and the recorded observation, so a device tightens itself after its first real file. The 32-token context guard keeps every patch, plus its title prefix, inside the 7,680-token context.
