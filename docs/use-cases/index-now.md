Status: in-code

## Actor

The person on the search page.

## Trigger

Home lists a granted folder with no finished `grant_scan` row.

## Path

`IIndexNowService.ListOffers` offers that folder; a child of a grant in the list is not offered. **Index now** calls `IngestGrantAsync` for that path and reloads offers.

## Failure

Unknown path returns. Text and Code both off returns. A throw sets the search fault key.

## Domains

Scope, Text. Feature page: [folder-grants](../features/folder-grants.md).
