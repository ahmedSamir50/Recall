Status: in-code

## Owns

Scope owns which folders and file kinds Recall may read. Types: `ScopeGrantService`, `ScopeGrantPolicy`, `ScopeFolderGrant`, `GrantFileKinds`. The flags in code are `None` 0, `Text` 1, `Image` 2, `Video` 4, `Audio` 8, `Code` 16, and `All` includes Code.

## Does not own

It does not own ingest, search scoring, or a watcher.

## Use cases

Use cases: [onboard-and-grant](../use-cases/onboard-and-grant.md), [ingest-granted-text](../use-cases/ingest-granted-text.md), [find-text](../use-cases/find-text.md). Watch and index-now are on the [absent](../use-cases/absent.md) use-case page.

## Store

Store key `recall.scope.grants`.
