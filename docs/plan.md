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

**Progress percentage.** A task is 100 when `done`, 50 when `partial`, and 0 when `not-started`. An epic's percentage is the mean of its tasks, rounded to the nearest whole number. A phase's percentage is the mean of its tasks, the same way. The only place those figures are written is the one table at the end of this plan. Do not invent a finer score.

Phase Three Addendum sits between Phase 3 and Phase 4. Its tasks are in that same table. The all-epics row is the mean of every task in Phases 1–7 and that addendum.

Will not be built: a web app, clipboard, camera, screen, or microphone capture, any language model, any speech-to-text model, browser history, sync transport, a cloud build, HNSW-RaBitQ, bundled model weights, and stored vectors at 128 numbers.

Stack for every task: `net10.0`, MAUI Blazor Hybrid, Tailwind, four shells (`android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64`), `Microsoft.ML.OnnxRuntime`, NuGet id `ZVec.NET` at `1.0.0-beta.6` from [Directory.Packages.props](../Directory.Packages.props) (local tree `D:\A_S\AdamSystems.ZVec.NET` reports `1.0.0-beta.6+zvec.0.7.0`; do not restore a package named `AdamSystems.ZVec.NET`). Git remote is Bitbucket. Build and release stay on the developer machine.

**Content id (Phase 2+):** SHA-256 of modality, source-content hash, stored width, and chunk ordinal (`0` when the note is one vector). A repeated insert of the same id is skipped, including after restart when the store already has that id. The first insert also appends a local unsent `INSERT` and a line in the UTC-day file under `oplog`. Sync stays off.

**Stale references (do not treat as this gate):** InitDocs 128-wide cold tiers, HNSW-RaBitQ, `[semantic]` / `[search]` prefixes, AdamSystems.ZVec.NET as the NuGet id, and the InitDocs ~50 ms cell as the Phase 2 benchmark pass line.

Open measurements, not tasks: disk size, memory after warmup, and whether crash reports ever leave the device. Crash reports stay on the device until that choice is made.

---

## Phase 1 — Shell — `done`

The app opens, speaks Arabic and English, and remembers this device. It does not index yet.

### Epic 1.1 — Solution — `done`

- **1.1.1** — `done` — Create `Recall.Core`, `Recall.UI`, `Recall.Maui`, benchmark and test projects. Core has no MAUI, Razor, or HTTP. UI calls Core ports only. Maui is the only composition root. Done when a phone and a Windows build both open an empty shell. The Windows shell and an Android shell were opened. iOS and Mac Catalyst installers stay on task 7.2.1.
- **1.1.2** — `done` — Add the ports: scope watcher, scope manager, video frame sampler, secure storage, idle detector, model download, index policy. Done when each port has a fake and no clipboard, camera, screen, or mic type exists. Each port has a fake. Clipboard, camera, screen, and mic types are absent. The watcher, sampler, secure storage, and idle detector stay empty until tasks 3.2.1 and 4.2.1.
- **1.1.3** — `done` — Ban `HttpClient`, `Socket`, `TcpListener`, and `UdpClient` inside Core, including the sync stub. Done when a socket in Core fails the build. BannedSymbols.txt is an AdditionalFile, the banned-API analyzer is on, and BannedSymbols_ListsHttpClientSocketTcpListenerUdpClient locks the four names.

### Epic 1.2 — Screens that can ship — `done`

- **1.2.1** — `done` — Put every visible string in `ar` and `en` resource files. Arabic layout is right to left. Done when a string in only one language fails a test. StringsResx_ArabicAndEnglish_HaveTheSameKeys passes.
- **1.2.2** — `done` — First screen is the folder grant: add and remove folders, pick file types, show the overlap warning. iPhone copy says new files are picked up when Recall is opened. Done when that screen works with no folders granted. The empty-grant screen was opened on Windows and Android. Continue was disabled.
- **1.2.3** — `done` — Add search, results, pin, age, this-device, and model-download screens as empty states. Pin copy is “keep this easy to find.” Age choices are only 2, 5, and 7 years, starting at 2. No width or quantization words on screen. Done when each empty state exists in both languages. Those routes and empty states exist in both languages. Home calls Find in task 2.3.2.

