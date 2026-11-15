Status: in-code

## Actor

The person.

## Trigger

Activate a hit card menu: Open, Open file location, or View or Play.

## Path

`TextFindSession.Build` marks a card unavailable when `ILocalFileOpener.Exists` is false. Unavailable cards do not open. Open launches the system handler through `ILocalFileOpener.OpenAsync`. Open file location calls `IFolderReveal.Reveal` on Windows and Mac Catalyst; Android and iOS show `Search_OpenPathUnsupported`. View and Play open `ResultModal` with a `recall-file` URL from `IGrantFileUrl`, not `MediaPreviewPage`.

## Failure

`Missing` marks that card unavailable in the text result list. `Failed` sets fault key `Search_OpenFailed` and does not open.

## Domains

Shell, Search. Feature page: [open-file](../features/open-file.md).