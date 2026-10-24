Status: in-code. Production shrink stays at 768 until a recall corpus ships.

## Choice

The person can set only `AgeCutoffYears.Two`, `Five`, or `Seven`. The stored default is 2. Another number throws `RecallFault.AgeCutoffNotAllowed`. Changing the choice calls `AgeCutoffActions.Set`, which stores the years and runs `AgeMoveService.Sweep`. Text never ages; `AgeCutGate.Passes` and `IAgeCutDecision.TryPass` throw `RecallFault.AgeCutTextForbidden` on text.

## Shrink

File age is UTC last-write time. A missing time does not move a row. A younger time does not regrow a short row; only pin regrow does. When a picture, video, or audio file is past the cutoff, `AgeMoveService` cuts its stored 768 vector to 512 only after `IAgeCutDecision.TryPass` for that modality. Production registers `FailClosedAgeCutDecision`, and `AgeCutCorpus.TryLoad` returns false until a real corpus exists, so no row is cut on device today. The gate logic and move order (short insert, `MoveLiveAliases`, long delete, `age_cut_pending` resume) are in the tree and locked by unit tests. Picture, video, and audio each have a matching `_old` collection at width 512.

## Pin

`PinService` stores pins in `ingest_pin`, can regrow a short row to 768 when the file is present, and can cut back on unpin when the file is old and the gate passes. Unpin of an old file returns `PinResult.GateFailedStaysLong` when the gate fails closed. Local op-log lines use `OpLogOperation.Pin` and `Unpin`. The Pin route is an empty state only; Find has no pin control yet.
