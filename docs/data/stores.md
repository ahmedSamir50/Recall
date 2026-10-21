Status: in-code

## On device

MAUI Preferences keys `recall.onboarding.complete`, `recall.scope.grants`, `recall.preferences.ageCutoff`, `recall.locale`, `recall.device.record`, `recall.theme`, `recall.skips.user`. App-data `models/text`, `zvec/items_text`, `oplog/yyyy-MM-dd.log`, optional `appsettings.secrets.json`.

## Checkpoints

`checkpoints.db` is on device. It stores paths, hashes, and states, not file text. No copied source archive. `ResetTextIngest` deletes those rows when a stale `items_text` collection is replaced.

## Not on device

No server.

Failure: a missing preferences value uses the store default named on the [preferences](preferences.md) page.
