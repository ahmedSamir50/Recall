Status: semi-final plan, 13 Oct 2026. InitDocs/ is history and is not this plan.

# Recall plan

Build order is phases. Each phase is a set of epics. Each epic is a set of tasks. A task is done when its check is true. A phase is done when its epics are done.

Will not be built: a web app, clipboard, camera, screen, or microphone capture, any language model, any speech-to-text model, browser history, sync transport, a cloud build, HNSW-RaBitQ, bundled model weights, and stored vectors at 128 numbers.

Stack for every task: `net10.0`, MAUI Blazor Hybrid, MudBlazor, Tailwind, four shells (`android-arm64`, `ios-arm64`, `win-x64`, `maccatalyst-arm64`), `Microsoft.ML.OnnxRuntime`, ZVec.NET pin `1.0.0-beta.6+zvec.0.7.0` from `D:\A_S\AdamSystems.ZVec.NET`. Git remote is Bitbucket. Build and release stay on the developer machine.

Open measurements, not tasks: disk size, memory after warmup, and whether crash reports ever leave the device. Crash reports stay on the device until that choice is made.

---

## Phase 1 — Shell

The app opens, speaks Arabic and English, and remembers this device. It does not index yet.

### Epic 1.1 — Solution

- **1.1.1** Create `Recall.Core`, `Recall.UI`, `Recall.Maui`, benchmark and test projects. Core has no MAUI, Razor, or HTTP. UI calls Core ports only. Maui is the only composition root. Done when a phone and a Windows build both open an empty shell.
- **1.1.2** Add the ports: scope watcher, scope manager, video frame sampler, secure storage, idle detector, model download, index policy. Done when each port has a fake and no clipboard, camera, screen, or mic type exists.
- **1.1.3** Ban `HttpClient`, `Socket`, `TcpListener`, and `UdpClient` inside Core, including the sync stub. Done when a socket in Core fails the build.

### Epic 1.2 — Screens that can ship

- **1.2.1** Put every visible string in `ar` and `en` resource files. Arabic layout is right to left. Done when a string in only one language fails a test.
- **1.2.2** First screen is the folder grant: add and remove folders, pick file types, show the overlap warning. iPhone copy says new files are picked up when Recall is opened. Done when that screen works with no folders granted.
- **1.2.3** Add search, results, pin, age, this-device, and model-download screens as empty states. Pin copy is “keep this easy to find.” Age choices are only 2, 5, and 7 years, starting at 2. No width or quantization words on screen. Done when each empty state exists in both languages.

### Epic 1.3 — This device

- **1.3.1** On first launch, store a random device id and the OS device name. Both stay forever. Done when a second launch does not mint a new id.
- **1.3.2** Let the person edit the name Recall shows. Done when the original name and the id are unchanged after the edit, and neither is sent on the network.

---

## Phase 2 — Text search

A granted text file can be found by meaning and by words.

### Epic 2.1 — Model download

- **2.1.1** Download the text ONNX file into the platform app-data folder. Check SHA-256. Resume after a drop. Done when a bad hash fails, a retry works, and the request carries no file name, document text, vector, or device id.
- **2.1.2** Show progress, hash failure, and retry. Copy says the fetch is model weights only. Done when those three states exist in Arabic and English.
- **2.1.3** Keep weights out of the installer. Done when the app package contains no ONNX weights.

### Epic 2.2 — Text embed and index

- **2.2.1** Run EmbeddingGemma 2 with ONNX Runtime. Mean-pool. L2-normalize the 768 numbers. Store text at 512 by keeping the leading part and L2-normalizing again. Done when a 128-wide store throws and a unit vector is length 1 after the cut.
- **2.2.2** Apply the search prefix once: `task: search result | query: {query}`. Documents use `title: {title} | text: {content}`. Done when a second wrap of the prefix fails a test.
- **2.2.3** Create one text collection, plain HNSW, `M=16`, `efConstruction=200`, `efSearch=64`, `QuantizeType` left off. Done when one float index exists and a quantized second index is not created.
- **2.2.4** Embed a note whole when it fits in 8192 tokens. Split only past that. Do not average chunks. Done when an over-limit note becomes more than one vector and an under-limit note stays one.

### Epic 2.3 — Query text

- **2.3.1** Embed the question once. Search the text collection with dense match, keyword match, and the folder filter. Fuse inside ZVec with RRF at `k=60`. Done when skipping RRF fails a test and a file outside the grant cannot be returned.
- **2.3.2** Show the top 20. Each card shows kind, name, local time, and why it matched. Opening a card opens the file. A missing file stays on the card as unavailable. Done when the first five cards can show before the rest are drawn.

---

## Phase 3 — Folders that stay current

New, changed, and deleted files inside a grant update the index without a timer.

### Epic 3.1 — Grants

