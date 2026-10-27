Status: in-code

## Actor

The person using the app after grants exist.

## Trigger

The text model is Ready and the window opens, or a prior ingest pass left paths in `Reading` or `Missing`.

## Path

`App.xaml.cs` calls `IngestGrantedTextAsync` for every grant when the window is created and the text snapshot is Ready. Finished grants are not skipped. The header `ResourceStrip` shows ingest status; when indexing stopped with unread paths, **Indexing stopped. Retry** calls `IngestGrantedTextAsync` again. Home does not offer Index now. `IIndexNowService` remains in the tree for tests and older call sites; the search page does not surface it.

## Failure

Unknown path returns. Text and Code both off returns. A throw sets the search fault key.

## Domains

Scope, Text. Feature page: [folder-grants](../features/folder-grants.md).
