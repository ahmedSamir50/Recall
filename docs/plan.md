Status: semi-final plan, 13 Oct 2026. InitDocs/ is history and is not this plan.

The code map is [index.md](index.md). This file remains the only progress table.

# Recall plan

Build order is phases. Each phase is a set of epics. Each epic is a set of tasks. A task is done when its check is true. A phase is done when its epics are done.

**Status signals.** Every phase, epic, and task carries one of these words. Nothing else counts as a status.

| Signal | Means |
|---|---|
| `done` | The "Done when" check is true. |
| `partial` | Some of that check is in the tree. The sentence after the check says what is left. |
| `not-started` | No code meets that check. |

A parent is `done` only when every child is `done`. A parent is `not-started` only when every child is `not-started`. Any mix is `partial`.

**Progress table.** The table at the end of this plan lists status and percent. Recompute the all-epics row from the task rows that are `done`, `partial`, or `not-started`. Do not write a single headline percent for the whole project.

Phase Three Addendum sits between Phase 3 and Phase 4. Its tasks are in that same table. The Launch Addendum sits after Phase 7. Its tasks are in that same table. The all-epics row is the mean of every task in Phases 1–7 and both addenda.

Will not be built: a web app, clipboard, camera, screen, or microphone capture, a chat UI, a generative summary, a helper process for the embedder, any speech-to-text model, browser history, sync transport, a cloud build other than the one macOS compile workflow, HNSW-RaBitQ, bundled model weights, and stored vectors at 128 numbers.

Stack for every task: `net10.0`, MAUI Blazor Hybrid, Tailwind, four buildable shells (`android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64`) and v1 RID `linux-x64` with no shell, in-process llama.cpp once a build contains commit 4fbc76dec51d0add466f0210855c0596589b60d4; the class in the tree is LlamaTextEmbeddingEncoder, NuGet id `ZVec.NET` at `1.0.0-beta.6` from [Directory.Packages.props](../Directory.Packages.props) (local tree `D:\A_S\AdamSystems.ZVec.NET` reports `1.0.0-beta.6+zvec.0.7.0`; do not restore a package named `AdamSystems.ZVec.NET`). Git remote is https://github.com/ahmedSamir50/Recall. Branch main is the pushed branch. Windows, Linux in WSL or Docker, and Android on an emulator or a phone over adb are the local proof hosts. ios-arm64 and maccatalyst-arm64 stay in the solution and compile on a GitHub-hosted macOS runner. A physical iPhone or Mac run is not a done-when. Platform code for those heads is still written.

**Content id (Phase 2+):** SHA-256 of modality, source-content hash, stored width, and chunk ordinal (`0` when the note is one vector). A repeated insert of the same id is skipped, including after restart when the store already has that id. The first insert also appends a local unsent `INSERT` and a line in the UTC-day file under `oplog`. Sync stays off.

**Stale references (do not treat as this gate):** InitDocs 128-wide cold tiers, HNSW-RaBitQ, `[semantic]` / `[search]` prefixes, AdamSystems.ZVec.NET as the NuGet id, and the InitDocs ~50 ms cell as the Phase 2 benchmark pass line.

Open measurements, not tasks: disk size, memory after warmup, and whether crash reports ever leave the device. Crash reports stay on the device until that choice is made.

---

## Phase 1 — Shell — `done`

The app opens, speaks Arabic and English, and remembers this device. It does not index yet.

### Epic 1.1 — Solution — `done`

- **1.1.1** — `done` — Create `Recall.Core`, `Recall.UI`, `Recall.Maui`, benchmark and test projects. Core has no MAUI, Razor, or HTTP. UI calls Core ports only. Maui is the only composition root. Done when a phone and a Windows build both open an empty shell. The Windows shell and an Android shell were opened. iOS and Mac Catalyst installers stay on task 7.2.1.
- **1.1.2** — `done` — Add the ports: scope watcher, scope manager, video frame sampler, secure storage, idle detector, model download, index policy. Done when each port has a fake and no clipboard, camera, screen, or mic type exists. Each port has a fake. Clipboard, camera, screen, and mic types are absent. The watcher is no longer empty. The sampler, secure storage, and idle detector stay empty until task 4.2.1.
- **1.1.3** — `done` — Ban `HttpClient`, `Socket`, `TcpListener`, and `UdpClient` inside Core, including the sync stub. Done when a socket in Core fails the build. BannedSymbols.txt is an AdditionalFile, the banned-API analyzer is on, and BannedSymbols_ListsHttpClientSocketTcpListenerUdpClient locks the four names.

### Epic 1.2 — Screens that can ship — `done`

- **1.2.1** — `done` — Put every visible string in `ar` and `en` resource files. Arabic layout is right to left. Done when a string in only one language fails a test. StringsResx_ArabicAndEnglish_HaveTheSameKeys passes.
- **1.2.2** — `done` — First screen is the folder grant: add and remove folders, pick file types, show the overlap warning. iPhone copy says new files are picked up when Recall is opened. Done when that screen works with no folders granted. The empty-grant screen was opened on Windows and Android. Continue was disabled.
- **1.2.3** — `done` — Add search, results, pin, age, this-device, and model-download screens as empty states. Pin copy is “keep this easy to find.” Age choices are only 2, 5, and 7 years, starting at 2. No width or quantization words on screen. Done when each empty state exists in both languages. Those routes and empty states exist in both languages. Home calls Find in task 2.3.2. The shell header shows RAM, CPU, and GPU through `ResourceStrip` on every width. `AppShell_ShowsResourceStrip` locks the strip.

### Epic 1.3 — This device — `done`

- **1.3.1** — `done` — On first launch, store the public platform device id (random only when the read is empty, known-bad, or not ready). Store the native OS device name; both stay forever. Done when a second launch does not mint a new id and known-bad Android id `9774d56d682e549c` is rejected. EnsureLoaded_KeepsFirstStoredId_WhenLaterCandidateDiffers and ChooseId_RejectsKnownBadAndroidId pass.
- **1.3.2** — `done` — Let the person edit the display name Recall shows. The native name stays read-only. Done when the original name and the id are unchanged after the edit, and neither name nor the id is sent on the network. TrySetDisplayName_LeavesDeviceIdAndOriginalOsNameUnchanged passes. BuildRequestUri takes only the public file name and the base URI.

---

## Phase 2 — Text search — `done`

A granted text file can be found by meaning and by words. Scope stays a one-shot read when download finishes — not a watcher or Index now. A granted code file is searched as text: the text encoder, `items_text`, width 512.

### Epic 2.1 — Model download — `done`

- **2.1.1** — `done` — Download embeddinggemma-2-Q8_0.gguf into app data. Check SHA-256. Resume after a drop. The request may name the public weight file. It must not carry a user document name, document text, vector, device id, native device name, or custom display name. Float16 and a 4-bit file are not this download. Vision and audio files stay unloaded. Done when a bad hash fails, a retry works, and the request builder omits those fields. MauiModelDownloadService appends a 206, replaces on 200, keeps a short body as .partial, hashes the one GGUF before moving it, and retry starts the download again. URL, file name, and checksum come from appsettings.json. DownloadRequestBuilder_OmitsForbiddenPayload and DownloadTransfer_PlacesBody lock the request and resume rules. No public-network download in tests.
- **2.1.2** — `done` — Show progress, hash failure, and retry. Copy says the fetch is model weights only. Done when those three states exist in Arabic and English. The shell models screen shows those states and says the fetch is model weights only. The percent counts finished files plus the current file, and a progress tick is published only when that percent changes. `TextModelDownloadUiPolicy.MarkVerifying` assigns `Verifying` before the hash check. `MarkVerifying_SetsPhaseAndClearsFault` locks it.
- **2.1.3** — `done` — Keep weights out of the installer. Done when the app package contains no ONNX weights and no GGUF weights. `*.onnx`, `*.gguf`, and `tests/Recall.IntegrationTests/TestAssets/embeddinggemma-2-text/` are gitignored. `MauiProject_ContainsNoOnnxWeights` scans `src/Recall.Maui` (except `bin` and `obj`) and the csproj for `.onnx` and `.gguf`. No apk build.

### Epic 2.2 — Text embed and index — `done`

- **2.2.1** — `done` — Run EmbeddingGemma 2 through in-process llama.cpp. The library mean-pools. C# L2-normalizes the 768 numbers. Store text at 512 by keeping the leading part and L2-normalizing again. Done when a 128-wide store throws and a unit vector is length 1 after the cut. `CutAndNormalize` L2-normalizes the 512 slice. `IndexPolicy_Text512_Accepts_128Throws` and `Matryoshka_AfterCut_LengthIsOne` lock the width and the cut.
- **2.2.2** — `done` — Prefixes live in one Core type. Search uses `task: search result | query: {query}` once. A titled note uses `title: {title} | text: {content}` once. Cluster and classify prefix strings may exist on that type and are not wired to a query in this gate. Do not copy InitDocs `[semantic]` or `[search]` prefixes. Done when a second wrap of the prefix fails a test. `EmbeddingTaskPrefixes` is that type. An empty title is `text: {content}`. A second search wrap throws. Unit tests cover both.
- **2.2.3** — `done` — Create one text collection. Index params: plain HNSW, `M=16`, `efConstruction=200`, `QuantizeType` left `Undefined`. `EfSearch` 64 is set on `ZVecHnswQueryParams`, because the index param has no such field and a null query value uses 300. Done when one float index exists and a quantized second index is not created. `ZVecTextVectorIndex` creates `items_text` with those index params and sets `EfSearch` 64 and RRF rank 60 on the query. `Recall.Maui` references `ZVec.NET` so the phone native library is packaged. Android 16 warns that `libzvec_c_api.so` is not 16 KB page-aligned. A second quantized index was not created. The opened root contains a single `items_text` directory. The Android 16 KB warning stays a ZVec native warning.
- **2.2.4** — `done` — Token counts come from recall_embed_count_tokens on the loaded GGUF. The 8192 budget includes the prefix. One vector when the note fits. Split only past that. Do not average chunks. Later chunks keep the title prefix and do not split a surrogate pair. Done when an over-limit note becomes more than one vector and an under-limit note stays one. The chunk planner and unit tests do that with a fake counter. Windows, Mac Catalyst, Android, and iOS use that same count. They do not fail closed. `PlanChunks_UnderBudget_StaysOnePiece` locks the one-piece branch. tokenizer.json, Tokenizers.DotNet, and GemmaTextTokenizer are not the loader.
- **2.2.5** — `done` — BenchmarkDotNet text-only exit: one text session, one 512-d float HNSW graph, `EfSearch` 64, `QuantizeType` off, insert ≤5 ms, hybrid top-20 ≤200 ms mobile / ≤25 ms desktop. Encoder load is its own number (≤1000 ms mobile / ≤800 ms desktop) and is not added into query time. Label emulator and desktop runs so they are not reported as a phone. The benchmark project embeds one search query only when `RECALL_TEXT_MODEL_DIR` points at the text model, and it exits without a number otherwise. Desktop run `Host: desktop`: `InsertOne` mean 30.66 us, `HybridTop20` mean 2,202.43 us, `LoadTextEncoder` mean 727.3 ms. Those insert and hybrid means sit inside the desktop lines on 1000 identical vectors. That run is not a 100k diverse graph, not a recall number, and not an `EfSearch` pressure test. The 800 ms line is that load mean plus 10 percent. The recorded desktop run closes this task. The mobile lines are hybrid ≤200 ms and load ≤1000 ms. Task A.2.9 records them. Recall.Benchmarks is a Windows net10.0 executable. Stores: the benchmark process only. Signature: `InsertOne`, `HybridTop20`, and `LoadTextEncoder`. A missing model directory exits with no number and does not pass. `DesktopIdenticalVectors_AreNotThePhoneGate` locks the partial row. A connected phone cannot run Recall.Benchmarks. That project is net10.0. The 727.3 ms load is the recorded ONNX run and is not a llama.cpp measurement. An emulator label is not a phone number.
- **2.2.6** — `done` — A granted code file is text. The grant offers Code beside Text, Images, Video, and Audio. The English label is `Code`. The Arabic label is `كود`. Extensions, exact names, skip folders, secrets, databases, lock files, and gitignore rules are the canon bullets. Embed with the text encoder into `items_text` at 512, using `title: {file name} | text: {content}`. There is no separate code collection and no `code:` query prefix. Opening a hit opens the file. Preferences shows those lists. The add row sits above each list. Built-in names are wrapping chips in a scroll box with a per-group filter. Folders and secrets start collapsed. Built-in rows cannot be removed. The person can add an extension, a folder name, or a file name, and can remove only what they added. The heading is `Skipped files` and `الملفات المتخطاة`. Done when a `.cs` file in a grant with Code on is searchable as text, a grant with Code off does not index it, a `.env` file and a `.db` file are not read, a file ignored by `.gitignore` is not read, and an extension the person added is skipped. `Decide_CsWithCode_Reads`, `ReadAsync_CodeOff_SkipsCs`, `ReadAsync_Env_NotRead`, `ReadAsync_Db_NotRead`, `ReadAsync_GitIgnore_SkipsTxt`, `TryAdd_Log_AddedAndLowercased`, `PreferencesRazor_AddBeforeBuiltInList`, `PreferencesRazor_BuiltInHasNoRemove`, and `SkipFilter_Blank_IsTrue` lock it. Task 2.2.6 is done.

