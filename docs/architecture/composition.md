Status: in-code

## Registrations

`AddRecallCore` registers singletons `ThemeService`, `LocaleService`, `AgeCutoffPreferenceService`, `IScopeManagerService`/`ScopeGrantService`, `IIndexPolicy`/`RecallIndexPolicy`, `LocalOpLogWriter`, `IRandomDeviceIdSource`/`GuidRandomDeviceIdSource`, `IDeviceRecordService`/`DeviceRecordService`, `IGrantedTextReader`/`DirectoryGrantedTextReader`, `ITextSearchService`/`TextSearchService`, `TextIngestEngine`, `IGrantedTextIngestService`/`GrantedTextIngestService`, `ScopeChangeApplier`, `ForegroundScanService`, `IIndexNowService`/`IndexNowService`, `ScopeWatchCoordinator`, and `DisabledScopeWatcher`. Do not construct SQLite here.

`AddRecallUi` registers `ShellUiStore`.

`AddRecallAdapters` registers `TextModelRuntime`, `ITextEmbeddingEncoder`/`LlamaTextEmbeddingEncoderLazy`, `ITextTokenCounter`/`LazyTextTokenCounter`, and `ITextVectorIndex`/`ZVecTextVectorIndex`. The text index is constructed with `IIndexPolicy` and `IIngestCursorStore`.

`AddRecallMauiPlatform` runs after `AddRecallCore` and registers `IGrantedTextReader`/`MauiGrantedTextReader`, `ILocalFileOpener`/`MauiLocalFileOpener`, `IModelDownloadService`/`MauiModelDownloadService`, `IIngestCursorStore`/`SqliteIngestCursorStore` at `Path.Combine(FileSystem.AppDataDirectory, CheckpointSql.FileName)`, `WindowsFileScopeWatcher` and `IFileScopeWatcherService` via `ScopeWatcherKindPolicy.Choose`, then calls `AddRecallAdapters`. The host resolves the later `IGrantedTextReader`. `AddRecallAdapters` throws if `modelDirectory` or `vectorDatabaseRoot` is null or white space. Paths: model `FileSystem.AppDataDirectory` plus `models/text`; vectors plus `zvec`; op-log plus `oplog`; SQLite plus `checkpoints.db` under app data. `MauiProgram` does not call `AddMudServices`.
