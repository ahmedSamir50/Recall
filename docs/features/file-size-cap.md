Status: open

End-of-v1 study only. No per-file size limit is in the tree, and this page does not name a byte threshold.

## Problem

One granted file can keep producing patches, frames, or windows until ingest finishes the whole file. That can grow working set and stored vectors without a stop tied to file size. A desktop indexing pass showed working set **10,275 MB** then **41,450 MB** while the text meter still read **34/41** finished files. That climb is what the study must bound; it is not a simple per-vector formula.

## What the tree does today

`DocumentPatchPlanner` in Recall.Core cuts one patch at target **2048** tokens, hard-caps one patch at **7680** tokens, and `PlanCore` continues while `startChar < documentLength`, so one file has no patch-count stop. Task **5.3.1** in [plan.md](../plan.md) already states there is no 10-page stop on PDF text-layer ingest.

Stored vectors:

| Kind | Width | Bytes |
|---|---|---|
| Text (Matryoshka stored) | 512 floats | 2,048 |
| Image, video frame, audio window | 768 floats | 3,072 |

Each text patch also stores `raw_text` for the slice embedded.

## What a later change would do

Pick **one stop per file** for plain text, PDF, image, video, and audio. When a file passes the limit, the rest of **that file** is not embedded and is not written into the vector collection. The limit itself is **not** chosen on this page.

The study must derive the limit from at least one measured file: bytes on disk, patch or frame or window count, and stored vector bytes plus raw text, so a single file cannot keep adding vectors indefinitely.

## Out of scope for this study page

No implementation, no test that encodes a numeric cap, and no new row on the [plan.md](../plan.md) progress table. Choosing the limit and shipping the stop belong in a later accepted plan.