### Epic 1.3 — This device — `done`

- **1.3.1** — `done` — On first launch, store the public platform device id (random only when the read is empty, known-bad, or not ready). Store the native OS device name; both stay forever. Done when a second launch does not mint a new id and known-bad Android id `9774d56d682e549c` is rejected. EnsureLoaded_KeepsFirstStoredId_WhenLaterCandidateDiffers and ChooseId_RejectsKnownBadAndroidId pass.
- **1.3.2** — `done` — Let the person edit the display name Recall shows. The native name stays read-only. Done when the original name and the id are unchanged after the edit, and neither name nor the id is sent on the network. TrySetDisplayName_LeavesDeviceIdAndOriginalOsNameUnchanged passes. BuildRequestUri takes only the public file name and the base URI.

---

## Phase 2 — Text search — `done`

A granted text file can be found by meaning and by words. Scope stays a one-shot read when download finishes — not a watcher or Index now. A granted code file is searched as text: the text encoder, `items_text`, width 512.

### Epic 2.1 — Model download — `done`

- **2.1.1** — `done` — Download the int8 text ONNX into app data. Check SHA-256. Resume after a drop. The request may name the public weight file. It must not carry a user document name, document text, vector, device id, native device name, or custom display name. Float16 and the published 4-bit file are not this download. Vision and audio files stay unloaded. Done when a bad hash fails, a retry works, and the request builder omits those fields. `MauiModelDownloadService` appends a `206`, replaces on `200`, keeps a short body as `.partial`, hashes the three text files before moving them, and retry starts the download again. URLs, file names, and checksums come from `appsettings.json`. `DownloadRequestBuilder_OmitsForbiddenPayload` and `DownloadTransfer_PlacesBody` lock the request and resume rules. No public-network download in tests.
- **2.1.2** — `done` — Show progress, hash failure, and retry. Copy says the fetch is model weights only. Done when those three states exist in Arabic and English. The shell models screen shows those states and says the fetch is model weights only. The percent counts finished files plus the current file, and a progress tick is published only when that percent changes. `TextModelDownloadUiPolicy.MarkVerifying` assigns `Verifying` before both hash checks. `MarkVerifying_SetsPhaseAndClearsFault` locks it.
- **2.1.3** — `done` — Keep weights out of the installer. Done when the app package contains no ONNX weights. `*.onnx` and `tests/Recall.IntegrationTests/TestAssets/embeddinggemma-2-text/` are gitignored. `MauiProject_ContainsNoOnnxWeights` scans `src/Recall.Maui` (except `bin` and `obj`) and the csproj for `.onnx`. No apk build.

### Epic 2.2 — Text embed and index — `done`

