## Question to page

- Which routes exist, and when does the text model row start a download? [docs/features/screens.md](features/screens.md). Source: `src/Recall.UI/Components/Models/ModelRow.razor.cs`.
- When does adding a folder save, and when does it refuse? [docs/features/folder-grants.md](features/folder-grants.md). Source: `src/Recall.Core/Scope/ScopeGrantService.cs`.
- Which text weight files download, and how is a partial body placed? [docs/features/model-download.md](features/model-download.md). Source: `src/Recall.Maui/appsettings.json`.
- How is one text embedding cut to 512, and which prefixes wrap a query? [docs/features/text-embed.md](features/text-embed.md). Source: `src/Recall.Adapters/Embedding/Llama/LlamaTextEmbeddingEncoder.cs`.
- How does the 8192 token split work, and which package loads the tokenizer? [docs/features/text-chunking.md](features/text-chunking.md). Source: `src/Recall.Core/Text/TextChunkPlanner.cs`.
- How does granted text become rows? [docs/features/text-ingest.md](features/text-ingest.md). Source: `src/Recall.Core/Text/TextIngestEngine.cs`.
- How does Find fuse a query, and when is a path dropped? [docs/features/text-search.md](features/text-search.md). Source: `src/Recall.Core/Search/TextSearchService.cs`.
- How does activating a hit choose opened, missing, or failed? [docs/features/open-file.md](features/open-file.md). Source: `src/Recall.Core/Search/LocalFileOpenPolicy.cs`.
- Which age values are stored, and what does not shrink a vector? [docs/features/age-cutoff.md](features/age-cutoff.md). Source: `src/Recall.Core/Preferences/AgeCutoffPreferenceService.cs`.
- Is sync transport in the tree? [docs/features/sync-stub.md](features/sync-stub.md). Source: `src/Recall.Core/Sync/SyncFeature.cs`.
- Which watcher runs while Recall is open, and how does iPhone catch up? [docs/features/watchers.md](features/watchers.md). Sources: `src/Recall.Core/Scope/ScopeWatchCoordinator.cs`, `src/Recall.Core/Scope/ScopeChangeBuffer.cs`, `src/Recall.Core/Checkpoints/SqliteIngestCursorStore.cs`.
- Are vision, video sampling, and OCR in the tree? [docs/features/vision-and-ocr.md](features/vision-and-ocr.md). No type for those readers is in `src/`.
- Is an audio encoder or a sibling transcript indexed? [docs/features/audio-and-transcripts.md](features/audio-and-transcripts.md). No speech-to-text package is in the solution.
- Is a code grant flag or a skip filter in the tree? [docs/features/code-and-skips.md](features/code-and-skips.md). Source: `src/Recall.Core/Scope/GrantFileKinds.cs`.
- Does the text split read a heading? [docs/features/structure-chunks.md](features/structure-chunks.md). No structure reader is in src/.
- Is a person name stored? [docs/features/people.md](features/people.md). No people type is in src/.
- Does Recall keep a place from a file? [docs/features/file-details.md](features/file-details.md). No file-detail reader is in src/.
- Is a notes model downloaded? [docs/features/local-notes.md](features/local-notes.md). v1 has no language model.
- Is a tray, a sign-in start, or a search shortcut in the tree? [docs/features/desktop-presence.md](features/desktop-presence.md). No type for those is in src/.
- Which desktop benchmark means are recorded? [docs/features/proof.md](features/proof.md). Source: `docs/plan.md`.
- Which keys and folders are on the device? [docs/data/stores.md](data/stores.md).
- What does each preference key store? [docs/data/preferences.md](data/preferences.md). Source: `src/Recall.Core/Preferences/AgeCutoffPreferenceService.cs`.
- What does the device record hold, and what is not read? [docs/data/device-record.md](data/device-record.md). Source: `src/Recall.Core/Device/Record/DeviceRecordService.cs`.
- What does the `items_text` collection store? [docs/data/items-text.md](data/items-text.md). Source: `src/Recall.Adapters/Indexing/ZVecTextVectorIndex.cs`.
- What does the local op-log append? [docs/data/oplog.md](data/oplog.md). Source: `src/Recall.Core/Sync/LocalOpLogWriter.cs`.
- What is the content id preimage? [docs/data/content-id.md](data/content-id.md). Source: `src/Recall.Core/Text/ContentIdBuilder.cs`.

Do not answer from InitDocs/ or from the retired notes.

If a page says decided-absent, do not invent a call sequence.

Every file under src, tests, and tools is a heading under docs/code.

A domain owns one reason to change. A use case is the path a person or a job runs across domains.
