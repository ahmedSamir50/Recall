Status: in-code

## Actor

The person.

## Trigger

Activate a hit card.

## Path

`TextFindSession.Build` marks a card unavailable when `ILocalFileOpener.Exists` is false. Activating an unavailable card returns without `OpenAsync`. Otherwise Home calls `OpenAsync`. `LocalFileOpenPolicy.Classify` then `Decide` chooses the result.

## Failure

`Missing` sets that card unavailable. `Failed` sets fault key `Search_OpenFailed` and does not open.

## Domains

Shell, Search. Feature page: [open-file](../features/open-file.md).
