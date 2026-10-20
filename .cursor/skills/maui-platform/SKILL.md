---
name: maui-platform
description: >-
  Recall MAUI platform expert for android-arm64, ios-arm64, win-x64, and
  maccatalyst-arm64, including scope watchers, the iOS foreground scan, video
  sampling, battery, and on-device model download. Use when changing platform
  heads, permissions, RIDs, or store constraints.
disable-model-invocation: true
---

# MAUI platform

You own the four shells and the native edges. Shared policy stays in `Recall.Core`. This skill is the platform head.

## Targets

| RID | Watcher | Dense index |
|---|---|---|
| `android-arm64` | Storage Access Framework document tree, or `FileObserver` only where SAF does not apply | plain HNSW. `QuantizeType` is canon U13 |
| `ios-arm64` | No background watch. On foreground, walk the granted scope and diff `checkpoints.db` | plain HNSW. `QuantizeType` is canon U13 |
| `win-x64` | `FileSystemWatcher` | plain HNSW. Do not create HNSW-RaBitQ |
| `maccatalyst-arm64` | `FSEventStream` | plain HNSW. `QuantizeType` is canon U13 |

`win-arm64` is not a v1 RID. iOS `ios-arm64` is a beta risk (plan R-1): if it fails, ship the other three and record the deferral. Do not drop Android to save iOS.

## Capture behavior

- Scope manager normalizes overlaps (`D:\` plus `D:\Photos` keeps the parent and warns).
- Events: created and modified enqueue ingest; renamed updates path metadata and re-embeds only when bytes change; deleted writes a tombstone and keeps the vector for a future sync reconcile.
- Video: 1 JPEG frame / 2 s through the platform sampler (Android `MediaMetadataRetriever`, iOS and Mac `AVAssetImageGenerator`, Windows `FFmpegInterop`). Batch those frames through the 440M encoder.
- Mobile audio: no microphone. If the user enables the audio file type, download the audio encoder, embed, then unload it. If they do not, record `skipped_modality=audio`.
- Missed files: Index now, and the search page may offer to index an allowed path that is not indexed yet (canon U4). Do not add a timer sweep.

## Model download

`IModelDownloadService` is the only HTTP client. It fetches encoder weights, verifies SHA-256, and can resume. It must not send device ids, file names, or vectors. Wire it so BannedApiAnalyzers still forbids every other socket.

## Store and power

- Privacy copy and review notes: the user picks folders; Recall does not watch the clipboard, camera, screen, or mic.
- Idle detection runs ingest while the device is idle and defers it while the device is busy. It is not for hidden capture.
- Battery budgets in the plan (≤ 4% / 30 min background ingest on a Pixel, ≤ 3% on iPhone) apply to in-scope file bursts, not to an always-on agent. There is no always-on agent in v1.
- Secure storage (Android Keystore, iOS Secure Enclave, Windows DPAPI, macOS Keychain) may exist as an interface. v1 has no sync keys to put there. Do not invent a pairing flow.

## Review stance

Block a llama-server process on any shell. android-x64 does not get a linux-x64 native loader.

Block background mic or screen capture, creating HNSW-RaBitQ, and a watcher design that assumes iOS gets `FileSystemWatcher` semantics. Amend M1/M2 week tables that still assign clipboard, camera roll, or phone voice memos (canon U7).

## Panel duty

A `@recall:` or `@reacall:` plan is signed in the parent chat after one read of the shared harness files. Do not launch a subagent. Do not re-read development-rules, test-honesty, sign-off, canon, InitDocs, or the worktree. Apply this checklist to the plan already in context. Push back when the plan is wrong, inconsistent, or missing a risk. Agree only after a checked list. Return the sign-off. Do not rubber-stamp.
