Status: in-code

## Actor

The person.

## Trigger

Edit the display name on `/device`.

## Path

Blank returns `Empty`. The same name returns `Unchanged`. Any other name returns `Saved`. The device id and the original OS name stay.

## Failure

A not-ready id on first load throws `DeviceIdNotReadyException`. The names are not sent on the download.

## Domains

Shell, Device. Feature page is the [device-record](../data/device-record.md) data page.
