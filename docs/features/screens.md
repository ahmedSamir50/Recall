Status: in-code

## Routes

`/` Home, `/landing`, `/onboarding/welcome`, `/onboarding/folders`, `/onboarding/models`, `/onboarding/ready`, `/lattice`, `/folders`, `/models`, `/preferences`, `/pin`, `/device`. Phone tabs are Search, Lattice, Folders, Preferences. Wide rail also has Models.

## Download rows

`ModelRow` with `AutoStart` true is on both models routes. `OnAfterRenderAsync` starts `DownloadTextModelAsync` only when `Entry.Modality` is `Text` and the phase is `NotStarted`. Image and audio rows do not start a download. Onboarding Continue is enabled only when the text phase is `Ready`.

## Find

Home has Find only. Group and Tag are not on the page. Enter calls `SubmitAsync`. A blank query does not call `FindAsync`. A query that passes shows up to 20 hits, the first 5 immediate. `Lattice` is the sync-off page, not a cluster graph. `Pin` is the empty state. Preferences stores theme, locale, and age, and links to folders, models, pin, and device.
