Status: in-code

# What each piece is for

Read [How Recall is put together](design.md) first. This page names the types and says the job each one does. The [catalog](../code/map.md) quotes the branches. A sentence here is the reason the type exists.

## Recall.Core

Core is the rulebook. Folders below match the folders in `src/Recall.Core`.

### Vocabulary and faults

`Modality` is text, image, video, or audio. `StoredWidth` is 768, 512, or 256. `AgeCutoffYears` is 2, 5, or 7. `OpLogOperation` is insert, delete, pin, or unpin. These enums are the only spellings of those ideas.

`RecallFault` is the closed list of product failures, such as a blocked width, a prefix applied twice, or a rank constant other than 60. `RecallFaultException` carries one of those faults so a caller can tell a rule failure from a bug.

### Ports

A port is a promise Core makes and another project keeps.

`IIndexPolicy` exposes the text stored width and rejects a width the product forbids. `ITextEmbeddingEncoder` turns already-prefixed text into a vector. `ITextTokenCounter` counts tokens so the chunker can stay inside 8192. `ITextVectorIndex` inserts, deletes, reconciles, and searches text rows. `IGrantedTextIngestService` walks grants and documents. `IIndexNowService` lists unfinished grants and indexes one path. `ITextSearchService` answers Find. `ILocalFileOpener` checks that a hit still exists and opens it.

`IFileScopeWatcherService` starts and stops watching. `IScopeChangeSink` receives one change or a request to rescan. `IScopeManagerService` lists, adds, removes, and retargets grants. `IScopeGrantStore` persists them. `IFolderPickerService` returns selected, cancelled, or denied. `IGrantedFolderAccessService` restores a platform bookmark. `IVideoFrameSamplerService` turns a media stream into JPEGs two seconds apart. `IIdleDetectorService` reports idle or busy.

`IMediaVectorIndex`, `IVisionEmbeddingEncoder`, `IAudioEmbeddingEncoder`, `IPictureSearchService`, and `IAudioSearchService` are the picture, video, and audio promises. `IAudioFileIngest`, `IAudioSource`, `ISiblingFiles`, and `IAudioOffReport` are the audio-ingest promises. `DisabledAudioFileIngest` is the do-nothing ingest. `SiblingFile` is one caption found beside a media file. `IModelDownloadService` is the download promise, including the vision and audio files. `IEncoderFileHash` is the SHA-256 of the weight file that wrote a vector. `ILocalContentProbe` says whether a file is actually on this device. `ILocalFileOpener` is the opener Home calls. `IOpenIndexedFileService` is a second open port and has no MAUI implementation. `ISecureStorageService` and `ISyncAdapter` are empty ports. `IDebugAppReset` clears local data when the build enables it.

`IRandomDeviceIdSource` and `IDeviceIdCandidateProvider` feed device identity. `IDeviceRecordService`, `IDeviceRecordStore`, and `IDeviceOsNameProvider` are the device record. `IOnboardingCompletionStore` remembers that first-run finished. `IThemePreferenceStore`, `IUiLocaleStore`, `IAgeCutoffPreferenceStore`, `IUserSkipStore`, `ISystemThemeProvider`, and `IRecallShellKind` are the remaining preference and shell seams. MAUI implements them with the platform preference store.

### Scope

Scope answers one question: which folders and which kinds of file may be read.

`ScopeFolderGrant` is one allowed folder. `GrantFileKinds` is the flag set on that folder. `ScopeGrantService` is the service the UI calls. `ScopeGrantPolicy` normalizes paths and decides parent, child, and overlap. `ScopeGrantAddResult` is duplicate, child-of-parent, parent-of-child, or added. `GrantMembership` decides whether a path sits inside a grant.

`FileAdmission` is the gate in front of a single path. `BuiltInSkips` is the list the person can see and cannot remove: dependency folders, secret names, databases, and lockfiles. `UserSkipService` adds and removes only the extra names the person typed. `MemoryUserSkipStore` is the headless store. `GitIgnoreMatcher` applies `.gitignore` and `.git/info/exclude` between the file and the grant root. A secret, database, lockfile, or skip-folder still loses to a gitignore exception.

`GrantTextFiles` and `MediaFileKinds` know which extensions count as text, code, image, video, or audio. `DatasetFolderSkip` refuses a nested folder that looks like a dumped dataset. `DatasetSkipReport` remembers that refusal so Home can say so. `LocalContentPolicy` refuses a file that is not stored on this device. `WindowsCloudAttributes`, `AppleICloudPresence`, and `AndroidDocumentPresence` are the platform clues that policy reads. `RemoteFileSkipReport` remembers those skips.

