using System.Diagnostics;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Privacy;
using Recall.Core.Scope;
using Recall.Core.Search;
using Recall.IntegrationTests.Infrastructure;

namespace Recall.IntegrationTests;

public sealed class CrashRestartTests : IDisposable
{
    private string? root;

    public void Dispose()
    {
        if (root is null || !Directory.Exists(root))
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                Directory.Delete(root, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 19)
            {
                Thread.Sleep(200);
                SqliteConnection.ClearAllPools();
            }
        }
    }

    [Fact]
    public async Task CrashRestart_ResumesFromCheckpoint()
    {
        root = Path.Combine(Path.GetTempPath(), "recall_crash_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var grantPath = Path.Combine(root, "grant");
        Directory.CreateDirectory(grantPath);

        if (Environment.ProcessPath is null)
        {
            throw new InvalidOperationException("Integration test host path is missing.");
        }

        var finishedPath = Path.Combine(grantPath, "finished.txt");
        await File.WriteAllTextAsync(finishedPath, "finished before the crash", TestContext.Current.CancellationToken);

        var modelDir = IntegrationTextModelPaths.ResolveModelDirectory();
        var checkpointPath = Path.Combine(root, "zvec", CheckpointSql.FileName);

        string finishedContentId;
        await using (var provider = IntegrationTestServiceCollection.Build(modelDir, Path.Combine(root, "zvec")))
        {
            var grant = new ScopeFolderGrant(grantPath, "grant", GrantFileKinds.Text);
            provider.GetRequiredService<IScopeManagerService>().TryAddGrant(grant);

            await provider.GetRequiredService<IGrantedTextIngestService>()
                .IngestGrantedTextAsync(TestContext.Current.CancellationToken);

            var store = provider.GetRequiredService<IIngestCursorStore>();
            finishedContentId = store.ListChunks(finishedPath).Single().ContentId;
            store.GetFile(finishedPath)!.State.Should().Be(IngestCursorState.Finished);
        }

        var readingPath = Path.Combine(grantPath, "reading.txt");
        await File.WriteAllTextAsync(readingPath, string.Concat(Enumerable.Repeat("crash restart window ", 4000)), TestContext.Current.CancellationToken);

        using var child = new Process
        {
            StartInfo =
            {
                FileName = Environment.ProcessPath,
                WorkingDirectory = Path.GetDirectoryName(Environment.ProcessPath)!,
                UseShellExecute = false,
            },
        };
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                child.StartInfo.Environment[key] = value;
            }
        }

        child.StartInfo.Environment["RECALL_CRASH_KILL"] = "1";
        child.StartInfo.Environment["RECALL_CRASH_DIR"] = root;
        child.Start();

        if (!child.WaitForExit(180_000))
        {
            child.Kill(entireProcessTree: true);
            throw new InvalidOperationException("Crash child did not exit.");
        }

        var cursorAfterKill = new SqliteIngestCursorStore(checkpointPath);
        cursorAfterKill.GetFile(readingPath)!.State.Should().Be(IngestCursorState.Reading);
        cursorAfterKill.GetFile(finishedPath)!.State.Should().Be(IngestCursorState.Finished);
        cursorAfterKill.ListChunks(finishedPath).Single().ContentId.Should().Be(finishedContentId);

        await using var resumeProvider = IntegrationTestServiceCollection.Build(modelDir, Path.Combine(root, "zvec"));
        var resumeGrant = new ScopeFolderGrant(grantPath, "grant", GrantFileKinds.Text);
        resumeProvider.GetRequiredService<IScopeManagerService>().TryAddGrant(resumeGrant);

        await resumeProvider.GetRequiredService<IGrantedTextIngestService>()
            .IngestGrantedTextAsync(TestContext.Current.CancellationToken);

        var cursorAfterResume = resumeProvider.GetRequiredService<IIngestCursorStore>();
        cursorAfterResume.GetFile(readingPath)!.State.Should().Be(IngestCursorState.Finished);
        cursorAfterResume.ListChunks(finishedPath).Single().ContentId.Should().Be(finishedContentId);

        var search = resumeProvider.GetRequiredService<ITextSearchService>();
        var grants = new List<ScopeFolderGrant> { resumeGrant };
        var hits = await search.FindAsync("crash restart window", grants, TestContext.Current.CancellationToken);
        hits.Should().Contain(h => h.SourcePath.EndsWith("reading.txt", StringComparison.OrdinalIgnoreCase));
        hits[0].SourcePath.EndsWith("reading.txt", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }

    [Fact]
    public async Task Egress_AfterDownload_ZeroNonLocalBytes()
    {
        var path = Path.Combine(Path.GetTempPath(), "recall_egress_integration_" + Guid.NewGuid().ToString("N") + ".log");
        await File.WriteAllLinesAsync(path, ["0\tdownload-finished\t0", "60000\t127.0.0.1\t1"], TestContext.Current.CancellationToken);

        var probe = new ProxyLogEgressProbe(path);
        var result = await probe.HasNonLocalByteAsync(TestContext.Current.CancellationToken);
        result.Should().BeFalse();
    }
}