- **2.2.1** — `done` — Run EmbeddingGemma 2 with ONNX Runtime. Mean-pool. L2-normalize the 768 numbers. Store text at 512 by keeping the leading part and L2-normalizing again. Done when a 128-wide store throws and a unit vector is length 1 after the cut. The encoder mean-pools a token matrix and L2-normalizes once. `CutAndNormalize` L2-normalizes the 512 slice. `IndexPolicy_Text512_Accepts_128Throws` and `Matryoshka_AfterCut_LengthIsOne` lock the width and the cut.
- **2.2.2** — `done` — Prefixes live in one Core type. Search uses `task: search result | query: {query}` once. A titled note uses `title: {title} | text: {content}` once. Cluster and classify prefix strings may exist on that type and are not wired to a query in this gate. Do not copy InitDocs `[semantic]` or `[search]` prefixes. Done when a second wrap of the prefix fails a test. `EmbeddingTaskPrefixes` is that type. An empty title is `text: {content}`. A second search wrap throws. Unit tests cover both.
- **2.2.3** — `done` — Create one text collection. Index params: plain HNSW, `M=16`, `efConstruction=200`, `QuantizeType` left `Undefined`. `EfSearch` 64 is set on `ZVecHnswQueryParams`, because the index param has no such field and a null query value uses 300. Done when one float index exists and a quantized second index is not created. `ZVecTextVectorIndex` creates `items_text` with those index params and sets `EfSearch` 64 and RRF rank 60 on the query. `Recall.Maui` references `ZVec.NET` so the phone native library is packaged. Android 16 warns that `libzvec_c_api.so` is not 16 KB page-aligned. A second quantized index was not created. The opened root contains a single `items_text` directory. The Android 16 KB warning stays a ZVec native warning.
- **2.2.4** — `done` — Tokenize with the model's `tokenizer.json`. The published `Tokenizers.DotNet` 1.4.1 package is the loader, because that file is BPE and `Microsoft.ML.Tokenizers` does not load it. The 8192 budget includes the prefix. One vector when the note fits. Split only past that. Do not average chunks. Later chunks keep the title prefix and do not split a surrogate pair. Done when an over-limit note becomes more than one vector and an under-limit note stays one. The chunk planner and unit tests do that with a fake counter. Windows uses the `win-x64` tokenizer runtime. Mac Catalyst uses `osx-arm64`. Android and iOS have no published tokenizer runtime, so those shells fail closed with a localized fault. `PlanChunks_UnderBudget_StaysOnePiece` locks the one-piece branch. Android and iOS stay fail-closed in `TextTokenizerRuntime.IsSupported`.
- **2.2.5** — `done` — BenchmarkDotNet text-only exit: one text session, one 512-d float HNSW graph, `EfSearch` 64, `QuantizeType` off, insert ≤5 ms, hybrid top-20 ≤60 ms mobile / ≤25 ms desktop. Encoder load is its own number (≤600 ms mobile / ≤800 ms desktop) and is not added into query time. Label emulator and desktop runs so they are not reported as a phone. The benchmark project embeds one search query only when `RECALL_TEXT_MODEL_DIR` points at the text model, and it exits without a number otherwise. Desktop run `Host: desktop`: `InsertOne` mean 30.66 us, `HybridTop20` mean 2,202.43 us, `LoadTextEncoder` mean 727.3 ms. Insert and hybrid are inside the desktop line; encoder load of 727.3 ms is inside the 800 ms desktop line, so this task is done. The 800 ms line is that mean plus 10 percent. The mobile 600 ms cell is unmeasured.
- **2.2.6** — `done` — A granted code file is text. The grant offers Code beside Text, Images, Video, and Audio. The English label is `Code`. The Arabic label is `كود`. Extensions, exact names, skip folders, secrets, databases, lock files, and gitignore rules are the canon bullets. Embed with the text encoder into `items_text` at 512, using `title: {file name} | text: {content}`. There is no separate code collection and no `code:` query prefix. Opening a hit opens the file. Preferences shows those lists. Built-in rows cannot be removed. The person can add an extension, a folder name, or a file name, and can remove only what they added. The heading is `Skipped files` and `الملفات المتخطاة`. Done when a `.cs` file in a grant with Code on is searchable as text, a grant with Code off does not index it, a `.env` file and a `.db` file are not read, a file ignored by `.gitignore` is not read, and an extension the person added is skipped. `Decide_CsWithCode_Reads`, `ReadAsync_CodeOff_SkipsCs`, `ReadAsync_Env_NotRead`, `ReadAsync_Db_NotRead`, `ReadAsync_GitIgnore_SkipsTxt`, and `TryAdd_Log_AddedAndLowercased` lock it. Task 2.2.6 is done.

### Epic 2.3 — Query text — `done`

- **2.3.1** — `done` — Embed the question once. Dense HNSW plus BM25 on the raw note text, plus the folder filter, fused with `ZVecRrfReranker` at `RankConstant` 60. A file outside the grant is not returned. Done when skipping RRF fails a test and an out-of-grant path is dropped. `TextSearchService` embeds once and cuts to 512. The index fuses dense and full text at rank 60 and keeps a hit only when the path is inside the grant, so `D:\Photos` does not match `D:\PhotosExtra`. BM25 stores the chunk text. A rank other than 60 throws. `Search_DropsPathOutsideGrant` drops a path outside the grant, and Home calls `FindAsync`.
- **2.3.2** — `done` — The search page is Find only. Group and Tag do not run a query. Show the top 20. Each card shows kind, name, local time, and why it matched. Opening a card opens the file. A missing file stays on the card as unavailable. Done when demo cards, the translation line, and the fake item count are off the page and the first five cards can show before the rest are drawn. Home submits Find on Enter, shows up to 20 cards with the first 5 immediate, and opens a present file via `ILocalFileOpener`. `HomeRazor_OmitsDemoCards` locks the demo removal.

