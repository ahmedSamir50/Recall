Status: in-code

## Record

`DeviceRecordState` holds device id, original OS name, and display name in Preferences key `recall.device.record`. `EnsureLoaded` returns the cache, else the stored record, else creates one. `TrySetDisplayName` returns `Empty` for blank, `Unchanged` when equal, `Saved` after `store.Save`. `DeviceIdRules.ChooseId` keeps a ready acceptable platform id and uses `IRandomDeviceIdSource` when the candidate is empty, whitespace, or known-bad Android id `9774d56d682e549c`. Serial, IMEI, UDID, advertising id, and Mac `IOPlatformUUID` are not read. The id and both names are not sent on the download. See [ADR 0001](../adr/0001-platform-device-id.md).

## Failure

`GetCandidate` not ready throws `DeviceIdNotReadyException`.
