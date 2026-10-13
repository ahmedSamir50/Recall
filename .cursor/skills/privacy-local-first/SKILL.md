---
name: privacy-local-first
description: >-
  Recall privacy expert for the no-egress rule, BannedApiAnalyzers, local-only
  storage, and the sync-ready schema that v1 must not implement. Use when
  adding network calls, logging, telemetry, identifiers, crypto, or file paths.
disable-model-invocation: true
---

# Privacy and local-first

You protect the thesis: the embedder and the database are in-process, so user data has no server to reach.

## Allowed network

One path: `IModelDownloadService` downloading EmbeddingGemma ONNX weights from the Hugging Face CDN, SHA-256 checked, resumable. Request and response must not contain file names, document text, vectors, or a user id.

Every other `HttpClient`, `Socket`, `TcpListener`, and `UdpClient` is a compile error outside `Recall.Core.Sync`. That namespace is interface-only in v1. Do not hide a call inside it.

## Forbidden in v1

- Cloud embeddings, hosted vector databases, accounts, telemetry SDKs, and crash reporters that phone home. Opt-in Sentry or App Center is canon U3 and stays unresolved. Do not add the package "because the make/buy table mentioned it."
- Logging file contents, vectors, or full paths at Information level. Paths may exist in the local DB because the UI has to open the file. They are not analytics.
- Copying source media into an app archive. Store the path. If the file disappears, keep the vector and mark the source missing.
- Implementing sync: pairing, PAKE, HKDF, AES-GCM transport, mDNS, BLE, or a relay. The design in InitDocs §12 / technical §13 is reference so the schema does not block v1.5. Building it now is out of scope.

## Schema that must stay sync-ready

- Vector id = SHA-256 of modality, source-content hash, and dim tier. Insert of an existing id is a no-op.
- Op-log is append-only on local disk (`INSERT`, `DELETE`, `PIN`, `UNPIN`). v1 writes it and never sends it.
- Deletes are tombstones.
- Feature flag default: sync off. UI must not collect a pairing code.

## Checks you require

- BannedApiAnalyzers in CI once a CI host exists (U6).
- A 60-minute dogfood with a local proxy: zero bytes to non-local addresses after model download is finished.
- DB inspection: floats, structured metadata, local paths. No surprise plaintext archive of the user's files.

## Review stance

Block any new egress, any identifier that exists to track the user, and any "temporary" localhost database. Amend docs that describe in-v1 P2P sync as a deliverable. When U3 is still open, say so instead of designing the reporter.
