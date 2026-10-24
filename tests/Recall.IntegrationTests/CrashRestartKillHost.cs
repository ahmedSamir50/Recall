using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.IntegrationTests.Infrastructure;

namespace Recall.IntegrationTests;

public static class CrashRestartKillHost
{
    [ModuleInitializer]
    public static void Enter()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RECALL_CRASH_KILL"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var root = Environment.GetEnvironmentVariable("RECALL_CRASH_DIR");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        Run(root);
    }

    public static void Run(string root)
    {
        var modelDir = IntegrationTextModelPaths.ResolveModelDirectory();
        var grantPath = Path.Combine(root, "grant");
        Directory.CreateDirectory(grantPath);

        using var provider = IntegrationTestServiceCollection.Build(modelDir, Path.Combine(root, "zvec"));
        provider.GetRequiredService<IScopeManagerService>()
            .TryAddGrant(new ScopeFolderGrant(grantPath, "grant", GrantFileKinds.Text));

        // Task.Run's delegate would be compiled into this assembly. A second thread cannot enter
        // this module until Enter returns, so the poll below would wait forever. The ingest state
        // machine lives in Recall.Core and continues on the thread pool after the first await.
        _ = provider.GetRequiredService<IGrantedTextIngestService>()
            .IngestGrantedTextAsync(CancellationToken.None);

        var readingPath = Path.Combine(grantPath, "reading.txt");
        var checkpointPath = Path.Combine(root, "zvec", CheckpointSql.FileName);
        var deadline = Environment.TickCount64 + 120_000;

        while (Environment.TickCount64 < deadline)
        {
            var store = new SqliteIngestCursorStore(checkpointPath);
            var row = store.GetFile(readingPath);
            if (row is { State: IngestCursorState.Reading })
            {
                Process.GetCurrentProcess().Kill();
            }

            Thread.Sleep(50);
        }

        throw new InvalidOperationException("Reading row did not appear.");
    }
}
