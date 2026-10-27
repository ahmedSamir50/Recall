Status: in-code

## Add

`TryAddGrant` normalizes with trim, slash to backslash, trim trailing backslash. Null grant throws `ArgumentNullException`; white-space path throws `ArgumentException` and does not save. Duplicate path returns `Duplicate` and does not save. A new path that `IsChildPath` of an existing grant returns `OverlapChildOfExistingParent` and does not save. A new parent that `IsChildPath` of stored children removes those children, saves the parent, and returns `OverlapParentReplacesChildren`. Both overlap statuses use `Folders_OverlapWarning`. `RemoveGrant` drops a matching path.

## Window-open ingest

When the text model is Ready, the MAUI window starts `IngestGrantedTextAsync` for every grant, including grants already marked finished. Per grant, legs run only when that modality’s download phase is Ready: text and code, then images from `ListImagesAsync`, then video frames, then audio (and video soundtracks when Audio is on). One failed file is marked Missing and the pass continues. A retry in the header runs another pass when paths are still Reading or Missing.

## Kinds

`SetGrantFileKinds` throws `RecallFaultException` `GrantFileKindsEmpty` when kinds are `None`. A missing path returns without saving. `CanContinue` is true when the list is not empty.

## Search filter

`IsInsideGrant` is true for the same path or a child. Blank paths are false. Equal paths are not children. `D:\Photos` is not inside `D:\PhotosExtra`. Search builds the path set from grants whose kinds include `Text` or `Code`. A stored 15 does not gain Code. An empty set makes `FindAsync` return an empty list.

## Not done

Parent add removes children. That is plan task 3.1.1, done. Index now is task 3.1.2, done.
