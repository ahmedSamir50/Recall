Status: in-code

## Decision

`LocalFileOpenPolicy.Classify` returns `Empty` for null or white space, `ContentUri` when the path starts with `ContentUri.Scheme`, otherwise `Filesystem`. `Decide`: `threw` returns `Failed`; empty probe returns `Failed`; content URI returns `Opened` when `platformAccepted` else `Failed`; missing filesystem returns `Missing`; filesystem returns `Opened` when `launcherOpened` else `Failed`. `Exists` is false for empty, the content-stream flag for a content URI, and the filesystem flag otherwise. Home uses `ILocalFileOpener`. A missing file stays on the card as unavailable. A code hit opens through the same path because the stored path is the file.
