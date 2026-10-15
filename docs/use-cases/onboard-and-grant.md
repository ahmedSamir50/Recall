Status: in-code

## Actor

The person.

## Trigger

First launch, or Add folder.

## Path

Welcome goes to folders. `TryAddGrant` saves a new normalized path as `Added`. A duplicate returns `Duplicate` and does not save. A child of an existing parent returns `OverlapChildOfExistingParent` and does not save. The kind list includes Code. Continue stays disabled until `CanContinue`.

## Failure

File kinds `None` throws `GrantFileKindsEmpty`. Adding a parent does not remove existing children. That gap is task 3.1.1 and is not a step.

## Domains

Shell, Scope. Feature page: [folder-grants](../features/folder-grants.md).