`ScopeWatcherKindPolicy` selects the filesystem watcher for Windows, Android, and Mac Catalyst, and the disabled watcher for iOS. The selected watcher class for those three is `WindowsFileScopeWatcher`. `DisabledScopeWatcher` is the iOS stand-in. `WindowsWatchEventMapper` turns a raw watch event into a `ScopeChange`. `ScopeWatchRoots` chooses the directories to subscribe. `ScopeWatchLimits` bounds how much one burst may hold. `ScopeChangeBuffer` coalesces that burst. `ForegroundGrantDiff` compares the files on disk with the checkpoint rows. `ContentUri` recognizes an Android `content://` grant.

### Ingest

Ingest is the walk from a grant to a call on the text engine.

`GrantedTextIngestService` skips a grant unless text or code is on, asks the reader for documents, and asks the engine to ingest each one. `IGrantedTextReader` and `GrantedTextDocument` are that stream. `DirectoryGrantedTextReader` is the headless reader over a normal directory. `FilesystemScopeFileReader` applies admission while it walks. `TextModelReadyIngest` starts that walk once the text weight is ready.

`ScopeWatchCoordinator` is the sink the watcher reports to. It owns the buffer and the lifetime of the subscription. `ScopeChangeApplier` turns one change into an ingest, a rename, a missing mark, or a retire. `ForegroundScanService` is the iOS-style catch-up. `IndexNowService` lists grants that are not finished and ingests the one the person asked for. `IndexNowOfferPolicy` decides when Home should offer that button.

`IdleIngestGate` defers work while the device is busy. `DisabledIdleDetectorService` is the headless idle signal, always idle. `LocalOnlyContentProbe` is the headless probe.

`FrameCadence` is the two-second timetable for video frames. Windows thumbnails require OS build 22000. `AudioWindows` and `AudioWindow` plan the overlapping audio slices. `AudioResample` reaches 16000 Hz. `WavPcm` reads WAV bytes. `AudioDecoders` holds the failure sentence the platform decoders share. `SiblingTranscript` pulls cue text out of a caption file that shares the media name. It does not read an embedded track. `AudioFileIngest` is the MAUI implementation of that walk: WAV or platform PCM, one vector per window, sibling caption words on the row, then `CloseAudio`. `AudioOffReport` remembers that a file was skipped because the audio kind was off. `DisabledAudioFileIngest` is the headless stand-in and does not read.

### Text

Text is how a string becomes chunk ids. It does not own the ZVec collection.

`EmbeddingTaskPrefixes` wraps queries and titled documents, and refuses a second wrap. `TextChunkPlanner` and `TextChunkPiece` split on the token budget and stay on UTF-16 boundaries. `ContentIdBuilder` builds the id and the source hash. `TextIngestEngine` runs one file: width check, hash, chunks, embed, cut, insert, op-log. `TextIngestChunk` and `TextIngestResult` are what that call returns. `TextIngestOptions` and `GrantTextFiles.Use` install the extension list from settings.

`NumericSpanPolicy` sends a number-heavy span to keywords only. `KeywordFold` builds the BM25 key: NFKC, Arabic tatweel and diacritics removed, alef forms folded. `WidthOnEdit` always stores text at 512. For image, video, and audio it keeps 768 until the file is past the age cutoff, and a pin on a 512-wide vector grows it back to 768. No media ingest job calls it yet. `EncoderRepair` decides that a new encoder hash must re-embed. `PdfSlicePolicy` ends a PDF slice at 8192 tokens or 10 pages. `TextFileSignature` samples the first bytes and rejects binaries, NUL bytes, and script shebangs.

### Search

Search answers a question. It does not choose HNSW parameters.

`TextSearchService` embeds the question once and asks the text index. `TextSearchFilter` builds the grant, liveness, and encoder-hash filter the index applies inside the query. `TextSearchModels` holds the insert, the request, the hit, and the live-state record used when a pending reconcile runs. `HybridFusion` is reciprocal rank fusion at 60, used when two lists are fused in process. The live text query fuses inside ZVec with that same constant. `TextVectorIndexGuard` refuses a hybrid query that is not using 60, and refuses a text store that is not 512. `RecallIndexCaps` is 64 raw hits and 20 files. `Collapse` keeps one card per file, using the earliest start for frames. `SearchHitMarks` drops a retired chunk and a hit outside the grant, copies the checkpoint path onto the hit, and expands a shared content id to each live path. `TextMatchReason` has one value, dense and full text together. `LocalFileOpenPolicy` maps an activate into opened, missing, or failed.

`PictureSearchService` embeds one JPEG and searches the image and video collections. `PictureQueryGate` and `PictureQueryDecision` say whether that search may run, must wait for the download, or has failed. `AudioSearchService` embeds one PCM buffer and searches `items_audio` inside grants that have audio on. `AudioQueryGate` reuses the picture gate's decision for the audio download phase. `MediaSearchHit` and `MediaSearchRequest` are the media query shapes.