### Epic 2.3 — Query text — `done`

- **2.3.1** — `done` — Embed the question once. Dense HNSW plus BM25 on the raw note text, plus the folder filter, fused with `ZVecRrfReranker` at `RankConstant` 60. A file outside the grant is not returned. Done when skipping RRF fails a test and an out-of-grant path is dropped. `TextSearchService` embeds once and cuts to 512. The index fuses dense and full text at rank 60 and keeps a hit only when the path is inside the grant, so `D:\Photos` does not match `D:\PhotosExtra`. BM25 stores the chunk text. A rank other than 60 throws. `Search_DropsPathOutsideGrant` drops a path outside the grant, and Home calls `FindAsync`.
- **2.3.2** — `done` — The search page is Find only. Group and Tag do not run a query. Show the top 20. Each card shows kind, name, local time, and why it matched. Opening a card opens the file. A missing file stays on the card as unavailable. Under the search box, show ingested file counts per enabled grant kind as finished, seen, and percent from `checkpoints.db`. Done when demo cards, the translation line, and the fake item count are off the page and the first five cards can show before the rest are drawn. Home submits Find on Enter, shows up to 20 cards with the first 5 immediate, opens a present file via `ILocalFileOpener`, and lists ingest counts when a grant enables a kind. `HomeRazor_OmitsDemoCards`, `HomeRazor_ShowsIngestHeading`, `Count_Text_TwoOfFive_IsForty`, and `List_TextAndImage_OmitsAudio` lock the Find page and the counts.

---

## Phase 3 — Folders that stay current — `done`

New, changed, and deleted files inside a grant update the index without a timer.

### Epic 3.1 — Grants — `done`

