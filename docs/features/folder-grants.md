Status: in-code

## Add

`TryAddGrant` normalizes with trim, slash to backslash, trim trailing backslash. Duplicate path returns `Duplicate` and does not save. A new path that `IsChildPath` of an existing grant returns `OverlapChildOfExistingParent` and does not save. Any other path is saved as `Added`. `RemoveGrant` drops a matching path.

## Kinds

`SetGrantFileKinds` throws `RecallFaultException` `GrantFileKindsEmpty` when kinds are `None`. A missing path returns without saving. `CanContinue` is true when the list is not empty.

## Search filter

`IsInsideGrant` is true for the same path or a child. Blank paths are false. Equal paths are not children. `D:\Photos` is not inside `D:\PhotosExtra`. Search builds the path set only from grants whose kinds include `Text`. An empty set makes `FindAsync` return an empty list.

## Not done

Adding a parent does not remove existing children. That is plan task 3.1.1, status decided-absent. Index now is task 3.1.2, status decided-absent.