### Indexing, vectors, checkpoints

`RecallIndexPolicy` stores text at 512, blocks 128 for every modality, and publishes the HNSW settings: M 16, efConstruction 200, efSearch 64. Quantization stays off, so the index is float32. `TextIndexOpenPolicy` creates a missing collection, opens one that already has the `fts_key` field, and replaces a collection whose schema is missing that field. `MediaCollections` names `items_image`, `items_video`, and `items_audio` and fixes their width at 768. `MediaIndexInsert` is the row shape for a picture, a video frame, or an audio window. `AudioFileIngest` calls the audio-window shape. Nothing in `src/` calls the picture or video-frame shapes.

`MatryoshkaVectorMath` L2-normalizes a vector and cuts the native 768 down to the stored width, then normalizes again. `TokenMeanPool` is the mean-pool helper for a token matrix.

`IIngestCursorStore` is the checkpoint port. `SqliteIngestCursorStore` is the SQLite implementation of that port. It lives in Core because the SQL is a product rule, and the MAUI host only chooses the file path. `CheckpointSql` is the schema. `CheckpointLease` is the write transaction. `IngestCursorState` is reading, finished, or missing. `IngestCursorPolicy` returns read or skip. A finished file with the same content hash is a skip. A new file, a file left in reading, a missing file, and a changed hash are a read. The row records are `IngestFileRow`, `IngestChunkRow`, `IngestAliasRow`, `Bm25OnlyRow`, `ChunkWrite`, and `GrantScanRow`.

### Download, device, preferences, theme, locale, sync, catalog

`TextModelDownloadOptions`, `VisionModelDownloadOptions`, and `AudioModelDownloadOptions` are the file name, URL, and hash for each weight. `ModelHashVerifier` checks the bytes. `DownloadTransfer` places a partial body. `TextModelDownloadUiPolicy` maps a phase to the row the screen shows. `AudioModelDownloadOptions` defaults to the vision mmproj file and the vision directory. `AudioDownloadJoin` and `AudioDownloadJoinPolicy` look at the vision download phase and whether that file already matches its hash, then return use-ready, wait, or start-vision. The audio download method follows that decision and does not request its own URL. `DefaultEncoderFileHash` holds the hashes the index writes onto rows. `ModelCatalog` is the list of model ids the settings name.

`DeviceRecordService` stores the platform device id, the original OS name, and the display name the person can edit. `DeviceIdRules` keeps a ready platform id. A blank id or the known-bad Android id uses `GuidRandomDeviceIdSource`. A candidate that is not ready throws `DeviceIdNotReadyException`. `DevicePlatformIds` holds that known-bad Android id and the Windows machine-guid registry location. `DeviceInitials` and `DisplayNameSetResult` support the rename screen. `DeviceRecordState` is the stored record.

`ThemeService` resolves System, Dark, and Light. `ThemePreference` and `ThemeResolution` are that choice and the concrete theme after System is resolved. `LocaleService` switches Arabic and English and applies the culture. `UiLocale` and `UiLanguage` are that choice. The retrieval golden languages are a different list and are not this service.

`AgeCutoffPreferenceService` stores 2, 5, or 7. It does not move a vector.

`SyncFeature` is the constant false. `LocalOpLogWriter` and `LocalOpLogEntry` append the local log. `ISyncAdapter` has no methods.

`OcrModelFiles`, `OcrReaderGate`, `OcrLineChoice`, `BrokenArabic`, and `OcrSessionSet` are the OCR decisions that exist without an ONNX reader. See the design page.

`RecallCoreServiceCollectionExtensions.AddRecallCore` is the headless registration method.

## Recall.Adapters

Adapters keep llama.cpp and ZVec behind the ports.

`RecallEmbedNative` is the P/Invoke surface to `recall_embed`. `IRecallEmbedLibrary` is the seam tests replace. `RecallEmbedResults` maps native status codes. `LlamaTextEmbeddingEncoder` embeds prefixed text through that library. `LlamaTextTokenCounter` counts tokens through the same library. `TextModelRuntime` opens the text GGUF once, builds those two objects, and closes the native context on dispose. `LlamaTextEmbeddingEncoderLazy` and `LazyTextTokenCounter` wait until the first call so startup does not require the file. `VisionModelRuntime` loads the mmproj for one JPEG and closes it before returning. `AudioModelRuntime` embeds mono 16 kHz PCM through the native audio entry. `EmbedPcm` leaves the context open. `CloseAudio` closes it. `MediaContextPolicy.OneContext` tells the audio runtime to skip its own open when a context is already held.