- **3.1.1** — `done` — If one grant sits inside another, keep the parent and warn. Done when `D:\` plus `D:\Photos` indexes `D:\` only, with the warning in both languages. `TryAddGrant_NullGrant_Throws`, `TryAddGrant_BlankPath_ThrowsAndDoesNotSave`, `TryAddGrant_ChildOfGrantedParent_ReturnsOverlap`, `TryAddGrant_ParentReplacesChild_KeepsDriveRoot`, `TryAddGrant_ParentReplacesTwoChildren_KeepsDriveRoot`, `TryAddGrant_PhotosExtra_IsNotAChild`, `TryAddGrant_Duplicate_DoesNotSave`, `OverlapKey_BothOverlapStatuses_ReturnWarning`, `OverlapKey_AddedAndDuplicate_ReturnNull`, and `OverlapWarning_BothLanguages_SayParentIsKept` lock it.
- **3.1.2** — `done` — A path the person allowed but has not indexed yet is offered from the search page. Done when **Index now** indexes it and a child of an already allowed parent is not offered again. `ListOffers_Empty_IsEmpty`, `ListOffers_UnfinishedRoot_IsOffered`, `ListOffers_FinishedRoot_IsHidden`, `ListOffers_ChildOfAllowedParent_IsHidden`, `ListOffers_ChildWithoutParentInList_IsOffered`, `IndexAsync_FinishesGrant_AndOfferDisappears`, `ShouldShow_NullOrEmpty_IsFalse`, `ShouldShow_OneOffer_IsTrue`, and `HomeRazor_IndexNow_UsesBothLanguagesAndLtrPath` lock it.

### Epic 3.2 — Watch and scan — `done`

- **3.2.1** — `done` — Every shell diffs the granted reader against checkpoints.db on open and on resume. Windows, Mac Catalyst, and a real Android directory add one FileSystemWatcher per parent. iPhone does not construct FileSystemWatcher. A content:// grant has no live watcher. Done when each shell reports created, modified, renamed, and deleted for a granted folder. `Choose_WindowsAndroidMacIosUnknown_MapToWatcherKind`, `TryAdd_BlankPath_IsIgnored`, `TryAdd_SamePath_ReplacesWithoutGrowing`, `TryAdd_SixtyFifthPath_OverflowsAndRequestsRescan`, `Drain_ClearsItems`, `AcknowledgeRescan_ClearsFlag`, `TryMap_BlankOrError_ReturnsFalse`, `TryMap_RenamedWithPrevious_ReturnsRenamed`, `TryMap_RenamedWithoutPrevious_ReturnsCreated`, `TryMap_DeletedChangedCreated_ReturnThoseKinds`, `Select_NullOrEmpty_ReturnsEmpty`, `Select_Blank_IsDropped`, `Select_Duplicate_KeepsOne`, `Select_ChildOfParent_DropsChild`, `Select_ThousandChildren_StaysOneRoot`, `Select_TwoSiblingRoots_ReturnsBoth`, `Choose_WindowsMacAndroid_UseFilesystemWatcher`, `CanWatchDirectories_IosPhone_IsFalse`, `CanWatchDirectories_MacCatalyst_IsTrue`, `CanWatchDirectories_Windows_IsTrue`, `WindowsFileScopeWatcher_NestedGrants_OpensOneWatcher`, `WindowsFileScopeWatcher_ContentUri_OpensZeroAndRescans`, `DisabledScopeWatcher_Start_DoesNotWatch`, `Stop_LeavesWatchingFalse`, `WindowsFileScopeWatcher_TempFolder_ReportsCreatedModifiedRenamedDeleted`, `Stop_DisposesAndClearsWatching`, `OnError_RequestsRescan`, `OnCreated_Ios_StartsLoopWithoutWatcher`, `OnCreated_Unknown_DoesNotStart`, `OnCreated_Windows_StartsWatcher`, `RequestRescan_BeforeLoop_SetsFlag`, `PumpOnce_WaitsUntilReport`, `TryAdd_ConcurrentWithDrain_DoesNotThrow`, `PumpOnce_Cancel_Stops`, and `Stop_StopsWatcher` lock it.
- **3.2.2** — `done` — iPhone walks the granted folder when the app comes to the foreground and diffs `checkpoints.db`. Done when a file added while the app was closed is queued on the next open, and no background watch exists. `Diff_AddedWhileClosed_EmitsCreated`, `Diff_SameHash_EmitsNothing`, `Diff_DifferentHash_EmitsModified`, `Diff_MissingStored_EmitsDeleted`, `Diff_AlreadyMissing_EmitsNothing`, `Diff_UniqueHashPair_EmitsRenamed`, `Diff_RepeatedHash_DoesNotPair`, `Diff_BlankPath_IsIgnored`, `OnResumed_Ios_RequestsRescan`, and `PumpOnce_ExceptionOnFirstChange_AppliesSecond` lock it.
- **3.2.3** — `done` — A rename updates the path. Embed again only when the bytes changed. A delete keeps the vector, writes a tombstone, and marks the source missing. Done when those three cases are tested. `Apply_Deleted_AppendsDeleteAndDoesNotEmbed`, `Apply_DeletedWhenAlreadyMissing_DoesNotAppendAgain`, `Apply_DeletedWithNoChunks_MarksMissingOnly`, `Apply_RenamedSameHash_DoesNotEmbed`, `Apply_RenamedChangedHash_RetiresAndEmbedsOnce`, `Apply_RenamedUnknownPrevious_IngestsNewPath`, `Apply_RenamedMissingDocument_MarksPreviousMissing`, `Apply_CreatedNullDocument_DoesNotWrite`, `Apply_BlankPath_DoesNotWrite`, `Apply_Cancelled_Throws`, `TryRead_MissingBlockedIgnoredOrUnsafe_ReturnsNull`, `TryRead_AdmittedText_ReturnsDocument`, `Apply_RetiredChunk_DropsHit`, `Apply_MissingChunk_SetsSourceMissing`, `Apply_ChunkPath_ReplacesStoredPath`, `Apply_PathOutsideGrant_DropsHit`, `Apply_NoChunk_KeepsHit`, `Build_SourceMissing_IsUnavailableEvenWhenFileExists`, `Build_MissingFile_IsUnavailable`, and `Build_PresentFile_IsAvailable` lock it.

### Epic 3.3 — Crash cursor — `done`

- **3.3.1** — `done` — Write ingest progress to `checkpoints.db` in a transaction. Done when a kill midway resumes the unfinished file and does not re-index a finished one. `Decide_NullReadingMissingAndChangedHash_Read`, `Decide_FinishedSameHash_Skip`, `Decide_BlankHash_Throws`, `MarkReading_Committed_SurvivesNewStore`, `MarkFinished_DuplicateChunkId_RollsBackToReading`, `MarkFinished_ThenDecide_SkipsSameHash`, `RoundTrip_QuoteInPath_UsesParameter`, `RenamePath_MovesFileAndLiveChunks`, `MarkMissing_KeepsChunkRowAndSetsMissing`, `RetirePath_SetsRetired`, `IsGrantFinished_OnlyWhenFinished`, `IngestGrant_UnknownPath_DoesNotRead`, `IngestGrant_TextAndCodeOff_DoesNotRead`, `IngestDocument_FinishedSameHash_DoesNotEmbed`, `IngestDocument_Reading_Embeds`, `IngestDocument_ThrowAfterMarkReading_StaysReading`, `Resume_ReadingRow_EmbedsAgain_FinishedRow_DoesNotEmbed`, `AppendDelete_Blank_Throws`, `AppendDelete_Memory_IsDeleteAndUnsent`, `AppendDelete_NoDirectory_WritesNoFile`, and `AppendDelete_Directory_WritesDeleteLine` lock it.

---

## Phase Three Addendum — Gaps left by Phases 1–3 — `done`

This phase is new. It does not rewrite Phases 1, 2, or 3. Task 2.2.5 is done on the recorded desktop run. Task A.2.9 closed the mobile hybrid and load lines at 200 ms and 1000 ms. The other rows stay as written. The sentences in this paragraph are not tasks.

“Stack for every task” names the four buildable shells. linux-x64 is a v1 RID and has no shell. Task 1.1.1 stays done on a phone and Windows. The four-shell install stays task 7.2.1. Task 1.1.2 says the sampler, secure storage, and the idle detector stay empty until task 4.2.1. That sentence stays. The sampler is task 4.2.1. Secure storage stays empty, and task 4.2.1 does not fill it. The idle detector is task A.2.8. Task 1.2.2 stays done. A granted text file is found on Windows and Mac. A granted text file is found on Windows, Mac, Android, and iOS through the same in-process llama.cpp library. Task 3.2.1 diffs every shell against `checkpoints.db` on open and on resume. That diff is the catcher for a file that changed while Recall was closed. There is no timer, and Index now is not that catcher. Task 3.1.2 still offers only a path that is not indexed yet. A finished grant stays hidden. The code order is `MarkReading`, then `TextIngestEngine` (`TryInsert`, then `AppendInsert` when the insert is new), then `MarkFinished`. A kill after the vector insert and before the op-log line loses that unsent line. The next `Reading` pass finds the id, skips a second insert, and `MarkFinished` completes. A kill after the op-log line and before `MarkFinished` keeps the line and finishes the same way. Search on this device recovers. The lost line is only an unsent sync entry. Sync stays off. If sync is later turned on, that lost line has to be closed first. Two paths with the same bytes are task A.2.10, not this crash gap.

### Epic A.2 — Closures — `done`

- **A.2.2** — `done` — A shrink is a byte change. Task A.2.13 deletes the ZVec row of a previous id only when `IIngestCursorStore.AnyLivePath(contentId)` is false. This task appends `DELETE` once for that id. Another live path keeps the row and writes no `DELETE`. A source delete is task 3.2.3 and keeps the vector. Stores: `checkpoints.db` table `ingest_chunk_path`, and the ZVec collection. Signature: `bool IIngestCursorStore.AnyLivePath(string contentId)` and `bool ITextVectorIndex.TryDelete(string contentId)`. A kill after the alias retire commits and before `TryDelete` leaves the alias retired and the vector present. The next `ITextVectorIndex.Reconcile` deletes it before Find. Done when a shrink from two chunks to one removes the second id from Find, and the same id with another live path stays. `Shrink_LastPath_DeletesVector` and `Shrink_OtherPathLive_KeepsVector` lock it. Phase 4 waits on this task.
- **A.2.3** — `done` — Parent wins file types. Same path stays a duplicate. A child of a parent stays refused. A parent added after children replaces those children. The parent’s file types are the grant. The child’s types are not merged. `ScopeGrantService.TryAddGrant` already keeps the parent grant. Done when a parent granted Text only, added after a child granted Images, keeps Text only, and a test locks that. `TryAddGrant_ParentTextOnly_KeepsText` locks it. Phase 4 waits on this task.
- **A.2.4** — `done` — Each vector row stores scalar field `encoder_hash`. The same hash is stored on the file row in `checkpoints.db`. It is not part of the content id. v1 does not download a new file over the text encoder whose checksum task 2.1.1 already accepted. A different hash sets the file to `Reading`, calls `TryDelete`, re-embeds under the same content id with the new `encoder_hash`, then sets `Finished`. Source bytes may be unchanged. Search filter `encoder_hash` equals the current file, so an old hash does not enter the 64. Picture, video, and audio rows use the same field. Stores: `checkpoints.db` column `encoder_hash` on `ingest_file`, and the ZVec row. Signature: `bool ITextVectorIndex.TryDelete(string contentId)` and `bool ITextVectorIndex.TryInsert(TextIndexInsert insert)`. `TextIndexInsert` includes `string EncoderHash`, `bool PathLive`, and `string GrantPath`, and `TryInsert` writes those fields. A kill after `Reading` commits and before `TryDelete` leaves the old row. The filter hides it. Resume deletes and re-embeds. A kill after `TryDelete` and before `Finished` leaves no row and state `Reading`. Resume inserts and finishes. Done when a second hash keeps the content id and Find returns the new vector only after `Finished`. `SecondHash_DeletesOldRow_ReembedsSameId` and `Repair_KillAfterDelete_ResumesFromReading` lock it. Phase 4 waits on this task.
- **A.2.5** — `done` — Task 2.2.6 stays done on its written Done when. This task locks the canon classes that sentence names and that check does not. Exact name: `IsCodeFile_MakefileAnyCase_IsTrue`. Skip folder: `IsBlockedFolder_Git_IsTrue`. Secret other than `.env`: `IsSecret_Pem_IsTrue`. Lock file: `IsLockFile_PackageLock_IsTrue`. Done when those four tests stay the locks. Phase 4 does not wait on this task.
- **A.2.6** — `done` — The text collection creates one float HNSW index and one full-text index on `fts_key`. `raw_text` is a scalar. An opened schema reports `fts_key` as a string field. A collection without that field is destroyed and created again on open, and `ResetTextIngest` clears `checkpoints.db` so the foreground diff reads the granted files again. Task 2.2.3 stays the float-index check. `ZVecTextVectorIndex` sets `ZVecFtsIndexParam` on `fts_key`. Done when a test fails if that full-text index param is removed. `TextSchema_FtsKey_UsesFtsIndexParam` fails if `ZVecFtsIndexParam` is removed from `CreateSchema`. Phase 4 does not wait on this task.
- **A.2.7** — `done` — When the text model download reaches Ready, `MauiModelDownloadService` reads each grant once through `IngestGrantedTextAsync`. A grant with Text off and Code off is not read. A finished file with the same bytes is not embedded again. That pass does not start a watcher. `IngestGrant_TextAndCodeOff_DoesNotRead` and `IngestDocument_FinishedSameHash_DoesNotEmbed` stay. Done when a test shows the Ready path calls that read. `TextModelReadyIngest_ReadGrantsAsync_CallsIngest` locks it. Phase 4 waits on this task.
- **A.2.8** — `done` — The idle detector gates ingest. It does not sample video. Task 4.2.1 is the sampler only. Task 1.1.2’s “until 4.2.1” sentence is not edited; this task is the idle port. `IIdleDetectorService` is that port. An idle signal runs the next ingest. A busy signal defers it. The file is not dropped. Done when an idle signal runs that ingest and a busy signal defers it. `Idle_RunsIngest_BusyDefers` and `Idle_Coordinator_BusyKeepsChange_IdleDrains` lock it. Phase 4 waits on this task. Task 4.2.1 does not fill this port.
- **A.2.9** — `done` — Task 2.2.5 is done on the recorded desktop run. This task owns the mobile hybrid and load cells. The lines are hybrid ≤200 ms and load ≤1000 ms. The desktop run and the written 800 ms cell stay the desktop record. That desktop run inserts 1000 identical vectors and is not a 100k diverse-graph result, not a recall number, and not an `EfSearch` pressure test. `AndroidTextBenchSession` in `com.recall.shell` times the same `HybridTop20` and `LoadTextEncoder` bodies as `Recall.Benchmarks`, warmup 1 and measured 10, and writes `android-text-bench.txt`. Host label `emulator` on adb serial `emulator-5554` (`x86_64`, 1 GB RAM, `android-x64` install). The APK packages NDK `libomp.so` beside `librecall_embed.so`, and that library exports `recall_embed_open`. Report from `run-as com.recall.shell cat files/android-text-bench.txt`: `Host: emulator`, `HybridTop20 mean 135.37 ms`, `LoadTextEncoder mean 886.14 ms`. Both means are inside 200 ms and 1000 ms. `PhoneHybridAndLoad_MeetTheExit` locks those lines. Phase 4 does not wait on this task.
- **A.2.10** — `done` — Two paths with the same bytes share one content id. The content id formula stays. `ingest_chunk.content_id` stays the primary key and keeps the first path. The second table is `ingest_chunk_path(content_id TEXT NOT NULL, path TEXT NOT NULL, missing INTEGER NOT NULL, retired INTEGER NOT NULL, PRIMARY KEY(content_id, path))`. On open, each existing `ingest_chunk` row is copied into that table in the schema transaction when the pair is absent. Find joins live rows (`missing = 0` and `retired = 0`) and returns one card per live path. One vector is stored. The second path is local. The op-log line stays `{at}\t{operation}\t{contentId}` and a second `INSERT` is not written. Deleting one path sets that alias `missing = 1`. `DELETE` is appended, and ZVec `path_live` becomes 0, only when `AnyLivePath` is false. Equal RRF scores list paths in ordinal order. A width change is a new content id. Task 6.1.2 calls `MoveLiveAliases` before `TryDelete` of the long id. This task does not age a row. Stores: `checkpoints.db` and one ZVec row. Signature: `void IIngestCursorStore.AddAlias(string contentId, string path)`, `bool IIngestCursorStore.AnyLivePath(string contentId)`, and `void IIngestCursorStore.MoveLiveAliases(string fromContentId, string toContentId)`. A kill inside `AddAlias` rolls the SQLite transaction back and leaves the second file `Reading`. There is no ZVec write on the second path. The same change replaces `MarkFinished_DuplicateChunkId_RollsBackToReading` with `SecondPath_SameBytes_FinishesAndBothPathsHit`. `DeleteOnePath_SharedId_OtherPathStaysHit` and `SecondPath_SameBytes_DoesNotAppendInsert` lock the refcount. Phase 4 waits on this task. Task 6.1.2 calls `MoveLiveAliases`.
- **A.2.11** — `done` — Search order is one pipeline. ZVec fields `path_live`, `encoder_hash`, and `grant_path` are the filter. `path_live` is 1 only when some alias is live. The filter runs inside the query, before the 64 cap. There is no two-phase commit. SQLite commits first. `grant_path` is the path of the grant that enumerated the file, from task A.2.12, not the document URI. `TextIndexInsert` carries `EncoderHash`, `PathLive`, and `GrantPath`, and `TryInsert` writes them. The alias transaction also inserts `pending_reconcile(content_id TEXT PRIMARY KEY)`. `void ITextVectorIndex.Reconcile(IReadOnlyList<VectorLiveState> states)` updates only those ids. `FindAsync` calls it only when that table is non-empty. An empty table does not scan the corpus. One Find reconciles at most 64 pending ids, the same 64 as the raw-list cap, and leaves the rest pending. Find and ingest share the checkpoints write transaction. A Find that cannot begin that transaction returns without waiting and without calling `Reconcile`. A kill after the SQLite commit and before `Reconcile` leaves the pending row and the fields stale until the next Find or ingest resume. Then one RRF at `k=60`. Then one hit per path, keeping the row with the highest RRF score. A tie keeps the lower chunk ordinal. Then at most 20. Fewer than 20 returns those. Twenty chunks of one path become one card. Stores: ZVec scalar fields, `ingest_chunk_path`, and `pending_reconcile`. Signature: `string TextSearchFilter.Build(IReadOnlySet<string> grantPaths, string encoderHash)`, `void ITextVectorIndex.Reconcile(IReadOnlyList<VectorLiveState> states)`, and `MediaSearchHit Collapse.EarliestStart(IReadOnlyList<MediaSearchHit> hits)`. `TwentyChunks_OneFile_ReturnsOneCard` and `TwentyChunks_OneFile_KeepsMaxRrfScore` lock the collapse. `Find_PendingOver64_ReconcilesSixtyFour` locks the cap. `Find_WriteBusy_SkipsReconcile`, `Find_NoPending_DoesNotReconcile`, and `Find_TwentyChunks_ReturnsOneCard` lock the busy transaction and reconcile. Phase 4 waits on this task. Task 6.2.1 waits on this task. Task A.2.15 uses this filter.
- **A.2.12** — `done` — Ingest grant lookup and search use the same membership. A path is inside a grant when that grant’s reader enumerated it, including a `content://` document URI that does not start with the tree URI. The ZVec field `grant_path` stores that grant’s path, not the document URI. `Normalize` does not replace `/` with `\` inside `content://`. `content://photos` does not match `content://photosExtra`. Stores: ZVec field `grant_path` and `ingest_chunk_path.path` for the document URI. Signature: `string GrantMembership.GrantPathFor(string documentPath, IReadOnlyList<ScopeFolderGrant> grants)`. A kill before `TryInsert` leaves no row. Done when a document URI enumerated under a tree grant is inside that grant on ingest and on search, the stored `grant_path` equals the tree URI, and a URI that was not enumerated is outside. `ContentUri_EnumeratedDocument_IsInsideGrant`, `ContentUri_NotEnumerated_IsOutside`, `ContentUri_PhotosExtra_IsOutside`, and `GrantPathFor_NullDocument_Throws` lock it. Phase 4 waits on this task. Task A.2.11 stores grant_path from this task.
- **A.2.13** — `done` — When the bytes change, retire this path’s old aliases, `TryDelete` an old id only when `AnyLivePath` is false, append `DELETE` only then, and `TryInsert` the new ids. A short row stays in the old collection at 512 unless the path is pinned, in which case the new row is written long and the short row is deleted. A long row that is now past the cutoff is written short. A long row that is still young is written long. A younger last-write time does not move a short row back. A source delete keeps the vector and sets `path_live` from the aliases. Stores: `checkpoints.db` and the ZVec collection. Signature: `bool ITextVectorIndex.TryDelete(string contentId)`. Order: commit `Reading`, `TryInsert` the new row, `TryDelete` an unshared old row, commit `Finished` and the aliases. A kill after `TryDelete` and before `Finished` leaves the file `Reading`, the new vector stored, and the path absent from Find until resume commits the alias. `ChangedBytes_DeletesOldZvecRow`, `ChangedBytes_SharedId_KeepsVector`, `ShortRow_Edit_StaysShortUnlessPinned`, and `ChangedBytes_KillAfterTryDelete_StaysReadingUntilResume` lock it. Phase 4 waits on this task.
- **A.2.14** — `done` — The BM25 key is the folded string. The card shows the raw note. Folding is NFKC, then drop U+0640, U+064B through U+065F, and U+0670, then map U+0622, U+0623, U+0625, and U+0671 to U+0627. Do not add Yeh, Alef Maqsura, Teh Marbuta, or a Hindi or French stemmer. Each character in Unicode script Han is one token. It is not a bigram. Spaces are not the Han split. Non-Han runs then go through `Standard` plus `Lowercase`. Stores: the FTS key on the vector row, and the raw string on that same row for the card. Signature: `string KeywordFold.Fold(string raw)` and `IReadOnlyList<string> KeywordFold.Tokens(string folded)`. A kill has no store write in this task. The ingest task that calls it owns the commit. Done when alef and tatweel variants match, one Han character is one token, and the card text is the raw note. `Fold_AlefAndTatweel_Match`, `Fold_HanCharacter_IsOneToken`, `Card_ShowsRawNote`, `Fold_Yeh_StaysYeh`, `Fold_Null_Throws`, and `Tokens_LowercasesNonHan` lock it. Phase 4 waits on this task. Task 4.1.2 uses `KeywordFold.Tokens`. Task 6.2.1 waits on this task.
- **A.2.15** — `done` — The grant filter is the ZVec field `grant_path` from task A.2.11, inside the query, before the 64 cap. `EfSearch` stays 64. An out-of-grant vector does not enter the 64. The result count is the smaller of 20 and the number of live in-grant paths after the per-path collapse. Stores: ZVec field `grant_path`. Signature: `string TextSearchFilter.Build(IReadOnlySet<string> grantPaths, string encoderHash)`. A kill does not write in this task. `Reconcile` in task A.2.11 repairs a stale `grant_path` before Find. Task 2.3.1’s sentence stays. Done when nearer out-of-grant vectors do not consume a slot, and four in-grant files return four. `Search_GrantFilter_DoesNotPromiseTwenty` locks it. Phase 4 does not wait on this task. Task 6.2.1 waited on this task. This task waited on A.2.11.
- **A.2.16** — `done` — A non-code text span is embedded only when it has at least one letter and the letter count times 4 is at least the count of the other characters that are not spaces. A code file from task 2.2.6 always gets a dense vector. A span that fails the letter test is not a ZVec row and is not a zero vector. It is inserted into `checkpoints.db` table `bm25_only(content_id TEXT PRIMARY KEY, path TEXT NOT NULL, raw_text TEXT NOT NULL)` inside the `MarkFinished` transaction. Find orders `bm25_only` hits by how many folded query tokens occur in `raw_text`, highest first, appends that order after the ZVec BM25 ranks, and caps the merged BM25 list at 64. Stores: `bm25_only` only. Signature: `bool NumericSpanPolicy.IsDense(string raw, bool codeFile)` and `void IIngestCursorStore.PutBm25Only(string contentId, string path, string rawText)`. A kill before `MarkFinished` commits leaves no `bm25_only` row and the file `Reading`. Done when a digits-only text span is absent from HNSW and present in the BM25 list, and a `.cs` file is still a dense vector. `NumericSpan_DigitsOnly_Bm25Only` and `NumericSpan_CodeFile_StaysDense` lock it. Phase 4 does not wait on this task.
- **A.2.17** — `done` — Skip a folder that is not the granted root when its direct children include at least 300 image, audio, or video files and at least 70 percent of those direct names look machine-made. Nested files do not add to the count. After one extension is removed, a name looks machine-made only when it is 32 hex characters with no dashes, or a UUID of the form 8-4-4-4-12 hex, or at least 12 digits and no letters. `1.jpg` and `cafe1234` do not match. The skipped folder is not watched and gets no checkpoint rows. The report is one Find line. English `Skipped a generated folder.` Arabic `تم تخطي مجلد مُولَّد.` The path is shown left to right. The granted root is still indexed. Stores: none for the files inside the skipped folder. Signature: `bool DatasetFolderSkip.ShouldSkip(string folderPath, bool isGrantRoot, IReadOnlyList<string> directChildNames)`. A kill leaves no partial checkpoint for that folder. Done when 210 of 300 names of 32 hex characters skip the folder, `1.jpg` through `300.jpg` do not, and the granted root is indexed. `DatasetFolder_32Hex_Skips`, `DatasetFolder_NumberedScans_NotSkipped`, `DatasetFolder_GrantRoot_NotSkipped`, `DatasetFolder_Below300_NotSkipped`, `DatasetFolder_Uuid_IsMachineMade`, `DatasetFolder_TwelveDigits_IsMachineMade`, and `DatasetFolder_Cafe1234_IsNotMachineMade` lock it. Phase 4 waits on this task.
- **A.2.18** — `done` — A file whose bytes are not on this device is not downloaded and not indexed. The platform reports that. Stores: none. The signature is `LocalContentState ILocalContentProbe.Probe(string path)`. Windows, Apple, and Android report a file that is not on this device, and that file is not opened. `CloudFile_NotLocal_NotIndexed` locks the skip. Phase 4 does not wait on this task. Phases 1, 2, and 3 stay as written.

