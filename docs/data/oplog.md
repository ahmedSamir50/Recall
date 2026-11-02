Status: in-code

## Write

`LocalOpLogWriter.AppendInsert` rejects a blank content id. It appends an in-memory `LocalOpLogEntry` with `OpLogOperation.Insert`, `Sent` false, and UTC now. A null or white-space directory returns after the memory add and writes no file. Otherwise it creates `oplog` and appends `yyyy-MM-dd.log` with line `{at:o}\t{operation}\t{contentId}`. `AppendDelete` writes `Delete` and does not send it. `Pin` and `Unpin` exist on the enum and have no writer method. `SyncFeature.Enabled` is false. `ISyncAdapter` has no members. The log is not sent.

## Failure

`LocalOpLogWriter.AppendInsert` rejects a blank content id. `AppendDelete` rejects a blank content id.

## In-memory mirror

`LocalOpLogWriter.entries` is a queue bounded at 10,000 entries. When the queue is full the oldest entry is dropped from memory before the new one is appended. The day file under `oplog/` is unaffected and stays the durable record, so a dropped in-memory entry never loses an operation that was written to disk.
