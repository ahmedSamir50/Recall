Status: in-code

## Add

`TryAddGrant` normalizes with trim, slash to backslash, trim trailing backslash. Null grant throws `ArgumentNullException`; white-space path throws `ArgumentException` and does not save. Duplicate path returns `Duplicate` and does not save. A new path that `IsChildPath` of an existing grant returns `OverlapChildOfExistingParent` and does not save. A new parent that `IsChildPath` of stored children removes those children, saves the parent, and returns `OverlapParentReplacesChildren`. Both overlap statuses use `Folders_OverlapWarning`. `RemoveGrant` drops a matching path.

## Index now

Index now is in-code. `IIndexNowService.ListOffers` hides finished grants and children of allowed parents. `IndexAsync` ingests that folder.

## Kinds

`SetGrantFileKinds` throws `RecallFaultException` `GrantFileKindsEmpty` when kinds are `None`. A missing path returns without saving. `CanContinue` is true when the list is not empty.

## Search filter

`IsInsideGrant` is true for the same path or a child. Blank paths are false. Equal paths are not children. `D:\Photos` is not inside `D:\PhotosExtra`. Search builds the path set from grants whose kinds include `Text` or `Code`. A stored 15 does not gain Code. An empty set makes `FindAsync` return an empty list.

## Not done

Parent add removes children. That is plan task 3.1.1, done. Index now is task 3.1.2, done.