- **3.1.1** If one grant sits inside another, keep the parent and warn. Done when `D:\` plus `D:\Photos` indexes `D:\` only, with the warning in both languages.
- **3.1.2** A path the person allowed but has not indexed yet is offered from the search page. Done when **Index now** indexes it and a child of an already allowed parent is not offered again.

### Epic 3.2 — Watch and scan

- **3.2.1** Android watches a Storage Access Framework tree. Windows uses `FileSystemWatcher`. Mac uses `FSEventStream`. The watcher runs only while Recall runs. Done when each shell reports created, modified, renamed, and deleted for a granted folder.
- **3.2.2** iPhone walks the granted folder when the app comes to the foreground and diffs `checkpoints.db`. Done when a file added while the app was closed is queued on the next open, and no background watch exists.
- **3.2.3** A rename updates the path. Embed again only when the bytes changed. A delete keeps the vector, writes a tombstone, and marks the source missing. Done when those three cases are tested.

### Epic 3.3 — Crash cursor

- **3.3.1** Write ingest progress to `checkpoints.db` in a transaction. Done when a kill midway resumes the unfinished file and does not re-index a finished one.

---

## Phase 4 — Pictures, video, and words in documents

### Epic 4.1 — Vision

- **4.1.1** Download the vision ONNX file the first time a picture is indexed or used as a query. Unload it when the job ends. Do not keep it resident with the text encoder and the audio encoder. Done when a picture query before that download shows the waiting state, then succeeds after the download.
- **4.1.2** Store pictures at 768 in their own collection. Done when a picture vector is not written into the text collection.

### Epic 4.2 — Video frames

- **4.2.1** Sample one JPEG frame every two seconds with the platform sampler. Embed those frames with the vision encoder into the video collection at 768. Done when a two-second cadence is what the sampler emits.

### Epic 4.3 — OCR for English and Arabic

- **4.3.1** If a PDF has a text layer, index that layer. Do not OCR those pages. Done when a text-layer page produces text and does not call the OCR reader.
- **4.3.2** OCR a scanned page. OCR an image only for words printed in it. The vision vector is still stored. Done when a photo with no words is still findable by the picture, and a scanned page is findable by its words.
- **4.3.3** Ship one shared text finder, the PP-OCRv6 small English ONNX reader, and the separate Arabic ONNX reader of about 8 MB. Both readers are on for v1. Done when an English page and an Arabic page each return their words.
- **4.3.4** Any other language is off until the person turns it on, and that turn-on downloads only that reader. Done when Chinese weights are absent until Chinese is selected.

---

## Phase 5 — Audio and transcripts that already exist

### Epic 5.1 — Audio files

- **5.1.1** If the audio file type is off, show one line and do not embed. If it is on, download the audio ONNX file, embed, store in the audio collection at 768, then unload. Done when the encoder is not still loaded after the job, and there is no microphone control.
- **5.1.2** Do not add a speech-to-text model. Done when the solution contains no ASR package.

### Epic 5.2 — Sibling transcript files

- **5.2.1** If `talk.mp4` or `talk.m4a` has `talk.srt`, `talk.vtt`, `talk.sbv`, `talk.ass`, `talk.ssa`, `talk.ttml`, or a language tag such as `talk.en.vtt`, attach that text to the media item. Done when a search for those words opens the media file.
- **5.2.2** Do not also index that sibling as its own unlinked document. Do not read a caption track hidden inside the media file. Done when both of those stays are tested.

---

## Phase 6 — Age and pins

### Epic 6.1 — Move old pictures, video, and audio

- **6.1.1** Read the person’s choice of 2, 5, or 7 years. Default is 2. Done when any other number is rejected.
- **6.1.2** When a picture, video, or audio file is older than that, cut its 768 vector to 512, L2-normalize, insert into the old collection, and delete the long row. Text never moves. Done when only one copy remains.
- **6.1.3** A pin stays at 768. Growing a shortened item back embeds the file again. Done when a pinned old photo is still in the long collection.

### Epic 6.2 — Search across both ages

- **6.2.1** Embed the question once, cut it to each live collection’s width, search each collection the grant allows, and merge with RRF at `k=60`. Done when a query that should hit both ages returns one fused list.

---

## Phase 7 — Proof and a local build

### Epic 7.1 — Tests that can fail

- **7.1.1** Unit-test every method and every branch that decides behavior for the ports and policies above, with fakes for ONNX and ZVec. Done when deleting a branch makes a test go red. A coverage percent of 60% then 70% is a floor, not the exit.
- **7.1.2** Integration-test real ONNX plus real ZVec: crash restart from `checkpoints.db`, and zero non-local bytes after the model download has finished. Done when those two runs exist.
- **7.1.3** Golden retrieval in English, Arabic, Chinese, Hindi, and French. Recall@10 for the same query stays within 5 points across those five. Text stays at 512 unless this run clears 256. Done when that report exists. UI languages stay Arabic and English either way.

### Epic 7.2 — Local release

- **7.2.1** Produce the four shells on the developer machine from the Bitbucket repo. Done when each shell installs without ONNX weights inside the package and downloads the text model on first run.
- **7.2.2** Leave crash reports on the device. Done when no crash package is referenced.

---

## Later, not in these phases

Sync may later say a file is on another of the person’s devices. It will not copy the file. v1 only keeps the local id, the local op-log (`INSERT`, `DELETE`, `PIN`, `UNPIN`), and a sync flag that stays off.

The group browser is not in v1. Group labels, when built, are the strongest word, the date range, and the counts, in the person’s language. No language model writes them.

Browser history is not in v1.

A compressed ZVec index stays off until a measured search is about two or three times faster. That switch builds a second index beside the float one and does not shrink the file.
