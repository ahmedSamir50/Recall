Status: in-code

## Routes

`/` Home, `/landing`, `/onboarding/welcome`, `/onboarding/folders`, `/onboarding/models`, `/onboarding/ready`, `/lattice`, `/folders`, `/models`, `/preferences`, `/pin`, `/device`. Phone tabs are Search, Lattice, Folders, Preferences. Wide rail also has Models.

## Download rows

`ModelRow` with `AutoStart` true is on both models routes. Text, vision, audio, and the helper row each show the same bar, verified label, hash failure, and retry. The helper row is labeled `Models_Helpers`, shows no model id, and tracks the five OCR pack files on its own snapshot. OCR download progress does not write the text snapshot. While vision is downloading, the audio row mirrors the vision snapshot because both use the same mmproj. Onboarding Continue is enabled only when the text phase is `Ready`.

## Header meters

`ResourceStrip` stays in the sidebar, not the header. Every five seconds it reads RAM in megabytes, CPU percent, and GPU percent when a reading exists, plus ingest counts per kind (`finished/seen` and percent). Under the counts it shows the last completed run, the last stopped file with its reason key, and `Ingest_ModelResident` while the pass is idle and the next retry is still in the future. `/activity` lists local ingest faults (time, reason, file name). Ingest status is `Ingest_Running` while a pass runs or a model is downloading or verifying, `Ingest_Stopped` with a scrollable path list and retry when files are still unfinished, otherwise `Ingest_Done`. The rail and the footer use that same status. They show `Ingest_Running` while it is Running, and `Status_Ready` only when it is Done. A Stopped pass keeps the retry line in the meter and does not say Ready. The timer does not start ingest.

## Find

Home has Find only. Group and Tag are not on the page. Enter calls `SubmitAsync`. A blank query does not call `FindAsync`. Text, picture, and audio searches time out after 45 seconds with `Search_TimedOut`. While results or a working line show, `Search_EmptyTitle` stays hidden. Results render in a grouped grid (Text, Images, Videos, Audio) with match strength, modified time, and reason labels; View and Play open a closable in-app modal instead of pushing `MediaPreviewPage`. The picked picture is a thumbnail chip with Remove, not a path. Picture and audio search icons sit inside the search field; Index now is not on Home. `Lattice` is the sync-off page, not a cluster graph. `Pin` is the empty state. A search card shows at most 480 characters of the stored patch, with an ellipsis when the patch is longer, and that paragraph is clamped to 8rem. Device and Pin each open with Back, which returns to `/preferences`. The Preferences item in the rail and the bottom tabs stays active on `/device` and `/pin`. Preferences stores theme, locale, and age, shows the skipped-files groups with the canon heading, three labels, and the add button, and links to folders, models, pin, and device. Preferences does not show a desktop section. A tray, sign-in start, and a search shortcut are open on [desktop presence](desktop-presence.md). The default key is Space, and the person can replace it with a letter.