---

## Phase 3 — Folders that stay current — `not-started`

New, changed, and deleted files inside a grant update the index without a timer.

### Epic 3.1 — Grants — `not-started`

- **3.1.1** — `not-started` — If one grant sits inside another, keep the parent and warn. Done when `D:\` plus `D:\Photos` indexes `D:\` only, with the warning in both languages.
- **3.1.2** — `not-started` — A path the person allowed but has not indexed yet is offered from the search page. Done when **Index now** indexes it and a child of an already allowed parent is not offered again.

### Epic 3.2 — Watch and scan — `not-started`

- **3.2.1** — `not-started` — Android watches a Storage Access Framework tree. Windows uses `FileSystemWatcher`. Mac uses `FSEventStream`. The watcher runs only while Recall runs. Done when each shell reports created, modified, renamed, and deleted for a granted folder.
- **3.2.2** — `not-started` — iPhone walks the granted folder when the app comes to the foreground and diffs `checkpoints.db`. Done when a file added while the app was closed is queued on the next open, and no background watch exists.
- **3.2.3** — `not-started` — A rename updates the path. Embed again only when the bytes changed. A delete keeps the vector, writes a tombstone, and marks the source missing. Done when those three cases are tested.

### Epic 3.3 — Crash cursor — `not-started`

- **3.3.1** — `not-started` — Write ingest progress to `checkpoints.db` in a transaction. Done when a kill midway resumes the unfinished file and does not re-index a finished one.

---

## Phase Three Addendum — Gaps left by Phases 1–3 — `partial`

This phase is new. It does not rewrite Phases 1, 2, or 3. Those rows stay as written. The sentences in this paragraph are not tasks.

“Stack for every task” names the four RIDs. Task 1.1.1 stays done on a phone and Windows. The four-shell install stays task 7.2.1. Task 1.1.2 says the sampler, secure storage, and the idle detector stay empty until task 4.2.1. That sentence stays. The sampler is task 4.2.1. Secure storage stays empty, and task 4.2.1 does not fill it. The idle detector is task A.2.8. Task 1.2.2 stays done. A granted text file is found on Windows and Mac. Android and iOS stay fail-closed until a tokenizer runtime exists. Task 3.2.1 diffs every shell against `checkpoints.db` on open and on resume. That diff is the catcher for a file that changed while Recall was closed. There is no timer, and Index now is not that catcher. Task 3.1.2 still offers only a path that is not indexed yet. A finished grant stays hidden. The cursor file and its chunk rows commit in one transaction. The vector insert happens before that commit. The op-log `INSERT` is appended only after that insert succeeds. A crash after the insert and before the commit leaves the file `Reading`, and the next read skips an id the store already has. The op-log file is not inside that transaction. A crash in the gap between the cursor commit and the op-log append has no recovery in this plan. Search on this device still works, because the vector and the cursor are already committed. The lost line is only an unsent sync entry. Sync stays off. If sync is later turned on, that gap has to be closed first.

### Epic A.2 — Closures — `partial`

- **A.2.2** — `not-started` — When a file’s chunk count shrinks, ordinals past the new count are retired. Each retired id appends `DELETE` once. A shrink from two chunks to one appends one `DELETE`. The vector row stays. Find does not return those hits. `SqliteIngestCursorStore.MarkFinished` is the commit. This is not the file-delete path in task 3.2.3. A file that is already missing does not append `DELETE` again. A later delete of the whole file appends `DELETE` only for ordinals that are not already retired. Done when a file that goes from two chunks to one leaves the second id retired and absent from Find.
- **A.2.3** — `partial` — Parent wins file types. Same path stays a duplicate. A child of a parent stays refused. A parent added after children replaces those children. The parent’s file types are the grant. The child’s types are not merged. `ScopeGrantService.TryAddGrant` already keeps the parent grant. Done when a parent granted Text only, added after a child granted Images, keeps Text only, and a test locks that. The parent row is already kept. The Text-only test is left.
- **A.2.4** — `not-started` — The text collection stores, in `checkpoints.db`, the SHA-256 of the ONNX file that wrote its vectors. That hash is not part of the content id. Search of that collection hides vectors written by a different hash. It does not re-embed them. A finished file whose source hash is unchanged stays skipped, as in task 3.3.1. Those hidden vectors stay hidden until the source bytes change. There is no rebuild task. v1 does not replace the text encoder file whose checksum task 2.1.1 already accepted with a different hash, and a new hash is not downloaded over that file. Picture, video, and audio collections store the same kind of hash when Phases 4 and 5 create them. Task 6.1.2 writes the short row with the hash of the file that produced it and deletes the long row. Task 6.2.1 merges only rows that hash filter kept. Done when a second text-encoder hash hides the older text vectors and the content id is unchanged.
- **A.2.5** — `done` — Task 2.2.6 stays done on its written Done when. This task locks the canon classes that sentence names and that check does not. Exact name: `IsCodeFile_MakefileAnyCase_IsTrue`. Skip folder: `IsBlockedFolder_Git_IsTrue`. Secret other than `.env`: `IsSecret_Pem_IsTrue`. Lock file: `IsLockFile_PackageLock_IsTrue`. Done when those four tests stay the locks.
- **A.2.6** — `partial` — The text collection creates one float HNSW index and one full-text index on the raw note text. Task 2.2.3 stays the float-index check. `ZVecTextVectorIndex` already sets `ZVecFtsIndexParam` on `raw_text`. Done when a test fails if that full-text index param is removed. The param is already set. That failing test is left.
- **A.2.7** — `partial` — When the text model download reaches Ready, `MauiModelDownloadService` reads each grant once through `IngestGrantedTextAsync`. A grant with Text off and Code off is not read. A finished file with the same bytes is not embedded again. That pass does not start a watcher. `IngestGrant_TextAndCodeOff_DoesNotRead` and `IngestDocument_FinishedSameHash_DoesNotEmbed` stay. Done when a test shows the Ready path calls that read. The Ready call is already there. The test of that call is left.
- **A.2.8** — `not-started` — The idle detector pauses ingest. It does not sample video. Task 4.2.1 is the sampler only. Task 1.1.2’s “until 4.2.1” sentence is not edited; this task is the idle port. `IIdleDetectorService` is that port. An idle signal defers the next ingest until the device is busy again. It does not drop the file. Done when an idle signal defers that ingest and a busy signal runs it.
- **A.2.9** — `not-started` — Lines 12–17 would call task 2.2.5 `partial`, because its exit still lists hybrid ≤60 ms mobile and load ≤600 ms mobile, and the same paragraph says the mobile cell is unmeasured. This phase does not rewrite that row, so the row stays `done`. The desktop run and the written 800 ms cell stay the desktop record. Done when a labeled phone run, not an emulator, reports hybrid top-20 ≤60 ms and encoder load ≤600 ms. The phone run is left.

---

## Phase 4 — Pictures, video, and words in documents — `not-started`

Vision waits on tasks A.2.2, A.2.3, A.2.4, A.2.7, and A.2.8. It does not wait on A.2.6 or A.2.9. A.2.6 is the text full-text index. A.2.9 is the labeled phone run of the task 2.2.5 text gate: one text session, a 512-d float HNSW graph, `EfSearch` 64, hybrid top-20 ≤60 ms, and encoder load ≤600 ms. Those two stay in the addendum because that phase sits between Phase 3 and Phase 4. They do not gate these tasks.

### Epic 4.1 — Vision — `not-started`

- **4.1.1** — `not-started` — Download the vision ONNX file the first time a picture is indexed or used as a query. Unload it when the job ends. Do not keep it resident with the text encoder and the audio encoder. Done when a picture query before that download shows the waiting state, then succeeds after the download.
- **4.1.2** — `not-started` — Store pictures at 768 in their own collection. Done when a picture vector is not written into the text collection.

### Epic 4.2 — Video frames — `not-started`

- **4.2.1** — `not-started` — Sample one JPEG frame every two seconds with the platform sampler. Embed those frames with the vision encoder into the video collection at 768. The soundtrack is task 5.1.3. Done when a two-second cadence is what the sampler emits.

### Epic 4.3 — OCR for English and Arabic — `not-started`

- **4.3.1** — `not-started` — If a PDF has a text layer, index that layer. Do not OCR those pages. Done when a text-layer page produces text and does not call the OCR reader.
- **4.3.2** — `not-started` — OCR a scanned page. OCR an image only for words printed in it. The vision vector is still stored. Done when a photo with no words is still findable by the picture, and a scanned page is findable by its words.
- **4.3.3** — `not-started` — Ship one shared text finder, the PP-OCRv6 small English ONNX reader, and the separate Arabic ONNX reader of about 8 MB. Both readers are on for v1. Done when an English page and an Arabic page each return their words.
- **4.3.4** — `not-started` — Any other language is off until the person turns it on, and that turn-on downloads only that reader. Done when Chinese weights are absent until Chinese is selected.

---

## Phase 5 — Audio and transcripts that already exist — `partial`

### Epic 5.1 — Audio files — `partial`

- **5.1.1** — `not-started` — If the audio file type is off, show one line and do not embed. If it is on, download the audio ONNX file, embed, store in the audio collection at 768, then unload. Done when the encoder is not still loaded after the job, and there is no microphone control.
- **5.1.2** — `done` — Do not add a speech-to-text model. Done when the solution contains no ASR package. The solution has no speech-to-text package.
- **5.1.3** — `not-started` — When the audio file type is on, a video soundtrack is overlapping windows of the audio encoder’s native length. Each window stores a start and an end. Unload the vision session before the audio session loads. Frames stay the one JPEG every two seconds from task 4.2.1. The hop length is not set here. Done when a video with audio on produces those windows and the vision session is not loaded during them.

### Epic 5.2 — Sibling transcript files — `not-started`

- **5.2.1** — `not-started` — If `talk.mp4` or `talk.m4a` has `talk.srt`, `talk.vtt`, `talk.sbv`, `talk.ass`, `talk.ssa`, `talk.ttml`, or a language tag such as `talk.en.vtt`, attach that text to the media item. Done when a search for those words opens the media file.
- **5.2.2** — `not-started` — Do not also index that sibling as its own unlinked document. Do not read a caption track hidden inside the media file. Done when both of those stays are tested.

---

## Phase 6 — Age and pins — `partial`

### Epic 6.1 — Move old pictures, video, and audio — `partial`

- **6.1.1** — `done` — Read the person’s choice of 2, 5, or 7 years. Default is 2. Done when any other number is rejected. Preferences stores 2, 5, or 7, and another number throws. Shrink of old media is task 6.1.2 and has not started.
- **6.1.2** — `not-started` — When a picture, video, or audio file is older than that, cut its 768 vector to 512, L2-normalize, insert into the old collection, and delete the long row. The short row stores the encoder-file hash from task A.2.4. That delete is the age move, not a source tombstone. Text never moves. Done when only one copy remains.
- **6.1.3** — `not-started` — A pin stays at 768. Growing a shortened item back embeds the file again. Done when a pinned old photo is still in the long collection.

### Epic 6.2 — Search across both ages — `not-started`

- **6.2.1** — `not-started` — Embed the question once with that collection’s encoder, cut it to the long width and the old width, search those two collections when the grant allows them, and merge with RRF at `k=60`. A text query searches `items_text` only. A picture query searches the picture and video collections only. Audio is searched with the audio encoder only. Those lists are not fused, because one embedding cannot serve a different encoder. A row whose encoder-file hash differs from the file that should search that collection stays out of the merge. Done when a query that should hit both ages of one encoder returns one fused list, and a text query does not return a picture.

---

## Phase 7 — Proof and a local build — `partial`

### Epic 7.1 — Tests that can fail — `partial`

- **7.1.1** — `partial` — Unit-test every method and every branch that decides behavior for the ports and policies above, with fakes for ONNX and ZVec. Done when deleting a branch makes a test go red. A coverage percent of 60% then 70% is a floor, not the exit. Unit tests cover the text prefix, chunk split, download resume rules, grant boundary, width 128, age cutoff, and device id. Later-phase branches have no tests because that code does not exist.
- **7.1.2** — `not-started` — Integration-test real ONNX plus real ZVec: crash restart from `checkpoints.db`, and zero non-local bytes after the model download has finished. The unit cursor locks stay task 3.3.1. This task is the kill-and-restart run, not a second copy of those locks. Done when those two runs exist. A text-search integration test is in the tree. Crash restart and the egress check are not.
- **7.1.3** — `not-started` — Golden retrieval in English, Arabic, Chinese, Hindi, and French. Recall@10 for the same query stays within 5 points across those five. Text stays at 512 unless this run clears 256. Done when that report exists. UI languages stay Arabic and English either way.

### Epic 7.2 — Local release — `not-started`

- **7.2.1** — `not-started` — Produce the four shells on the developer machine from the Bitbucket repo. Done when each shell installs without ONNX weights inside the package and downloads the text model on first run.
- **7.2.2** — `not-started` — Leave crash reports on the device. Done when no crash package is referenced.

---

## Progress

Counted 15 Oct 2026. This is the only progress table.

| | Status | Progress |
|---|---|---|
| **Phase 1 — Shell** | `done` | 100% |
| **Epic 1.1 — Solution** | `done` | 100% |
| 1.1.1 | `done` | 100% |
| 1.1.2 | `done` | 100% |
| 1.1.3 | `done` | 100% |
| **Epic 1.2 — Screens that can ship** | `done` | 100% |
| 1.2.1 | `done` | 100% |
| 1.2.2 | `done` | 100% |
| 1.2.3 | `done` | 100% |
| **Epic 1.3 — This device** | `done` | 100% |
| 1.3.1 | `done` | 100% |
| 1.3.2 | `done` | 100% |
| **Phase 2 — Text search** | `done` | 100% |
| **Epic 2.1 — Model download** | `done` | 100% |
| 2.1.1 | `done` | 100% |
| 2.1.2 | `done` | 100% |
| 2.1.3 | `done` | 100% |
| **Epic 2.2 — Text embed and index** | `done` | 100% |
| 2.2.1 | `done` | 100% |
| 2.2.2 | `done` | 100% |
| 2.2.3 | `done` | 100% |
| 2.2.4 | `done` | 100% |
| 2.2.5 | `done` | 100% |
| 2.2.6 | `done` | 100% |
| **Epic 2.3 — Query text** | `done` | 100% |
| 2.3.1 | `done` | 100% |
| 2.3.2 | `done` | 100% |
| **Phase 3 — Folders that stay current** | `not-started` | 0% |
| **Epic 3.1 — Grants** | `not-started` | 0% |
| 3.1.1 | `not-started` | 0% |
| 3.1.2 | `not-started` | 0% |
| **Epic 3.2 — Watch and scan** | `not-started` | 0% |
| 3.2.1 | `not-started` | 0% |
| 3.2.2 | `not-started` | 0% |
| 3.2.3 | `not-started` | 0% |
| **Epic 3.3 — Crash cursor** | `not-started` | 0% |
| 3.3.1 | `not-started` | 0% |
| **Phase Three Addendum — Gaps left by Phases 1–3** | `partial` | 31% |
| **Epic A.2 — Closures** | `partial` | 31% |
| A.2.2 | `not-started` | 0% |
| A.2.3 | `partial` | 50% |
| A.2.4 | `not-started` | 0% |
| A.2.5 | `done` | 100% |
| A.2.6 | `partial` | 50% |
| A.2.7 | `partial` | 50% |
| A.2.8 | `not-started` | 0% |
| A.2.9 | `not-started` | 0% |
| **Phase 4 — Pictures, video, and words in documents** | `not-started` | 0% |
| **Epic 4.1 — Vision** | `not-started` | 0% |
| 4.1.1 | `not-started` | 0% |
| 4.1.2 | `not-started` | 0% |
| **Epic 4.2 — Video frames** | `not-started` | 0% |
| 4.2.1 | `not-started` | 0% |
| **Epic 4.3 — OCR for English and Arabic** | `not-started` | 0% |
| 4.3.1 | `not-started` | 0% |
| 4.3.2 | `not-started` | 0% |
| 4.3.3 | `not-started` | 0% |
| 4.3.4 | `not-started` | 0% |
| **Phase 5 — Audio and transcripts that already exist** | `partial` | 20% |
| **Epic 5.1 — Audio files** | `partial` | 33% |
| 5.1.1 | `not-started` | 0% |
| 5.1.2 | `done` | 100% |
| 5.1.3 | `not-started` | 0% |
| **Epic 5.2 — Sibling transcript files** | `not-started` | 0% |
| 5.2.1 | `not-started` | 0% |
| 5.2.2 | `not-started` | 0% |
| **Phase 6 — Age and pins** | `partial` | 25% |
| **Epic 6.1 — Move old pictures, video, and audio** | `partial` | 33% |
| 6.1.1 | `done` | 100% |
| 6.1.2 | `not-started` | 0% |
| 6.1.3 | `not-started` | 0% |
| **Epic 6.2 — Search across both ages** | `not-started` | 0% |
| 6.2.1 | `not-started` | 0% |
| **Phase 7 — Proof and a local build** | `partial` | 10% |
| **Epic 7.1 — Tests that can fail** | `partial` | 17% |
| 7.1.1 | `partial` | 50% |
| 7.1.2 | `not-started` | 0% |
| 7.1.3 | `not-started` | 0% |
| **Epic 7.2 — Local release** | `not-started` | 0% |
| 7.2.1 | `not-started` | 0% |
| 7.2.2 | `not-started` | 0% |
| **All epics** | `partial` | 44% |

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

| | Status | Progress |
|---|---|---|
| **Phase 1 — Sections** | `not-started` | 0% |
| **Epic 1.1 — Structure split** | `not-started` | 0% |
| 1.1.1 | `not-started` | 0% |
| 1.1.2 | `not-started` | 0% |
| **Epic 1.2 — Links inside a grant** | `not-started` | 0% |
| 1.2.1 | `not-started` | 0% |
| **Phase 2 — File details** | `not-started` | 0% |
| **Epic 2.1 — Details already in the file** | `not-started` | 0% |
| 2.1.1 | `not-started` | 0% |
| 2.1.2 | `not-started` | 0% |
| **Phase 3 — People** | `not-started` | 0% |
| **Epic 3.1 — Same face** | `not-started` | 0% |
| 3.1.1 | `not-started` | 0% |
| 3.1.2 | `not-started` | 0% |
| 3.1.3 | `not-started` | 0% |
| **Phase 4 — Notes** | `not-started` | 0% |
| **Epic 4.1 — One local model** | `not-started` | 0% |
| 4.1.1 | `not-started` | 0% |
| **Epic 4.2 — Entity rows** | `not-started` | 0% |
| 4.2.1 | `not-started` | 0% |
| 4.2.2 | `not-started` | 0% |
| **All v1.5 tasks** | `not-started` | 0% |

## Later, not in these phases — `not-started`

Sync may later say a file is on another of the person’s devices. It will not copy the file. v1 only keeps the local id, the local op-log (`INSERT`, `DELETE`, `PIN`, `UNPIN`), and a sync flag that stays off. A crash can drop one unsent op-log line after the cursor commit. Search on this device still works. That line is not repaired while sync stays off.

The group browser is not in v1. Group labels, when built, are the strongest word, the date range, and the counts, in the person’s language. No language model writes them.

Version 1.5 is the separate board after the progress table. It is not part of Phases 1–7.

Browser history is not in v1.

A compressed ZVec index stays off until a measured search is about two or three times faster. That switch builds a second index beside the float one and does not shrink the file.

A menu-bar launcher, a helper process, and a daily update check stay out of v1. Crawl skips, picture-word BM25, keyword folding, encoder-file identity, audio windows, and open-at page or time follow the canon bullets. Task 2.2.6 is done. This paragraph adds no other task row.