`ZVecTextVectorIndex` owns `items_text`: create or replace on open, insert, delete, reconcile, and hybrid query. `ZVecMediaVectorIndex` owns the three media collections. `WindowsVideoFrameSampler` is the Windows sampler that throws because no frame package is linked. The working samplers are in the MAUI project.

`AdapterServiceCollectionExtensions.AddRecallAdapters` registers this set.

## Recall.UI

The UI starts use cases. It does not own a stored width or a prefix.

`Routes` is the Blazor router. `AppRoutes` names the onboarding paths. `ShellUiStore` and `ShellUiSnapshot` are the one reactive snapshot. `UiStoreComponentBase` subscribes a page to it. `ShellLayoutBase`, `AppShell`, `MainLayout`, `OnboardingLayout`, and `LandingLayout` are the frames. `BootstrapRedirect` sends an unfinished onboarding to the right step. `ShellRouteSync` writes the URL into the snapshot. `ShellNavLink` and `ShellTabLink` are the navigation controls.

`Home` is Find. `TextFindSession` decides whether a query is worth sending, builds the first five cards, and maps a failure to a string key. `IndexNowPanel` is the offer on that page. `Lattice` is the sync-off page. `Pin` is the empty pin page. `Folders`, `Models`, `Preferences`, and `Device` are the library pages. `Welcome`, `Folders`, `Models`, and `Ready` under onboarding are the first-run path. `Landing` and `ShellPage` are the entry redirects.

`FolderGrantPanel` and `FolderGrantMessages` talk to the grant service and translate its result into Arabic and English. `ModelRow` is one download row. A text row with auto-start calls the text download. Image and audio rows do not auto-start. `ThemeLanguageToggles` cycles theme and locale. `RecallText` loads the Arabic and English strings. `UiCulture` applies layout direction. `RecallComponentBase` is the common component base. `InfinityMark`, `RecallIcon`, `LatticeBackground`, and `PrivacyPill` are chrome. `RecallUiServiceCollectionExtensions` registers the store.

## Recall.Maui

Maui is the composition root and the operating-system implementations.

`MauiProgram` builds the host, as the design page describes. `MauiServiceCollectionExtensions.AddRecallMauiPlatform` registers the platform services and then the adapters. `TextModelConfiguration` reads the settings sections.

`MauiModelDownloadService` performs the hash-checked download for the text file and the vision file. The audio phase follows the vision snapshot. `MauiGrantedTextReader` is the real granted-folder reader, with desktop, Apple, and Android parts. `MauiFolderPickerService` and `AndroidFolderPickerCoordinator` pick a folder. `MauiGrantedFolderAccessService` restores access, with Windows, iOS, and Android parts. `MauiLocalFileOpener` opens a hit. `MauiLocalContentProbe` answers the cloud-file question, with a Windows part and a default part. `MauiIdleDetector` is the real idle signal.

`MauiThemePreferenceStore`, `MauiUiLocaleStore`, `MauiAgeCutoffPreferenceStore`, `MauiScopeGrantStore`, `MauiDeviceRecordStore`, `MauiUserSkipStore`, `MauiOnboardingCompletionStore`, and `MauiSystemThemeProvider` are the preference implementations. `MauiDeviceIdCandidateProvider` and `MauiDeviceOsNameProvider` read the platform id and the OS name, each with a per-OS part. `MauiRecallShellKind` reports which shell is running so the watcher policy can choose.

`WindowsAudioSource`, `AndroidAudioSource`, and `AppleAudioSource` read a media file and decode compressed audio to PCM. `MauiSiblingFiles` lists caption files beside that media. `AndroidVideoFrameSampler` and `AppleVideoFrameSampler` emit the two-second JPEGs. `MauiDebugAppReset` clears local data in a debug build. `DebugAppResetDisabled` is the release stand-in. `App`, `MainPage`, and the platform `MainActivity`, `AppDelegate`, and Windows `App` classes are the MAUI entry points.

## Tests, benchmarks, and tools

`Recall.UnitTests` locks the rules: grants, cursors, watchers, prefixes, fusion, download phases, vision and audio decisions, locale parity, and the shape of the solution. `PortFakes`, `PreferenceFakes`, and `DeviceFakes` stand in for ports. `DocsSiteTests` checks that the site builds a complete catalog. That catalog is allowed to quote code. It is not a substitute for this page.

`Recall.IntegrationTests` runs the text index and the embedder against real libraries. `Recall.Benchmarks` and `TextBenchmarkHost` are the measurement host. `Recall.BenchTests` checks that host. `corpus-builder` writes corpus files. `mrl-eval` compares a cut vector with a full vector for the Matryoshka check.
