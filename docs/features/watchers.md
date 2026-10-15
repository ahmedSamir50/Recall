Status: decided-absent

## Interfaces only

`IFileScopeWatcherService`, `IVideoFrameSamplerService`, and `IIdleDetectorService` exist in `ScopePorts.cs`. No class in `src/` implements them. `ISecureStorageService` exists and has no implementing class. Accepted rules, not code: Android SAF or `FileObserver` only where SAF does not apply; Windows `FileSystemWatcher`; Mac Catalyst `FSEventStream`; iOS has no background watch and would diff on foreground. `checkpoints.db` is not in `src/`. Tasks 3.2.1, 3.2.2, 3.2.3, and 3.3.1 are not started.
