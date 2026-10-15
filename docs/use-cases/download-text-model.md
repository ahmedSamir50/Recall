Status: in-code

## Actor

The person opening models.

## Trigger

The text `ModelRow` first render when the phase is `NotStarted`, on `/onboarding/models` and on `/models`.

## Path

`DownloadTextModelAsync` places the body, `MarkVerifying` runs, the hash is checked, and Ready enables onboarding Continue. Image and audio rows do not start a download.

## Failure

`HashFailed`, `Interrupted`, and `Cancelled` offer retry.

## Domains

Shell, Download. Feature page: [model-download](../features/model-download.md).
