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
