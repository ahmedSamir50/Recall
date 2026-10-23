Status: in-code

## In words

The device record is a local name for this install. The first launch stores a platform device id and the original operating-system name. A blank id or the known-bad Android id is replaced with a random id. An id that is not ready yet throws, and the random id is not invented in that case. The person can change the display name. The download request does not include the id or either name.

## Owns

Device owns the local device id, the original OS name, and the display name. Types: `DeviceRecordService`, `DeviceIdRules`.

## Does not own

It does not send them on the download.

## Use cases

Use case: [rename-device](../use-cases/rename-device.md).

## Store

Store key `recall.device.record`.
