namespace Recall.Core.Ports;

/// <summary>Filesystem events while the process is running.</summary>
public interface IFileScopeWatcherService
{
}

/// <summary>Granted paths and the parent-wins tree check.</summary>
public interface IScopeManagerService
{
}

/// <summary>Platform video frame sampler.</summary>
public interface IVideoFrameSamplerService
{
}

/// <summary>Secure storage port. v1 stores no sync keys.</summary>
public interface ISecureStorageService
{
}

/// <summary>Pauses ingestion. It does not capture.</summary>
public interface IIdleDetectorService
{
}

/// <summary>The only network path. No method until the download epic.</summary>
public interface IModelDownloadService
{
}

/// <summary>Stored width, age cutoff, and enabled modalities. Rules stay in Core.</summary>
public interface IIndexPolicy
{
}
