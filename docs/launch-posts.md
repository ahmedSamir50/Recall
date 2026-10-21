# Launch posts

Drafts for announcing the first release. Rules for every draft, taken from the Launch Addendum in [plan.md](plan.md): lead with what the product is, state the limits in the same breath as the claims, name no other product, and quote no number that the tree cannot source. The P.2.1 release notes take their first sentence from the Show HN draft's opening. Update a draft only on the day an artifact actually exists.

The word after a line in parentheses marks the target channel of that draft. These are drafts, not published posts; nothing here is an invite to spam. Post one, answer every comment, and let the work speak.

## Show HN draft

Title: `Show HN: Recall - on-device Arabic-and-English search over the folders you trust (.NET, MAUI, llama.cpp)`

Body draft:

> Recall shows a search box over folders you granted, and finds what is in those files by meaning, on your device, in Arabic and English. You grant a folder and the file types, it downloads the model once and checks its SHA-256, and Find runs in-process: the embedder lives inside the app's process, so it is still one process to close on your machine.
>
> Personal search in Arabic on a laptop or a phone is the part that started this. Typical document search breaks Arabic the moment it inherits a tokenizer that does not write it; typing Arabic in the keyboard search is still hit or miss. Recall treats Arabic as a first language end to end: right-to-left layout in both languages, an Arabic OCR reader shipped beside the English one, and the text extractor that catches a broken Arabic PDF layer rather than words that come out broken.
>
> What is in the tree today: folder grants with parent-child overlap warnings, the built-in skip lists (secrets, databases, lock files, dependency folders, git-ignore aware), text embedding through llama.cpp in-process, exact-document spanning numbers at 512 with Matryoshka cut and re-normalization, machine translation and hybrid BM25 full-text, one-carded unweighted RRF, watcher-based ingestion and a crash-cursor that resumes an unfinished file after a kill, Arabic and English UI every string in both languages, and code files as a first-class text grant.
>
> What is openly not in the tree yet: pictures, video, OCR of a photo you walk in, audio, and pins. They are in the plan below with statuses, not buried in a roadmap slide. The plan itself is one of the reasons to check the repo: each task carries an "Done when" check and honest statuses that anyone can audit.
>
> Deliberate limits, stated rather than hidden: no helper process to manage, because there is no second process; updates via new downloads of a release, and no update check feature because the model download is the only network path; no web app, no clipboard capture, no telemetry; no signed installer (Windows is a zip while we are small), so the install experience here is rawer than commercial software.
>
> Everything is in one codebase. MAUI Blazor Hybrid for the UI, .NET 10, `native/` for the llama.cpp P/Invoke and OCR .NET bindings.
>
> Ask: point us at any worry you have about the trust story of local-search apps, so we can lock the guarantee with code rather than copy.

## Forum draft (r/LocalLLaMA-style)

Draft:

> Recall (open-source, source-available license): the folders you trust, searchable on your device, in Arabic and English. Runs embeddinggemma-Q8_0 through llama.cpp in-process; the stored index stays on the device; the only network call is the one model download with a SHA-256 check.
>
> What works today: grant folders, hybrid exact-full-text + semantic Find in the granted text and code, real-time watcher-based ingestion, and a crash-cursor that resumes an unfinished file after a kill. Arabic and English UI throughout.
>
> Not yet but in the plan with statuses: picture and video search, text-in-image OCR (English and Arabic packs), audio that is a separate downloaded model you turn on. What is always excluded: any speech-to-text model, any cloud vector database, any telemetry.
>
> Happy to answer questions about the Matryoshka width cuts, why no second process, why no update-check pinging, and why the Arabic story here is anti-typical of typical local tooling.

## Short posts (X thread)

Post 1 (English):

> Recall is open-source and in-process: one app, one process, one network call (the model download). Your folders, noticed and searchable on your own device. Arabic and English UI from the first string. Plan with per-task statuses in the repo.

Post 2 (English):

> What it will not do, on purpose: no speech-to-text, no camera or clipboard capture, no chat UI, no telemetry. The index never leaves the device. Read the plan before you trust any local search app; that link is in our bio.

Post 3 (Arabic):

> Recall now is not just text, but a full multi-source search on your device. Read it as a search that respects a language, not a tool that neglects it. Arabic and English, right-to-left and in both directions, with the source code open for review. The plan and progress are visible for every task.
