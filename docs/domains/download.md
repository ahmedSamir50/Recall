Status: in-code

## In words

Download is the only network path. It fetches a public weight file into app data and checks the SHA-256 before the file counts as ready. The text weight is what onboarding waits for. Home starts the vision download when a picture search is waiting. The audio method does not fetch a third file. It follows the vision download and copies that snapshot. The models screen does not auto-start the image or audio rows.

## Owns

Download owns the only network path, the hash check, and resume placement for the text file and the vision file. The audio phase follows the vision phase. Types: `DownloadTransfer`, `TextModelDownloadUiPolicy`, `MauiModelDownloadService`, `AudioDownloadJoinPolicy`.

## Does not own

It does not send a document, a vector, or the device record.

## Use cases

Use case: [download-text-model](../use-cases/download-text-model.md).
