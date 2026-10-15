Status: in-code for the ban, open for U3.

## Allowed

Allowed network is the model-weight download only.

## Forbidden

Forbidden: cloud embedder, hosted vector database, accounts, telemetry SDK, crash reporter that phones home, sync transport. Paths stay in the local collection so the UI can open the file. They are not analytics. Checkpoint paths stay on device. No new network path. No information log of a path or file text. Secrets, databases, lock files, and skip folders are not read, including when a gitignore exception names them.

## Open

U3: goal is zero telemetry; an opt-in reporter is unresolved; v1 does not add one.