---

## Phase 4 — Pictures, video, and words in documents — `partial`

Vision waits on tasks A.2.2, A.2.3, A.2.4, A.2.7, A.2.8, A.2.10, A.2.11, A.2.12, A.2.13, A.2.14, and A.2.17. It does not wait on A.2.6, A.2.9, A.2.15, or A.2.16. A.2.15 gates task 6.2.1. A.2.11 and A.2.14 gate these tasks and task 6.2.1. Phase 4 is partial: the vision download and runtime, the media collections, picture search, frame cadence, frame collapse, and the OCR file pins are in code. `NeedsOcr` pages are stored in `ocr_page` and queued for pass 3; that queue runs `OcrPassStub` only and does not block audio. Real ONNX OCR for tasks 4.3.2 through 4.3.5 stays Epic 6.3.

### Epic 4.1 — Vision — `done`

- **4.1.1** — `done` — This task waits on A.2.2, A.2.3, A.2.4, A.2.7, A.2.8, A.2.10, A.2.11, A.2.12, A.2.13, A.2.14, and A.2.17. Download mmproj-embeddinggemma-2-Q8_0.gguf through in-process llama.cpp the first time a picture is indexed or used as a query. The catalog id is `embeddinggemma-2-vision`. The task is not done until `appsettings.json` names that file and its SHA-256, and the output width is 768. Unload it when the job ends. Do not keep it resident with the text encoder and the audio encoder. Done when a picture query before that download shows the waiting state, then succeeds after the download.
- **4.1.2** — `done` — This task waits on A.2.2, A.2.3, A.2.4, A.2.7, A.2.8, A.2.10, A.2.11, A.2.12, A.2.13, A.2.14, and A.2.17. Store pictures at 768 in `items_image`. The row has vector `embedding`, FTS field `words` using `KeywordFold.Tokens`, and scalar fields `path_live`, `encoder_hash`, `grant_path`, `start_seconds`, and `end_seconds`. A picture leaves `start_seconds` and `end_seconds` unset. A picture vector is not written into the text collection. Stores: that ZVec collection. Signature: `void ZVecMediaVectorIndex.Create(string collectionName, int width)`. A kill before the first successful insert leaves the collection empty. Done when a picture vector is absent from `items_text` and those fields exist on `items_image`. `PictureRow_NotInTextCollection` and `PictureRow_StartAndEndUnset` lock it.
- **4.1.3** — `done` — This task waits on A.2.2, A.2.3, A.2.4, A.2.7, A.2.8, A.2.10, A.2.11, A.2.12, A.2.13, A.2.14, and A.2.17. Find can take a picture that already sits inside a grant. The vision encoder embeds that picture. Dense search stays in the picture and video collections. A text query does not run this search. English `Search with a picture`. Arabic `ابحث بصورة`. Done when that control returns a picture hit and a text query does not. `PictureQuery_GrantedImage_HitsPictureCollection` locks it.

### Epic 4.2 — Video frames — `partial`

- **4.2.1** — `partial` — This task waits on A.2.2, A.2.3, A.2.4, A.2.7, A.2.8, A.2.10, A.2.11, A.2.12, A.2.13, A.2.14, and A.2.17. Sample one JPEG frame every two seconds with the platform sampler. Embed those frames with the vision encoder into the video collection at 768. The soundtrack is task 5.1.3. Android, iOS, and Mac Catalyst emit the two-second frames. On Windows build 22000 or newer, `MediaComposition.GetThumbnailsAsync` samples every two seconds. On an older build the sampler throws `NotSupportedException` with message `Windows frame thumbnails need Windows 11.` and does not call `GetThumbnailsAsync`. Done when a real Windows clip returns JPEG thumbnails at 0, 2, and 4 seconds and the cadence tests stay green. `FrameCadence_FiveSeconds_StartsAtZeroTwoFour` and `FrameCadence_ZeroDuration_IsEmpty` lock the cadence. AppleVideoFrameSampler and AndroidVideoFrameSampler stay. An Android emulator or an adb phone is the device run. A physical Apple run is not required.
- **4.2.2** — `done` — This task waits on A.2.2, A.2.3, A.2.4, A.2.7, A.2.8, A.2.10, A.2.11, A.2.12, A.2.13, A.2.14, and A.2.17. Frames of one video compete as one file. The card is the frame `Collapse.EarliestStart` keeps, the highest RRF score, and a tie keeps the earlier start. Top 20 counts files. Stores: `items_video`. Signature: `MediaSearchHit Collapse.EarliestStart(IReadOnlyList<MediaSearchHit> hits)`. A kill does not write in this task. Done when one video does not fill the 20 slots. `Frames_OneVideo_OneFileInTop20` and `Frames_OneVideo_KeepsMaxRrfScore` lock it.

