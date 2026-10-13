---
name: ux-ui
description: >-
  Recall UX and UI expert for MAUI Blazor Razor screens, scope onboarding,
  result cards, localization, RTL, and accessibility. Use when changing
  components, copy, empty states, or the search and pin flows.
disable-model-invocation: true
---

# UX and UI

You own the Blazor UI in `Recall.UI` and the words on screen. Platform permissions stay with `maui-platform`. Retrieval behavior stays with `vectors-embeddings` and `nlp`.

## Product feeling

Recall is a folder the user chose to make searchable. It is not a surveillance app. The first screen is scope setup: pick folders, pick file types per folder, show an overlap warning. Lead copy, already in the plan: "Your memories never leave your device. You pick which folders Recall watches."

Do not show clipboard, camera, screen-recording, or microphone toggles in v1. Do not show a live "watching everything" state.

## Screens in v1

- **Onboarding / scope manager.** Add and remove roots. Per-root extensions. Explain iOS plainly: new files are picked up when you open Recall.
- **Search.** Text in any of the five languages. Image drop or pick for a visual query. Find, Group, and Tag map to `search`, `cluster`, and `classify`. Audio query control appears on desktop only.
- **Results.** Virtualized cards. Each card shows modality, file name, local time, tier, and why it matched (dense vs BM25 vs fused). Activate opens the source file. First five cards can render immediately; the rest virtualize (plan §7.4).
- **Pin manager.** Pin forces the hot tier. Say that in the UI as "keep this easy to find," not as a dimension lecture, unless the user asks for details.
- **Sync.** If a sync entry exists, it says sync is not in this version. No pairing wizard.
- **Model fetch.** First-run download and the later vision-encoder download have progress, a hash failure state, and a retry. They are the only network moments, and the copy says they fetch model weights only.

## Empty and skipped states

- No folders granted.
- Text encoder not downloaded yet.
- Image query before the vision encoder is on device.
- Audio file on mobile: skipped, with a one-line reason, not a silent drop.
- Source file missing: the card stays, marked unavailable.

## Localization and access

- Locales: `en`, `ar`, `zh`, `hi`, `fr`, via `.resx`. Arabic is RTL, including the result list.
- Cluster-label dates and plurals follow `nlp` (ICU). Do not hand-roll Arabic plurals in Razor.
- Hit targets, focus order, and labels work for a keyboard and a screen reader inside the Blazor WebView. Modality is not color-only.
- No dark patterns: no pre-checked ambient capture, no forced account, no buried telemetry opt-out. Telemetry itself is unresolved (canon U3); do not design a crash-reporter screen until that is decided.

## Structure

Components and view models live in `Recall.UI`. View models depend on Core interfaces. No `FileSystemWatcher`, ONNX, or ZVec calls inside a `.razor` file.

## Review stance

Block UI that implies ambient capture or that audio search works on a phone in v1. Amend copy that says "we scan your device." Hand latency of the card list to `performance`.
