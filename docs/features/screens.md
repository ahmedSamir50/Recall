Status: in-code

## Routes

`/` Home, `/landing`, `/onboarding/welcome`, `/onboarding/folders`, `/onboarding/models`, `/onboarding/ready`, `/lattice`, `/folders`, `/models`, `/preferences`, `/pin`, `/device`. Phone tabs are Search, Lattice, Folders, Preferences. Wide rail also has Models.

## Download rows

`ModelRow` with `AutoStart` true is on both models routes. Text, vision, audio, and the helper row each show the same bar, verified label, hash failure, and retry. The helper row is labeled `Models_Helpers`, shows no model id, and tracks the five OCR pack files on its own snapshot. OCR download progress does not write the text snapshot. While vision is downloading, the audio row mirrors the vision snapshot because both use the same mmproj. Onboarding Continue is enabled only when the text phase is `Ready`.

## Header meters

`ResourceStrip` stays in the sidebar, not the header. Every five seconds it reads RAM in megabytes, CPU percent, and GPU percent when a reading exists, plus ingest counts per kind (`finished/seen` and percent). Ingest status is `Ingest_Running` while a pass runs or a model is downloading or verifying, `Ingest_Stopped` with a scrollable path list and retry when files are still Reading or Missing, otherwise `Ingest_Done`. The timer does not start ingest.

## Find

Home has Find only. Group and Tag are not on the page. Enter calls `SubmitAsync`. A blank query does not call `FindAsync`. A query that passes shows up to 20 hits, the first 5 immediate. Picture and audio search icons sit inside the search field; Index now is not on Home. `Lattice` is the sync-off page, not a cluster graph. `Pin` is the empty state. Preferences stores theme, locale, and age, shows the skipped-files groups with the canon heading, three labels, and the add button, and links to folders, models, pin, and device. Preferences does not show a desktop section. A tray, sign-in start, and a search shortcut are open on [desktop presence](desktop-presence.md). The default key is Space, and the person can replace it with a letter.