### Epic 4.3 — OCR for English and Arabic — `partial`

Epic 6.3 delivers real ONNX sessions for these rows. The audio close only queues `ocr_page`, enqueues pass 3, and runs `OcrPassStub`; it does not mark 4.3.2 through 4.3.5 `done`.

- **4.3.2** — `not-started` — This task waits on A.2.2, A.2.3, A.2.4, A.2.7, A.2.8, A.2.10, A.2.11, A.2.12, A.2.13, A.2.14, and A.2.17. OCR a scanned page. OCR an image only for words printed in it. The vision vector is still stored. A photo with no words is found by the picture query in task 4.1.3. A text query does not return it. A scanned page is found by its words. Done when both of those are true.
- **4.3.3** — `partial` — This task waits on A.2.2, A.2.3, A.2.4, A.2.7, A.2.8, A.2.10, A.2.11, A.2.12, A.2.13, A.2.14, and A.2.17. Ship one shared text finder, the PP-OCRv6 small English ONNX reader, and the separate Arabic ONNX reader of about 8 MB. Both readers are on for v1. The three files, their Hugging Face bases, and their SHA-256 values are pinned in `OcrModelFiles`. No ONNX session is constructed. Done when an English page and an Arabic page each return their words.
- **4.3.4** — `not-started` — This task waits on A.2.2, A.2.3, A.2.4, A.2.7, A.2.8, A.2.10, A.2.11, A.2.12, A.2.13, A.2.14, and A.2.17. Any other language is off until the person turns it on, and that turn-on downloads only that reader. Done when Chinese weights are absent until Chinese is selected.
- **4.3.5** — `partial` — This task waits on A.2.2, A.2.3, A.2.4, A.2.7, A.2.8, A.2.10, A.2.11, A.2.12, A.2.13, A.2.14, and A.2.17. The shared finder and the English and Arabic readers unload when the OCR job ends. They are not kept resident with the audio encoder. A text-layer page does not load them. Done when the readers are unloaded after the job and are not loaded during an audio embed. `Scheduler_SwitchesModelOncePerPass` locks one `OcrPassStub` open per pass. There is no `Ocr_UnloadedAfterJob` test until Epic 6.3.

---

## Phase 5 — Audio and transcripts that already exist — `partial`

### Epic 5.1 — Audio files — `partial`

- **5.1.1** — `done` — If the audio file type is off, show one line and do not embed. If it is on, download the shared mmproj file `mmproj-embeddinggemma-2-Q8_0.gguf` through in-process llama.cpp, embed, store in `items_audio` at 768, then unload when the ingest pass ends. A lone `IndexAsync` call still closes audio after that file when it opened the session. The row uses the same fields as task 4.1.2: `embedding`, `words`, `path_live`, `encoder_hash`, `grant_path`, `start_seconds`, and `end_seconds`. The catalog id is `embeddinggemma-2-audio`. `appsettings.json` names that file and its SHA-256 `c4a8a52691ecef40618438928bdf9e68379b854e24166f292592353db0aab64f`, and the output width is 768. Stores: that ZVec collection. Signature: `void ZVecMediaVectorIndex.Create(string collectionName, int width)`. A kill before unload disposes the session in `finally`. Done when the encoder is not still loaded after the job, and there is no microphone control. `AudioOn_Wav_InsertsWindowsAndUnloads` and `AudioModelOptions_NamesMmprojAndHash` lock it.
- **5.1.2** — `done` — Do not add a speech-to-text model. Done when the solution contains no ASR package. The solution has no speech-to-text package.
- **5.1.3** — `partial` — When the audio file type is on, a video soundtrack and an audio file are overlapping windows of 11200 milliseconds. The hop is 5600 milliseconds, integer half of the native length. The second window starts at that hop. Each window stores a start and an end. A tail shorter than the native length extends the last window’s end to the file end and is not its own vector. Windows of one file compete as one card. The card is the window `Collapse.EarliestStart` keeps, the highest RRF score, and a tie keeps the earlier start. Unload the vision session before the audio session loads when `MediaContextPolicy.OneContext` is false. Frames stay the one JPEG every two seconds from task 4.2.1. A 16-bit PCM WAV is read by `WavPcm`. Compressed audio and a video soundtrack are decoded by `MediaExtractor` and `MediaCodec`, `AVAssetReader`, or `MediaTranscoder`, then resampled to 16 kHz mono PCM. Stores: `items_audio` and `items_video`. Signature: `MediaSearchHit Collapse.EarliestStart(IReadOnlyList<MediaSearchHit> hits)`. A kill before unload disposes the session in `finally`. Done when a real compressed file and a real video soundtrack on Android and on Windows each produce 16 kHz windows. Android may be an emulator or the phone over adb. AppleAudioSource stays. A physical Apple run is not required. `Windows_HopIsHalfNative_SecondStartsAtHalf`, `Windows_ShortTail_ExtendsLastWindow`, `Windows_OneFile_KeepsMaxRrfScore`, and `PlatformAudio_DecodersAreSourceLocked` lock the plan. `CompressedAudio_NotDecoded_DoesNotEmbed` locks the failure branch only.
- **5.1.4** — `done` — Find can take an audio file that sits inside a grant. The audio encoder embeds that file. Dense search stays in the audio collection. There is no microphone control. English `Search with an audio file`. Arabic `ابحث بملف صوتي`. Done when that control searches audio only. `AudioQuery_StaysInsideGrant`, `AudioQuery_NoAudioGrant_ReturnsEmpty`, and `HomeRazor_HasAudioControlWithoutMicrophone` lock it.

### Epic 5.2 — Sibling transcript files — `partial`

- **5.2.1** — `done` — If `talk.mp4` or `talk.m4a` has `talk.srt`, `talk.vtt`, `talk.sbv`, `talk.ass`, `talk.ssa`, `talk.ttml`, or a language tag such as `talk.en.vtt`, attach that text to the media item. Done when a search for those words opens the media file. `TranscriptWords_OpenMediaFile` locks it.
- **5.2.2** — `done` — Do not also index that sibling as its own unlinked document. Do not read a caption track hidden inside the media file. Done when both of those stays are tested. `Caption_Srt_IsSkippedBeforeDecoding`, `EmbeddedCaption_NotRead`, and `TranscriptWords_OpenMediaFile` lock it.
- **5.2.3** — `done` — When `talk.en.vtt` and `talk.ar.vtt` both exist, both texts are kept. Order is the UTF-8 byte order of the file name. Neither file is dropped. Neither file is its own document. Done when joined sibling text keeps both languages in that order on the media row. `TranscriptWords_MultipleLanguages_AppendInNameOrder` locks it.
- **5.2.4** — `partial` — Opening an audio or video card seeks to the start stored on the winning row, in seconds. A frame row uses the frame time. An audio-window row uses the window start. The two are not averaged. File collapse keeps that one row’s start. A text card opens the file. A PDF card is task 5.3.1. Stores: `start_seconds` on the media row. Signature: `double MediaHitStart.Seconds(TextSearchHit hit)`. A kill does not write. Done when `CommunityToolkit.Maui.MediaElement` on a real file seeks to the stored start. `OpenHit_SeeksToWindowStart`, `OpenHit_UsesWinningRowStart`, and `OpenHit_StartFour_PlaysAtFour` lock the numbers. A launcher path still ignores the start.

### Epic 5.3 — PDF text layer — `partial`

- **5.3.1** — `partial` — A text-layer PDF is read with PdfPig 0.1.16 through `ContentOrderTextExtractor.GetText`, then `PdfTextOrder` restores Arabic presentation forms stored left to right into logical base letters, patched by `DocumentPatchPlanner` at target 2048 tokens, lookback 512, overlap 256 to 384 starting at a sentence, cut snapped to a paragraph then a sentence then whitespace, hard cap 7680, and stored on `ingest_chunk` with `page_start`, `page_end`, `char_start`, and `char_end`. The stored text encoder hash is the file SHA-256 plus `:patch-v3`. There is no 10-page stop and no `document_patch` table. A `NeedsOcr` page is queued in `ocr_page` and is not embedded. `NeedsOcr` is true when the word contains at least one letter of Unicode script Arabic and a run of 3 or more U+FFFD. A missing page opens at page 1 through `DocumentPatchPlanner.OpenPage`. Done when a real PDF produces patches on `ingest_chunk` with a boundary cut near 2048 tokens and sentence-start overlap. `EndsPatch_At7680_IsTrue`, `Patch_Targets2048`, `SplitPdfPages_NeedsOcr_OmitsPageFromJoin`, and the other `Phase5PdfTests` locks stay green without a real file.

---

## Phase 6 — Age, pins, and OCR — `partial`

Task 6.2.1 waits on A.2.11, A.2.14, and A.2.15. Task 6.1.2 does not. Epic 6.3 waits on nothing in this phase.

### Epic 6.1 — Move old pictures, video, and audio — `partial`

- **6.1.1** — `done` — Read the person’s choice of 2, 5, or 7 years. Default is 2. Done when any other number is rejected. Preferences stores 2, 5, or 7, and another number throws. Shrink of old media is task 6.1.2 and is partial in the tree.
- **6.1.2** — `partial` — This task waits on A.2.10. File age is the UTC last-write time. A missing or unreadable time does not move the row. A last-write time that becomes younger does not move a short row back to long. Only task 6.1.3 regrows. When a picture, video, or audio file is older than the person’s choice, cut its 768 vector to 512 only after that encoder’s recall gate passes. Text is not in the probe and never moves. The runner is `AgeCut_RecallAt10_WithinFiveOfFullWidth`. `Passes` cuts every stored vector and every query and returns whether Recall@10 at 512 is within 5 points of Recall@10 at 768. The 32×32 PNG and pure-tone set is not a passing corpus. This task does not name a replacement corpus. A missing corpus fails closed and no row is cut. If the encoder file is absent, the gate fails closed and no row is cut. If the gate fails, the row stays at 768. Picture, video, and audio each have their own old collection. The short content id differs because width is part of the id. Order: insert the short row, then in one `checkpoints.db` transaction `MoveLiveAliases` copies each live alias from the long id to the short id and retires the long id’s aliases, then `TryDelete` the long row. That delete is not a source tombstone. The short row stores `encoder_hash`. Stores: `items_image`, `items_video`, `items_audio`, the matching `_old` collection, and `ingest_chunk_path`. Signature: `bool AgeCutGate.Passes(Modality modality, IReadOnlyList<AgeCutItem> items)` where `AgeCutItem` is `(int Id, float[] Stored768, float[] Query768)`, and `void IIngestCursorStore.MoveLiveAliases(string fromContentId, string toContentId)`. A kill before that transaction commits leaves the aliases on the long id. A kill after the commit and before `TryDelete` leaves the aliases on the short id and both rows. Resume deletes the long row. Done when only one copy remains, the live aliases point at the short id, a younger time leaves a short row short, a missing time leaves the long row, and a failed or missing corpus leaves the long row. `AgeMove_CopiesAliasBeforeLongDelete`, `AgeMove_YoungerTime_StaysShort`, `AgeMove_MissingTime_StaysLong`, `AgeMove_RecallGateFails_Stays768`, `AgeMove_SyntheticNearDup_DoesNotPass`, and `AgeMove_DeletesLongRow_OneOldCollectionPerEncoder` lock it. Production registers `FailClosedAgeCutDecision` and `AgeCutCorpus.TryLoad` returns false until a real corpus ships, so no row is cut on device.
- **6.1.3** — `partial` — A pin stays at 768. Growing a shortened item back embeds the file again when the file is present, writes the long row, and `TryDelete`s the short row. A missing file is not re-embedded and stays in the collection it is already in. Unpin of a file whose UTC last-write is past the cutoff writes the short row and deletes the long row. Unpin of a young file stays long. Stores: the long collection and the matching `_old` collection. Signature: `PinResult PinService.Unpin(string path)`. A kill after the long insert and before the short delete leaves both rows. Resume deletes the short row when the pin is still on. Done when a present pinned photo returns to the long collection, a missing file stays, and unpin of an old file returns it to the short collection. `PinRegrow_MissingFile_DoesNotEmbed` and `Unpin_OldFile_ReturnsShort` lock it. `PinService`, `ingest_pin`, and `pin_resume` are in the tree. Find has no pin control. Unpin of an old file returns `GateFailedStaysLong` when the recall gate fails closed.

