Status: in-code

## In words

Scope is the permission boundary. Recall reads a file only when it sits inside a folder the person granted and the file kind on that grant is on. Adding a folder that is already inside a grant fails. Adding a folder that contains smaller grants replaces those smaller grants. Skip lists, gitignore, secret names, and cloud-only files are applied before any bytes are embedded. `ILocalContentProbe.Probe` reports whether the bytes are on this device. Windows uses cloud attributes. Apple reads ubiquitous-item status and does not start a download. Android skips `OpenInputStream` when `FromFlags` returns `Remote`. A SAF file without those flags is still read. Watching and the foreground rescan live next to this domain because they exist to notice changes inside a grant. They do not decide a new grant.

## Owns

Scope owns which folders and file kinds Recall may read. Types: `ScopeGrantService`, `ScopeGrantPolicy`, `ScopeFolderGrant`, `GrantFileKinds`. The flags in code are `None` 0, `Text` 1, `Image` 2, `Video` 4, `Audio` 8, `Code` 16, and `All` includes Code.

## Does not own

It does not own ingest or search scoring. Native subscriptions stay in the MAUI head.

## Use cases

Use cases: [onboard-and-grant](../use-cases/onboard-and-grant.md), [ingest-granted-text](../use-cases/ingest-granted-text.md), [find-text](../use-cases/find-text.md), [watch-a-grant](../use-cases/watch-a-grant.md), [index-now](../use-cases/index-now.md).

Scope owns overlap, the change decision, and the cursor policy.

## Store

Store key `recall.scope.grants`.
