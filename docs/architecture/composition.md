Status: in-code

## Who is registered, and why

Startup order and the headless-default rule are in [How Recall is put together](design.md).

`AddRecallCore` registers the rules with implementations that need no phone: theme, locale, age, grants, index policy, device record, text search, text ingest, the watch coordinator, Index now, a directory file reader, a memory skip list, and an idle detector that always reports idle. It does not open SQLite and it does not open a model.

`AddRecallUi` registers `ShellUiStore`, the single UI snapshot.

`AddRecallMauiPlatform` runs after Core. It registers the real preference stores, the folder picker, the device id and OS name, the granted-folder reader, the file opener, the content probe, the idle detector, the HTTP download, the SQLite checkpoint file under app data, and the scope watcher. A later registration of the same service replaces the headless one. The platform method then calls `AddRecallAdapters`.

`AddRecallAdapters` registers the text runtime, the lazy text encoder and token counter, `ZVecTextVectorIndex`, the vision runtime, the audio runtime, `ZVecMediaVectorIndex`, and picture search. A blank model directory or a blank vector directory is rejected.

Paths under app data are `models/text` for weights, `zvec` for the vector databases, `oplog` for the local log, and `checkpoints.db` for cursors. `MauiProgram` does not call `AddMudServices`.