### Epic 6.2 — Search across both ages — `done`

- **6.2.1** — `done` — This task waited on A.2.11, A.2.14, and A.2.15. Dense search stays per encoder. Inside one encoder, embed once and cut to the long width and the old width. A text vector is not a picture vector or an audio vector. A text query’s dense search stays in text. Its hit in a picture, video, or audio row is BM25 only. A picture or audio file with no matching words is absent from that text query. A picture query is task 4.1.3 and stays in picture and video. An audio query is task 5.1.4 and stays in audio. Those dense lists are not fused with the text dense list. OCR words, the sibling transcript, and keywords stay on that media row, in that order, joined by a newline. They are not copied into `items_text`. There is no per-source weight. Long and old lists use the same unweighted RRF. That equal weight is the age trade once the task 6.1.2 gate has passed. There is no weighted RRF in this plan. Search order is the task A.2.11 pipeline: filters inside the query, at most 64 on each raw list, one RRF at `k=60`, one hit per file, at most 20 files. A row whose encoder-file hash differs stays out until task A.2.4 has finished the repair. Done when both ages of one encoder return one list, a text query misses a wordless picture, and the words on a scanned page or on `talk.srt` open that media file. `TextQuery_WordlessPicture_IsAbsent`, `PictureQuery_EmbedsOnce_SearchesLongAndOld`, `AudioQuery_EmbedsOnce_SearchesLongAndOld`, and `Fuse_EqualWeight_TieKeepsEarlierStart` lock it.

### Epic 6.3 — OCR runtime and ingest order — `partial`

This epic delivers tasks 4.3.2 through 4.3.5 with real sessions. The Epic 4.3 text is not changed. When 6.3.1 and 6.3.3 are `done`, tasks 4.3.2, 4.3.3, and 4.3.5 are set `done`. Task 4.3.4 is met because no Chinese, Korean, Japanese, or Indic reader is shipped. OCR scripts are Arabic, English, and European Latin only. Epic 6.3 does not wait on Epic 6.1 or Epic 6.2.

- **6.3.1** — `not-started` — Real OCR. Add NuGet `Microsoft.ML.OnnxRuntime` 1.31.0. Replace the bool-only `OcrSessionSet` with three `InferenceSession`s: the pinned PP-OCRv6 small detector, the pinned PP-OCRv6 small reader for English and European Latin, and the pinned Arabic PP-OCRv5 mobile reader. All three load together at the start of one OCR pass and are disposed once in `finally` at its end. No reader is loaded or unloaded per page or per line. Inputs: pages in `ocr_page`, image-only PDF pages rendered at 300 DPI by the OS renderer (Windows.Data.Pdf, Android PdfRenderer, Apple PDFKit), and images in which the detector finds words. A physical device run is not required for the renderer. Routing: for each page, read the first 3 detected lines with both readers and keep the reader with the higher mean confidence as the page reader. Every other line goes to the page reader. A line below 0.6 confidence is also read by the other reader, and the higher confidence wins. Reading order: Arabic lines right to left, Latin lines left to right, and a mixed line by box x within the line direction. The OCR text goes through `DocumentPatchPlanner` like any text and is stored on that file's rows with page spans. If French, German, or Spanish accents fail task 6.3.3, the Latin reader is replaced by the pinned `latin_PP-OCRv5_mobile_rec` and this task stays open. Done when a real scanned Arabic page, a real scanned English page, and a mixed Arabic and English page are each found by their words. `OcrPass_LoadsOnce_ForManyPages`, `OcrRoute_ArabicPage_UsesArabicReader`, `OcrRoute_LowConfidenceLine_TriesOtherReader`, and `OcrOrder_MixedLine_ArabicRightToLeft` lock it. The five fixture pages measured on 26 Oct 2026 are accepted at the edit ceilings in `OcrFixtureReference`. Those ceilings do not replace this task and do not replace the pinned readers.
- **6.3.2** — `done` — Ingest by model. One ingest scheduler groups pending files by the model they need and runs passes in this order: text model (notes, code, text-layer PDFs), media context (pictures, video frames, audio windows), then OCR. Each model opens once per pass, drains its queue, and closes in `finally`. Watcher events go to the queue, not straight to a model. A pass opens when its queue reaches 20 files or 30 seconds after the last event. A model is never opened twice inside one pass. Pass 3 runs `OcrPassStub` until Epic 6.3 replaces it with ONNX. Done when a mixed grant of text, pictures, audio, and scanned pages opens each model once, and a burst of 50 watcher events opens one pass. `Scheduler_SwitchesModelOncePerPass` and `Scheduler_BurstOf50Events_OneOpen` lock it.
- **6.3.3** — `not-started` — OCR quality gate on real pages. Golden set: 30 Arabic, 30 English, and 20 French, German, and Spanish pages, including phone photos and mixed Arabic and English pages, each with a ground-truth text file outside the indexed folders. Character error rate (CER) is at most 5% on clean scans and at most 10% on phone photos for each script, and Arabic word order is right on every mixed line. These limits are starting values and may be tuned once from the first run, with the change written here. Stores: the OCR golden report. Done when the report passes. `OcrGolden_OverLimit_Fails` locks the fail. The five fixture pages are a separate accepted measurement and do not close this task.

---

## Phase 7 — Proof and a local build — `partial`

### Epic 7.1 — Tests that can fail — `partial`

- **7.1.1** — `partial` — Unit-test every method and every branch that decides behavior for the ports and policies above, with fakes for the text embedder and ZVec. Done when deleting a branch makes a test go red. A coverage percent of 60% then 70% is a floor, not the exit. Unit tests cover the text prefix, chunk split, download resume rules, grant boundary, width 128, age cutoff, and device id. Phase 3 branches now have tests; later phases still do not.
- **7.1.2** — `partial` — Integration-test the llama.cpp text encoder plus real ZVec: crash restart from `checkpoints.db`, and zero non-local bytes after the model download has finished. The unit cursor locks stay task 3.3.1. This task is the kill-and-restart run, not a second copy of those locks. Stores: the local proxy log. Signature: `Task<bool> IEgressProbe.HasNonLocalByteAsync(CancellationToken cancellationToken)`. The probe watches for 60 minutes after the download has finished. A missing proxy fails the run. Done when crash restart resumes and the probe returns false. A text-search integration test is in the tree. `CrashRestart_ResumesFromCheckpoint` and the egress parser tests are in the tree. A log the test writes is not a 60-minute traffic record, so this task stays partial. `Egress_AfterDownload_ZeroNonLocalBytes` locks the parser, not that record.
- **7.1.3** — `not-started` — Golden retrieval in English, Arabic, French, German, and Spanish. The spread is text queries against text. Recall@10 for the same query stays within 5 points across those five. A spread wider than 5 points fails the task. A French, German, or Spanish miss inside those 5 points is a result of that run. No Chinese, Korean, Japanese, or Indic language is in this gate. This task does not add a stemmer for any language. BM25 uses the folding in task A.2.14. The set also has one OCR'd Arabic page and one OCR'd English page, and each must be in the top 10 for a query of its words. A wordless photo is not in this text gate. Text stays at 512 unless this run clears 256. Clearing 256 is not required for this task to pass. Stores: the golden report. Signature: the report's Recall@10 for each of the five languages on the same query, plus the two OCR page ranks. A missing language fails the run. Done when the report shows the spread within 5 points and both OCR pages in the top 10. UI languages stay Arabic and English either way. `GoldenSpread_OverFivePoints_Fails` and `GoldenOcrPage_NotInTop10_Fails` lock the fail.

### Epic 7.2 — Local release — `partial`

- **7.2.1** — `done` — Produce the Windows shell on this machine and the Android shell on an emulator or the phone over adb. Either Android target passes. Linux proof is dotnet test of tests/Recall.UnitTests in WSL, or in the SDK container if WSL is absent. The Apple heads stay in the project. apple-compile.yml compiles them. A physical install is not required. linux-x64 has no shell. The package contains no .gguf and no .onnx. First-run download stays task 2.1.1. Done when the Windows publish folder and the apk have no weight file, adb install succeeds, and the Linux test command exits 0. `WinX64Publish_ContainsNoWeight`, `AndroidApk_ContainsNoWeight`, adb install, and the WSL unit-test command passed on 24 Oct 2026.
- **7.2.2** — `done` — Leave crash reports on the device. Done when no crash package is referenced. `CrashPackage_IsNotReferenced` locks it.
- **7.2.3** — `done` — Windows, Mac Catalyst, Android, and iOS tokenize and embed through recall_embed in-process. No Tokenizers.DotNet package and no GemmaTextTokenizer. `Embed_LocalGguf_Is768AndUnit` locks a 768-wide unit vector when the GGUF is present. Done when those four shells share that library.
- **7.2.4** — `not-started` — A later `libzvec_c_api.so` may be rebuilt at 16 KB page alignment. ZVec.NET 1.0.0-beta.6 measures four load segments at 4096. Android reports that as warning XA0141. The warning does not block install or search. The same library runs in `ZVec.NET.Samples.Maui` and in `ZVec.Net-DemosAndPOCs`. `Libzvec_LoadAlign_Is4096` locks the current measurement. This task does not hold 7.2.1. Done when a later package measures at least 16384.

---

## Launch Addendum — Public proof and growth — `partial`

This addendum is written after Phases 1–7 and does not edit any of them. No sentence above is rewritten, no status above moves, and the progress table gains rows only. It takes the public repository from a pushed source tree to a distribution surface: a story, installable artifacts, reach, and stated trust rules.

Rules for every task:

- The only workflow is apple-compile.yml. It compiles the Apple heads. Windows, Android, and Linux proofs stay on the machines named in the stack paragraph. Distribution is a published artifact, not a cloud build other than that compile workflow.
- The app adds no network path. The model download stays the only one, so there is no self-updater, no update check, and no telemetry. Updates are new downloads the person chooses; package managers surface them.
- No task is done by a star count. A star number measures how reach went after execution and is never a check.
- Comparison tables that name a competing product are not written anywhere in this repository. The text states the differentiators themselves: phones, Arabic end to end, in-process embedding, the single network path, and the public per-task plan.
- Every sentence a person reads in tree copy is true on the day it is published. A README row for capability that is not in the tree, or an installer that is not in a release, fails this addendum.

### Epic P.1 — Story — `partial`

