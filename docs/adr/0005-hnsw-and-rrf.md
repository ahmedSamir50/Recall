Status: accepted, 15 Oct 2026.

Context: index compression and fusion had competing notes.

Decision: plain HNSW, `QuantizeType` left `Undefined`, `M` 16, `EfConstruction` 200, query `EfSearch` 64, RRF rank constant 60. Do not construct `ZVecHnswRabitqIndexParam`. DiskANN is out.

Consequences: a quantized second index waits for a benchmark that shows query time about 2× or 3× faster, and that benchmark has not passed.
