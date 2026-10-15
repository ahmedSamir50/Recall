Status: in-code

## On device

MAUI Preferences keys `recall.onboarding.complete`, `recall.scope.grants`, `recall.preferences.ageCutoff`, `recall.locale`, `recall.device.record`, `recall.theme`. App-data `models/text`, `zvec/items_text`, `oplog/yyyy-MM-dd.log`, optional `appsettings.secrets.json`.

## Not on device

No `checkpoints.db`, no copied source archive, no server.

Failure: a missing preferences value uses the store default named on the [preferences](preferences.md) page.