- **P.1.1** — `done` — README rewrite. The first line under the title is the value proposition: **Find what's in your files by meaning. Entirely on your device, in Arabic and English.** Keep "What it can do" and "What v1 will do" as the honest split. Add "Why Recall": Arabic and English end to end with right-to-left layout and an Arabic OCR reader; embedding runs inside the app's process with no helper process and no extra daemon; the model download is the only network call and carries no document, no vector, and no device id; one public plan shows per-task status. Add "Install and updates": it names only artifacts that exist in a release on the day of the sentence, states which task of this plan publishes the next artifact, and ends with the sentence **Updates are new downloads. Recall checks for nothing.** No benchmark number appears unless its source run and its machine label are written next to it; the Phase 2 desktop runs stay labeled the way task 2.2.5 labels them or stay out. Done when a number without a named source, or an installer row without a published release, fails a manual audit of the file.
- **P.1.2** — `not-started` — Demo recording. One continuous screen recording of a real Find against a real granted folder, no staged result and no editor cut that changes a query, 30 seconds or less, exported as `docs/media/demo-text-search.gif` and embedded at the top of the README. A second recording with a picture or video Find is added when Phase 4 lands. Done when the GIF exists in the tree, is 30 seconds or less, and the README embeds it.
- **P.1.3** — `not-started` — The repo face off the tree: the About line reads as the value proposition, not as a spec, the topic list names the search neighborhoods a reader looks in (`semantic-search`, `on-device`, `local-first`, `embeddings`, `embeddinggemma`, `llama-cpp`, `maui`, `dotnet`, `arabic`, `rtl`, `android`, `ios`), and one pinned Discussion holds the story so it is not buried in commit noise. Done when those three are set on the repository site.

### Epic P.2 — Installable proof — `not-started`

- **P.2.1** — `not-started` — First release, `v0.1.0`: one Windows artifact `Recall-v0.1.0-win-x64.zip`, a self-contained win-x64 build that contains no `.gguf` and no `.onnx` weight, and downloads the text model on first run. The release notes follow the template: **What this does** (grant folders, text and code search, open a hit, watch while running, crash-cursor resume), **What this does not yet do** (pictures, OCR, video, audio, pins, Mac and iOS installers), **Checksums** (SHA-256 per artifact), **Updates are new downloads. Recall checks for nothing.** Done when the release is public and its notes contain those four headings verbatim.
- **P.2.2** — `not-started` — Android artifact added to releases: `Recall-vX.Y.Z-android-arm64.apk`, with first-run download as 2.1.1. The 16 KB page warning does not gate this artifact. Done when the apk installs on a device from a release page and the notes row exists.
- **P.2.3** — `not-started` — Mac artifact once 7.2.1 passes: an unsigned Mac Catalyst `.app` inside a `.zip`. An Apple-signed installer and any iOS distribution are separate decisions a later plan takes; they are not here. Done when the zip is attached to a release.
- **P.2.4** — `not-started` — Package-manager upgrade path, after P.2.1: a manifest submitted once to the Windows community package repository for identity `Recall.read`, pointing at the published asset and its SHA-256, updated one manifest per release; and `.github/homebrew/recall.rb` written only when a Mac artifact exists. Upgrades are commands the person runs; nothing checks automatically. Done when `winget upgrade Recall.read` shows a published version, or, for Homebrew, `brew upgrade --cask recall` installs the published artifact.
- **P.2.5** — `not-started` — Version 1.0.0 release gate. `v1.0.0` is not published until tasks 6.3.1, 6.3.2, and 6.3.3 are `done` and no production type in the tree is a fake or a bool-only stand-in. `v0.1.0` in task P.2.1 may ship without OCR when its notes say OCR is not in that build. Done when the `v1.0.0` notes link the OCR golden report.

### Epic P.3 — Reach — `partial`

- **P.3.1** — `partial` — Launch posts live in the tree: `docs/launch-posts.md` holds a Show HN draft, a r/LocalLLaMA-style forum draft, and two short posts in English and Arabic for an X thread. Each draft leads with what the product is, states the limits in the same breath as the claims, names no other product, and quotes no number that the tree cannot source. The P.2.1 release notes take their first sentence from the Show HN draft's opening. Done when P.2.1 exists with that first sentence and the drafts match the published text.
- **P.3.2** — `done` — Contribution surface: `CONTRIBUTING.md` and `.github/ISSUE_TEMPLATE/bug_report.yml` / `feature_request.yml`. A bug report asks for platform and version; a feature request is accepted only when it names a task id of `docs/plan.md` or argues for a new task in that plan; a pull request carries the task id and the honest test rule from `docs/testing.md`. Done when those templates exist, a contribution flow names the plan as the task list, and no issue template links a behavior the tree does not have.
- **P.3.3** — `done` — README Roadmap section links `docs/plan.md` directly at the progress table so status is readable per task without opening the repo. Done when the link is on the README.

### Epic P.4 — Trust — `done`

- **P.4.1** — `done` — No growth machinery inside the app: this addendum adds no updater, no counter, no crash reporter, and modifies no `csproj` or `BannedSymbols.txt` list. The README privacy sentences are no stronger and no weaker than `docs/privacy.md`. The egress guarantee is still task 7.1.2's, and this addendum claims none of it. Done when the diff adds only Markdown, YAML, and one navigation line, and no byte of app code changes.

---

## Progress

Counted 26 Oct 2026. This is the only progress table.

| | Status | Percent |
|---|---|---|
| **Phase 1 — Shell** | `done` | 100 |
| **Epic 1.1 — Solution** | `done` | 100 |
| 1.1.1 | `done` | 100 |
| 1.1.2 | `done` | 100 |
| 1.1.3 | `done` | 100 |
| **Epic 1.2 — Screens that can ship** | `done` | 100 |
| 1.2.1 | `done` | 100 |
| 1.2.2 | `done` | 100 |
| 1.2.3 | `done` | 100 |
| **Epic 1.3 — This device** | `done` | 100 |
| 1.3.1 | `done` | 100 |
| 1.3.2 | `done` | 100 |
| **Phase 2 — Text search** | `done` | 100 |
| **Epic 2.1 — Model download** | `done` | 100 |
| 2.1.1 | `done` | 100 |
| 2.1.2 | `done` | 100 |
| 2.1.3 | `done` | 100 |
| **Epic 2.2 — Text embed and index** | `done` | 100 |
| 2.2.1 | `done` | 100 |
| 2.2.2 | `done` | 100 |
| 2.2.3 | `done` | 100 |
| 2.2.4 | `done` | 100 |
| 2.2.5 | `done` | 100 |
| 2.2.6 | `done` | 100 |
| **Epic 2.3 — Query text** | `done` | 100 |
| 2.3.1 | `done` | 100 |
| 2.3.2 | `done` | 100 |
| **Phase 3 — Folders that stay current** | `done` | 100 |
| **Epic 3.1 — Grants** | `done` | 100 |
| 3.1.1 | `done` | 100 |
| 3.1.2 | `done` | 100 |
| **Epic 3.2 — Watch and scan** | `done` | 100 |
| 3.2.1 | `done` | 100 |
| 3.2.2 | `done` | 100 |
| 3.2.3 | `done` | 100 |
| **Epic 3.3 — Crash cursor** | `done` | 100 |
| 3.3.1 | `done` | 100 |
| **Phase Three Addendum — Gaps left by Phases 1–3** | `done` | 100 |
| **Epic A.2 — Closures** | `done` | 100 |
| A.2.2 | `done` | 100 |
| A.2.3 | `done` | 100 |
| A.2.4 | `done` | 100 |
| A.2.5 | `done` | 100 |
| A.2.6 | `done` | 100 |
| A.2.7 | `done` | 100 |
| A.2.8 | `done` | 100 |
| A.2.9 | `done` | 100 |
| A.2.10 | `done` | 100 |
| A.2.11 | `done` | 100 |
| A.2.12 | `done` | 100 |
| A.2.13 | `done` | 100 |
| A.2.14 | `done` | 100 |
| A.2.15 | `done` | 100 |
| A.2.16 | `done` | 100 |
| A.2.17 | `done` | 100 |
| A.2.18 | `done` | 100 |
| **Phase 4 — Pictures, video, and words in documents** | `partial` | 61 |
| **Epic 4.1 — Vision** | `done` | 100 |
| 4.1.1 | `done` | 100 |
| 4.1.2 | `done` | 100 |
| 4.1.3 | `done` | 100 |
| **Epic 4.2 — Video frames** | `partial` | 75 |
| 4.2.1 | `partial` | 50 |
| 4.2.2 | `done` | 100 |
| **Epic 4.3 — OCR for English and Arabic** | `partial` | 25 |
| 4.3.2 | `not-started` | 0 |
| 4.3.3 | `partial` | 50 |
| 4.3.4 | `not-started` | 0 |
| 4.3.5 | `partial` | 50 |
| **Phase 5 — Audio and transcripts that already exist** | `partial` | 83 |
| **Epic 5.1 — Audio files** | `partial` | 88 |
| 5.1.1 | `done` | 100 |
| 5.1.2 | `done` | 100 |
| 5.1.3 | `partial` | 50 |
| 5.1.4 | `done` | 100 |
| **Epic 5.2 — Sibling transcript files** | `partial` | 88 |
| 5.2.1 | `done` | 100 |
| 5.2.2 | `done` | 100 |
| 5.2.3 | `done` | 100 |
| 5.2.4 | `partial` | 50 |
| **Epic 5.3 — PDF text layer** | `partial` | 50 |
| 5.3.1 | `partial` | 50 |
| **Phase 6 — Age, pins, and OCR** | `partial` | 57 |
| **Epic 6.1 — Move old pictures, video, and audio** | `partial` | 67 |
| 6.1.1 | `done` | 100 |
| 6.1.2 | `partial` | 50 |
| 6.1.3 | `partial` | 50 |
| **Epic 6.2 — Search across both ages** | `done` | 100 |
| 6.2.1 | `done` | 100 |
| **Epic 6.3 — OCR runtime and ingest order** | `partial` | 33 |
| 6.3.1 | `not-started` | 0 |
| 6.3.2 | `done` | 100 |
| 6.3.3 | `not-started` | 0 |
| **Phase 7 — Proof and a local build** | `partial` | 57 |
| **Epic 7.1 — Tests that can fail** | `partial` | 33 |
| 7.1.1 | `partial` | 50 |
| 7.1.2 | `partial` | 50 |
| 7.1.3 | `not-started` | 0 |
| **Epic 7.2 — Local release** | `partial` | 75 |
| 7.2.1 | `done` | 100 |
| 7.2.2 | `done` | 100 |
| 7.2.3 | `done` | 100 |
| 7.2.4 | `not-started` | 0 |
| **Launch Addendum — Public proof and growth** | `partial` | 38 |
| **Epic P.1 — Story** | `partial` | 33 |
| P.1.1 | `done` | 100 |
| P.1.2 | `not-started` | 0 |
| P.1.3 | `not-started` | 0 |
| **Epic P.2 — Installable proof** | `not-started` | 0 |
| P.2.1 | `not-started` | 0 |
| P.2.2 | `not-started` | 0 |
| P.2.3 | `not-started` | 0 |
| P.2.4 | `not-started` | 0 |
| P.2.5 | `not-started` | 0 |
| **Epic P.3 — Reach** | `partial` | 83 |
| P.3.1 | `partial` | 50 |
| P.3.2 | `done` | 100 |
| P.3.3 | `done` | 100 |
| **Epic P.4 — Trust** | `done` | 100 |
| P.4.1 | `done` | 100 |
| **All epics** | `partial` | 78 |

## Behavior matrix

Find after the operation. One card per live path. At most 20 files (`RecallIndexCaps.FileCap`). A shared id stays visible while any path is live. A text query lists dense text hits first, then appends transcript-word hits from `items_audio` and `items_video`, then `Take(20)`, so a mixed grant can show only text cards when text already fills the cap. Deleting an audio or video file retires its `ingest_chunk` rows and sets `path_live` to 0 on the vector rows.

