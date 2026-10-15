using Recall.Core.Ports;

namespace Recall.Core.Scope;

public sealed class WindowsFileScopeWatcher : IFileScopeWatcherService, IDisposable
{
    private readonly List<FileSystemWatcher> watchers = [];

    public bool Watching { get; private set; }

    public int WatcherCount => watchers.Count;

    public static bool CanWatchDirectories(bool isIos, bool isMacCatalyst)
    {
        if (isIos && !isMacCatalyst)
        {
            return false;
        }

        return true;
    }

    public void Start(IReadOnlyList<ScopeFolderGrant> grants, IScopeChangeSink sink)
    {
        ArgumentNullException.ThrowIfNull(grants);
        ArgumentNullException.ThrowIfNull(sink);
        Stop();
        if (!CanWatchDirectories(OperatingSystem.IsIOS(), OperatingSystem.IsMacCatalyst()))
        {
            Watching = false;
            sink.RequestRescan();
            return;
        }

        var roots = ScopeWatchRoots.Select(grants.Select(g => g?.Path ?? string.Empty).ToList());
        foreach (var path in roots)
        {
            if (ContentUri.IsContent(path))
            {
                continue;
            }

            if (!Directory.Exists(path))
            {
                continue;
            }

            var watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true,
            };
            watcher.Created += (_, args) =>
            {
                if (WindowsWatchEventMapper.TryMap(WindowsWatchKind.Created, args.FullPath, null, out var change))
                {
                    sink.Report(change);
                }
            };
            watcher.Changed += (_, args) =>
            {
                if (WindowsWatchEventMapper.TryMap(WindowsWatchKind.Changed, args.FullPath, null, out var change))
                {
                    sink.Report(change);
                }
            };
            watcher.Renamed += (_, args) =>
            {
                if (WindowsWatchEventMapper.TryMap(WindowsWatchKind.Renamed, args.FullPath, args.OldFullPath, out var change))
                {
                    sink.Report(change);
                }
            };
            watcher.Deleted += (_, args) =>
            {
                if (WindowsWatchEventMapper.TryMap(WindowsWatchKind.Deleted, args.FullPath, null, out var change))
                {
                    sink.Report(change);
                }
            };
            watcher.Error += (_, _) => sink.RequestRescan();
            watchers.Add(watcher);
        }

        if (watchers.Count == 0)
        {
            sink.RequestRescan();
        }

        Watching = watchers.Count > 0;
    }

    public void Stop()
    {
        foreach (var watcher in watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        watchers.Clear();
        Watching = false;
    }

    public void Dispose()
    {
        Stop();
    }
}
