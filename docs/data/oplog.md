Status: in-code

## Write

`LocalOpLogWriter.AppendInsert` rejects a blank content id. It appends an in-memory `LocalOpLogEntry` with `OpLogOperation.Insert`, `Sent` false, and UTC now. A null or white-space directory returns after the memory add and writes no file. Otherwise it creates `oplog` and appends `yyyy-MM-dd.log` with line `{at:o}\t{operation}\t{contentId}`. `Delete`, `Pin`, and `Unpin` exist on the enum and have no writer method. `SyncFeature.Enabled` is false. `ISyncAdapter` has no members. The log is not sent.

## Failure

`LocalOpLogWriter.AppendInsert` rejects a blank content id.