| | Create | Edit | Shrink | Delete | Rename | Age | Pin | Repair |
|---|---|---|---|---|---|---|---|---|
| Text | New ids at 512. The file is a hit. | Old ids go away when no other path is live. New ids stay at 512. | Same as edit. `DELETE` only for an id with no live path. | Vector stays. That path is hidden. | Same hash updates the alias. The new path is the hit. No second embed. | Text does not move. | Width stays 512. | `Reading`, delete, same id, `Finished`. The new vector is a hit only after `Finished`. |
| Picture | One 768 row in `items_image`. `start_seconds` and `end_seconds` stay unset. | A short row stays short unless pinned. A young long row stays long. A long row past the cutoff is written short. | One vector. Byte change follows edit. | Vector stays. The path is hidden. | Alias only. Same row. | UTC last-write. Aliases move to the short id before the long row is deleted. A younger time does not regrow. The recall gate has no corpus in production, so the row stays long until one ships. A failed gate leaves the row long. | Present file returns to 768. Unpin of an old file writes short and deletes long when the gate passes; otherwise it stays long. | Same `Reading` rule on that collection. |
| Video | Frames at 768. One card, the highest RRF score. Seek is that frame’s start. | Same width rule as a picture. Dropped frame ids follow the shared-path delete rule. | Fewer frames follow edit. | Frames stay. The path is hidden. | Alias only. | All frames of the file move together. Each frame’s aliases move to its short id before the long row is deleted. The recall gate has no corpus in production, so frames stay long until one ships. | All frames stay long while pinned. Unpin of an old file moves them together when the gate passes. | One repair per frame id. |
| Audio | Windows at 768. One card, the highest RRF score. Seek is that window’s start. | Same width rule. A short tail extends the last window. | Fewer windows follow edit. | Chunk rows retire through the alias path and `path_live` goes to 0. The path is hidden. | Alias only. | All windows move together. Each window’s aliases move to its short id before the long row is deleted. The recall gate has no corpus in production, so windows stay long until one ships. | Same as video. | One repair per window id. |

A text query does not dense-search picture, video, or audio. Transcript words on `items_audio` and `items_video` append after the text hits, then the combined list is cut at `RecallIndexCaps.FileCap` (20). Text hits already in the list are not duplicated. A wordless picture is absent from that query. A picture query stays in picture and video. An audio query stays in audio. One card keeps the highest RRF score. A tie keeps the earlier ordinal or start. Two paths with the same bytes are two cards, in ordinal path order. A digits-only non-code span is in the BM25 list and not in HNSW. `Bm25OnlyRank.Merge` appends those hits after the ZVec BM25 ranks. A code file stays dense. A skipped dataset folder has no hit. Phase 4 is partial: vision, pictures, video frames, and the OCR file pins are in code; `ocr_page` and pass 3 use `OcrPassStub` only. Real ONNX OCR is Epic 6.3. Find reconciles at most 64 pending ids.

# Version 1.5

## Phase 1 — Sections — `not-started`

The 8192 budget and the title prefix stay. A heading, a table, and a fence are read as structure.

### Epic 1.1 — Structure split — `not-started`

- **1.1.1** — `not-started` — Split a long note on a Markdown heading, a fenced block kept whole when it fits, a pipe table kept whole when it fits, an HTML heading, a whole HTML table, `pre`, or `code` when it fits, and a PDF page. The breadcrumb is the heading path inside `title: {file name} | text:`. A unit larger than 8192 tokens uses the rune cut and repeats the breadcrumb. A code file splits on a blank line inside the budget, otherwise the rune cut. Chunks are not averaged. Done when a fenced block that fits is one piece and a plain note still uses the rune cut.
- **1.1.2** — `not-started` — Show that heading path on the Find card. Done when an empty heading draws no extra line, in Arabic and English.

### Epic 1.2 — Links inside a grant — `not-started`

- **1.2.1** — `not-started` — Keep a Markdown or HTML link that points at another file inside the grant, and show at most three of those names on the card. Drop `http` and `https`. Done when a link outside the grant is absent from the card.

## Phase 2 — File details — `not-started`

Picture, video, and audio ingest must already exist. This phase does not add a camera.

### Epic 2.1 — Details already in the file — `not-started`

- **2.1.1** — `not-started` — Read capture time, camera make, model, lens, ISO, and keywords already stored in a granted file. Keywords join that row’s word search and are not a separate dense vector. Done when a file with no keywords adds no extra vector.
- **2.1.2** — `not-started` — `FileDetails.KeepPlace` defaults false. Latitude and longitude are stored only while that switch is on. Recall does not send the place to a server. Done when the switch starts off and a place value is absent from the model-download request. English `Keep place from the file`. Arabic `احتفظ بالمكان من الملف`.

## Phase 3 — People — `not-started`

EmbeddingGemma does not identify a person. The switch `People.FindSameFace` defaults false. No camera-roll capture.

### Epic 3.1 — Same face — `not-started`

- **3.1.1** — `not-started` — Suggest a shared face only inside a granted folder, and only after the person turns the switch on. The detector candidate is OpenCV YuNet. Video uses the existing one JPEG every two seconds, and at most five faces on a frame. Done when the switch starts off and a folder that was not granted is not scanned.
- **3.1.2** — `not-started` — Store a face vector only after an Apache-compatible ONNX file at 256 or 512 numbers is named and hashed. A stored face vector at 128 numbers is forbidden. InsightFace buffalo weights are not a dependency. The same-face threshold stays unmeasured until that file exists. Done when a 128-wide store throws and no buffalo file is referenced.
- **3.1.3** — `not-started` — A cluster id is a local GUID the screen never shows and never sends. The person types the name, and search uses that name. English `Name this person`. Arabic `سمّ هذا الشخص`. Done when search for that name returns the granted photos in the cluster and the GUID is absent from the screen.

## Phase 4 — Notes — `not-started`

v1 still has no language model. Group labels stay the deterministic TF-IDF label.

### Epic 4.1 — One local model — `not-started`

- **4.1.1** — `not-started` — `Notes.Enabled` defaults false. The download is one local ONNX file generated for ONNX Runtime GenAI, checked with SHA-256, from `Qwen/Qwen3-0.6B` after that pinned commit’s license file says Apache-2.0. The switch stays off on a shell whose generative library does not load, including iPhone while GenAI lists iOS as under development. The model is unloaded before a text, vision, or audio encoder loads. LiteGraph is not a dependency. Done when notes off means no generative file is loaded, and the download request carries no document, vector, face template, or place. English `Notes are off`. Arabic `الملاحظات متوقفة`. English `Notes are not available on this iPhone yet`. Arabic `الملاحظات غير متاحة على هذا الآيفون بعد`.

### Epic 4.2 — Entity rows — `not-started`

- **4.2.1** — `not-started` — When notes are on, entity and relation rows live in the local SQLite database and merge as a changed file arrives. Search still returns when notes are off. Done when a notes-off search uses the existing hybrid path and does not call the generative library.
- **4.2.2** — `not-started` — The notes model does not write group labels. Done when a group label is still the strongest word, the date range, and the counts.

## Progress

| | Status |
|---|---|
| **Phase 1 — Sections** | `not-started` |
| **Epic 1.1 — Structure split** | `not-started` |
| 1.1.1 | `not-started` |
| 1.1.2 | `not-started` |
| **Epic 1.2 — Links inside a grant** | `not-started` |
| 1.2.1 | `not-started` |
| **Phase 2 — File details** | `not-started` |
| **Epic 2.1 — Details already in the file** | `not-started` |
| 2.1.1 | `not-started` |
| 2.1.2 | `not-started` |
| **Phase 3 — People** | `not-started` |
| **Epic 3.1 — Same face** | `not-started` |
| 3.1.1 | `not-started` |
| 3.1.2 | `not-started` |
| 3.1.3 | `not-started` |
| **Phase 4 — Notes** | `not-started` |
| **Epic 4.1 — One local model** | `not-started` |
| 4.1.1 | `not-started` |
| **Epic 4.2 — Entity rows** | `not-started` |
| 4.2.1 | `not-started` |
| 4.2.2 | `not-started` |
| **All v1.5 tasks** | `not-started` |

## Later, not in these phases — `not-started`

Sync may later say a file is on another of the person’s devices. It will not copy the file. v1 only keeps the local id, the local op-log (`INSERT`, `DELETE`, `PIN`, `UNPIN`), and a sync flag that stays off. A crash can drop one unsent op-log line after the cursor commit. Search on this device still works. That line is not repaired while sync stays off.

The group browser is not in v1. Group labels, when built, are the strongest word, the date range, and the counts, in the person’s language. No language model writes them.

Version 1.5 is the separate board after the progress table. Progress on that board is 0%. It is not part of Phases 1–7.

Browser history is not in v1.

The English PP-OCRv6 small ONNX reader and the separate Arabic ONNX reader are not used until `appsettings.json` names each file and its SHA-256 and its catalog id. The English id is embeddinggemma-2-ocr-en. The Arabic id is embeddinggemma-2-ocr-ar. Task 4.3.3 stays as written. Done when both names, both hashes, and both catalog ids are in that file and a missing hash does not load a reader. `OcrReaders_MissingHash_DoesNotLoad` locks the missing-hash branch.

A picture or video Find passes `TextSearchFilter.Build` into the query before the 64 cap. That filter is the one task A.2.11 already shipped. A.2.15 is done on `Search_GrantFilter_DoesNotPromiseTwenty`. `PictureFind_OutOfGrant_DoesNotTakeASlot` locks the picture path.

A frame card and a window card carry a start and an end, in seconds. A tie on an equal score keeps the earlier start. Text search keeps `TextSearchHit` and `Collapse.MaxRrf`. Tasks 4.2.2 and 5.1.3 use `Collapse.EarliestStart`. Signature: `MediaSearchHit Collapse.EarliestStart(IReadOnlyList<MediaSearchHit> hits)` where `MediaSearchHit` is `string ContentId`, `string SourcePath`, `double StartSeconds`, `double EndSeconds`, `double Score`. Done when two frames with the same score keep the smaller start, and the card has both seconds. `FrameTie_KeepsEarlierStart` locks it.

The 800 ms cell is the recorded desktop load of 727.3 ms plus 10 percent. The mobile means are the A.2.9 report: HybridTop20 135.37 ms and LoadTextEncoder 886.14 ms on Host: emulator. A missing model directory is not a pass. `LlamaDesktopRun_MissingModel_ExitsWithNoNumber` locks the missing-directory branch.

Distribution for the Launch Addendum: builds stay on the developer machine, and a release is an artifact upload, not a cloud build. There is no self-updater and no update check, because the model download stays the app's only network path. Signed installers, store listings (Play, Microsoft Store, App Store), and any Linux GUI shell are later decisions a later accepted plan must take; they are not in the Launch Addendum. Package-manager manifests, the Windows community repository for identity `Recall.read` and a Homebrew cask once a Mac artifact exists, describe published releases only. No task of this plan is done by a star count.

A compressed ZVec index stays off until a measured search is about two or three times faster. That switch builds a second index beside the float one and does not shrink the file.

A helper process and a daily update check stay out of v1. A Windows tray, a Mac menu bar, sign-in start, and a search shortcut are studied on [desktop presence](features/desktop-presence.md) and are not in the tree. A Linux tray, sign-in start, and a search shortcut are on that same page and are not in the tree. Picture-word BM25 is task 6.2.1. Encoder-file repair is task A.2.4. Audio windows are task 5.1.3. Opening at the stored start is task 5.2.4. Keyword folding is task A.2.14. Numeric spans are task A.2.16 and are done. The dataset-folder skip is task A.2.17. The cloud-file skip is task A.2.18 and is done. The long-document slice, page cap, and broken-Arabic letter count are task 5.3.1. Task 2.2.6 is done.
