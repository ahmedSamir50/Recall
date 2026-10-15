Status: in-code

## Registrations

`AddRecallCore` registers singletons `ThemeService`, `LocaleService`, `AgeCutoffPreferenceService`, `IScopeManagerService`/`ScopeGrantService`, `IIndexPolicy`/`RecallIndexPolicy`, `LocalOpLogWriter`, `IRandomDeviceIdSource`/`GuidRandomDeviceIdSource`, `IDeviceRecordService`/`DeviceRecordService`, `IGrantedTextReader`/`DirectoryGrantedTextReader`, `ITextSearchService`/`TextSearchService`, `TextIngestEngine`, `IGrantedTextIngestService`/`GrantedTextIngestService`, and `ShellUiStore`.

`AddRecallUi` registers `ShellUiStore`.

`AddRecallAdapters` registers `TextModelRuntime`, `ITextEmbeddingEncoder`/`LazyOnnxTextEmbeddingEncoder`, `ITextTokenCounter`/`LazyTextTokenCounter`, and `ITextVectorIndex`/`ZVecTextVectorIndex`.

`AddRecallMauiPlatform` runs after `AddRecallCore` and registers `IGrantedTextReader`/`MauiGrantedTextReader`, `ILocalFileOpener`/`MauiLocalFileOpener`, `IModelDownloadService`/`MauiModelDownloadService`, and the platform stores, then calls `AddRecallAdapters`. The host resolves the later `IGrantedTextReader`. `AddRecallAdapters` throws if `modelDirectory` or `vectorDatabaseRoot` is null or white space. Paths: model `FileSystem.AppDataDirectory` plus `models/text`; vectors plus `zvec`; op-log plus `oplog`. `MauiProgram` does not call `AddMudServices`.
