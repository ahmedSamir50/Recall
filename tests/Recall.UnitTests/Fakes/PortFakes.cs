using Recall.Core.Ports;
using Recall.Core.Sync;

namespace Recall.UnitTests.Fakes;

public sealed class FakeFileScopeWatcherService : IFileScopeWatcherService
{
}

public sealed class FakeScopeManagerService : IScopeManagerService
{
}

public sealed class FakeVideoFrameSamplerService : IVideoFrameSamplerService
{
}

public sealed class FakeSecureStorageService : ISecureStorageService
{
}

public sealed class FakeIdleDetectorService : IIdleDetectorService
{
}

public sealed class FakeModelDownloadService : IModelDownloadService
{
}

public sealed class FakeIndexPolicy : IIndexPolicy
{
}

public sealed class FakeSyncAdapter : ISyncAdapter
{
}
