# ADR 0001: Platform device id

Status: accepted, 13 Oct 2026.

## Context

Recall needs a stable device id for local-only identity. v1 does not sync. The model download must not include device id or names.

## Decision

Store the public platform id on first launch when it is ready and acceptable:

- Android: `Settings.Secure.ANDROID_ID`, rejecting `9774d56d682e549c`.
- iOS and Mac Catalyst: `identifierForVendor` (not saved when not ready).
- Windows: `MachineGuid`.

When the candidate is empty, whitespace, or known-bad, store one random id (`Guid` without dashes). The first stored id wins. Serial, IMEI, UDID, advertising id, and Mac `IOPlatformUUID` are not read.

## Consequences

Canon and plan epic 1.3 describe platform id with random fallback. Privacy copy states the id stays on the device and is never sent.
