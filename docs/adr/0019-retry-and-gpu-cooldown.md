# ADR 0019 — Retry clock and GPU cooldown

Status: accepted, 15 November 2026.

## Decision

Unfinished ingest rows retry on a fifteen-second clock that reads cursor rows only, not the disk. The wait between attempts comes from Preferences (default 5 minutes, range 1–60). A tick runs ingest only when the memory gate is open, the text model is Ready, the GPU is at or below 80 °C, and the scheduled time has passed. A GPU reading above 80 °C pauses the current file in `Paused` state without unloading the text encoder.

## Consequence

The canon line that a background timer is not required is narrowed: this clock is allowed. Watchers still discover new files.