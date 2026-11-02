using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Runtime;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.Core.Vocabulary;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class IngestGuardTests
{
    [Fact]
    public void LocalOpLogWriter_KeepsTenThousandEntries()
    {
        var writer = new LocalOpLogWriter();

        for (var i = 0; i < 10_001; i++)
        {
            writer.AppendInsert("id-" + i.ToString("D5", System.Globalization.CultureInfo.InvariantCulture));
        }

        writer.Entries.Should().HaveCount(LocalOpLogWriter.MaxEntriesInMemory);
        writer.Entries[^1].ContentId.Should().Be("id-10000");
        writer.Entries[0].ContentId.Should().Be("id-00001");
        writer.Entries.Should().OnlyContain(e => e.Operation == OpLogOperation.Insert);
    }

    [Fact]
    public void Starts_CapsAtMaxFrames()
    {
        FrameCadence.Starts(10_000).Should().HaveCount(FrameCadence.MaxFramesPerFile);
        FrameCadence.Starts(0).Should().BeEmpty();
        FrameCadence.Starts(5).Should().Equal(0d, 2d, 4d);
    }

    [Fact]
    public void Chrome_DeviceBelowMinimum_IsBlocked()
    {
        var status = IngestChromePolicy.Status(
            false,
            Recall.Core.Download.TextModelDownloadPhase.Ready,
            Recall.Core.Download.TextModelDownloadPhase.Ready,
            Recall.Core.Download.TextModelDownloadPhase.Ready,
            false,
            DeviceVerdict.BelowMinimum);

        status.Should().Be(IngestChromeStatus.Blocked);
        Recall.UI.Layout.ShellStatusLabel.Key(status).Should().Be(IngestStopKeys.DeviceTooSmall);
    }

    [Fact]
    public void Reclaimer_ReleasesLoadedAudioOnly()
    {
        var loaded = new FakeAudioEmbeddingEncoder();
        loaded.EmbedPcm(ReadOnlyMemory<float>.Empty, new float[768]);
        var reclaimer = new IngestResourceReclaimer(loaded);

        reclaimer.ReleaseOptionalEncoders();

        loaded.CloseCalls.Should().Be(1);

        var idle = new FakeAudioEmbeddingEncoder();
        new IngestResourceReclaimer(idle).ReleaseOptionalEncoders();
        idle.CloseCalls.Should().Be(0);
    }

    [Fact]
    public async Task Index_OversizedVideo_MarksTooLarge()
    {
        var path = Path.Combine(Path.GetTempPath(), "recall-oversize-" + Guid.NewGuid().ToString("N") + ".mp4");
        await File.WriteAllBytesAsync(path, new byte[16], TestContext.Current.CancellationToken);
        try
        {
            var cursors = new FakeIngestCursorStore();
            cursors.MarkReading(path, "hash", @"C:\G");
            var encoder = new FakeVisionEmbeddingEncoder();
            var budget = TestIngest.Memory();
            budget.RefuseFiles = true;
            var vision = new VisionFileIngest(
                encoder,
                new FakeMediaVectorIndex(),
                new FakeEncoderFileHash(),
                cursors,
                budget,
                new FakeVideoFrameSamplerService());
            var grant = new ScopeFolderGrant(Path.GetDirectoryName(path)!, "G", GrantFileKinds.Video);

            await vision.IndexVideoFramesAsync(grant, path, TestContext.Current.CancellationToken);

            encoder.Calls.Should().Be(0);
            budget.FileChecks.Should().Be(1);
            cursors.GetFile(path)!.MissingStatusKey.Should().Be(IngestStopKeys.FileTooLarge);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Scan_StopsWhenGateIsClosed()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "a.txt");
        await File.WriteAllTextAsync(filePath, "hello", TestContext.Current.CancellationToken);
        try
        {
            var store = new FakeScopeGrantStore();
            var scope = new ScopeGrantService(store);
            scope.TryAddGrant(new ScopeFolderGrant(root, "temp", GrantFileKinds.Text));
            var cursors = new FakeIngestCursorStore();
            var ingest = new FakeGrantedTextIngestService();
            var applier = new ScopeChangeApplier(cursors, ingest, new LocalOpLogWriter(null));
            var budget = TestIngest.Memory();
            budget.Pressure = MemoryPressure.Hard;
            var scan = new ForegroundScanService(
                scope,
                IngestTestKit.CreateDirectoryReader(),
                cursors,
                applier,
                budget,
                new MemoryGate());

            await scan.ScanAsync(TestContext.Current.CancellationToken);

            ingest.IngestDocumentCalls.Should().Be(0);
            cursors.GetFile(filePath).Should().BeNull();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Ingest_MemoryHard_StopsBeforeEmbedding()
    {
        var encoder = new FakeTextEmbeddingEncoder();
        var (engine, _, cursors) = IngestTestKit.CreateEngineParts(encoder, new FakeTextVectorIndex());
        var budget = TestIngest.Memory();
        budget.Pressure = MemoryPressure.Hard;
        var gated = new TextIngestEngine(
            encoder,
            new FakeTextVectorIndex(),
            new FakeIndexPolicy(),
            new FakeTextTokenCounter(),
            new LocalOpLogWriter(null),
            cursors,
            budget,
            new MemoryGate());

        var result = await gated.IngestFileAsync(
            "/g/a.txt",
            "Intro. " + new string('X', 20000),
            DateTimeOffset.UtcNow,
            "/g",
            "enc",
            false,
            TestContext.Current.CancellationToken);

        result.Stop.Should().Be(IngestStopReason.MemoryBudget);
        encoder.Calls.Should().Be(0);
        engine.Should().NotBeNull();
    }

    [Fact]
    public async Task Ingest_ResumesWithoutReembeddingStoredChunks()
    {
        var encoder = new FakeTextEmbeddingEncoder();
        var (engine, _, cursors) = IngestTestKit.CreateEngineParts(encoder, new FakeTextVectorIndex());
        var text = "Intro. " + new string('X', 20000);
        var hash = ContentIdBuilder.HashSourceContent(text);
        cursors.AppendLiveChunk("/g/a.txt", new ChunkWrite(ContentIdBuilder.Build(Modality.Text, hash, StoredWidth.W512, 0), 0));

        var result = await engine.IngestFileAsync(
            "/g/a.txt",
            text,
            DateTimeOffset.UtcNow,
            "/g",
            "enc",
            false,
            TestContext.Current.CancellationToken);

        result.Stop.Should().Be(IngestStopReason.None);
        result.Chunks.Should().HaveCountGreaterThan(1);
        encoder.Calls.Should().Be(result.Chunks.Count - 1, "the already stored chunk must not be embedded again");
        cursors.AnyLivePath(ContentIdBuilder.Build(Modality.Text, hash, StoredWidth.W512, 0)).Should().BeTrue();
    }

    [Fact]
    public async Task Ingest_TruncatedDocument_StopsAtCap()
    {
        var encoder = new FakeTextEmbeddingEncoder();
        var (engine, _, _) = IngestTestKit.CreateEngineParts(encoder, new FakeTextVectorIndex());
        var text = string.Join(". ", Enumerable.Range(0, 600).Select(i => "Sentence " + i + " " + new string('z', 2200))) + ".";

        var result = await engine.IngestFileAsync(
            "/g/big.txt",
            text,
            DateTimeOffset.UtcNow,
            "/g",
            "enc",
            false,
            TestContext.Current.CancellationToken);

        result.Stop.Should().Be(IngestStopReason.PatchCap);
        result.Chunks.Should().HaveCount(DocumentPatchPlanner.MaxPatchesPerDocument);
    }

    [Fact]
    public async Task Index_OversizedAudio_MarksTooLarge()
    {
        var path = Path.Combine(Path.GetTempPath(), "recall-oversize-" + Guid.NewGuid().ToString("N") + ".wav");
        await File.WriteAllBytesAsync(path, new byte[16], TestContext.Current.CancellationToken);
        try
        {
            var cursors = new FakeIngestCursorStore();
            cursors.MarkReading(path, "hash", @"C:\G");
            var source = new FakeAudioSource();
            var budget = TestIngest.Memory();
            budget.RefuseFiles = true;
            var ingest = new AudioFileIngest(
                source,
                new FakeSiblingFiles(),
                new FakeAudioEmbeddingEncoder(),
                new FakeMediaVectorIndex(),
                new FakeAudioOffReport(),
                new FakeEncoderFileHash(),
                IngestTestKit.CreateDirectoryReader(),
                cursors,
                budget);
            var grant = new ScopeFolderGrant(Path.GetDirectoryName(path)!, "G", GrantFileKinds.Audio);

            await ingest.IndexAsync(grant, path, TestContext.Current.CancellationToken);

            source.ReadCalls.Should().Be(0);
            cursors.GetFile(path)!.MissingStatusKey.Should().Be(IngestStopKeys.FileTooLarge);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Reader_SkipsFileAboveTextCap()
    {
        var root = Path.Combine(Path.GetTempPath(), "recall-cap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var big = Path.Combine(root, "big.txt");
        var small = Path.Combine(root, "small.txt");
        await File.WriteAllTextAsync(big, new string('a', (int)LocalContentPolicy.MaxTextFileBytes + 1024), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(small, "hello", TestContext.Current.CancellationToken);
        try
        {
            var oversize = new OversizeFileSkipReport();
            var reader = new DirectoryGrantedTextReader(
                new FakeUserSkipStore(),
                new FakeLocalContentProbe(),
                new RemoteFileSkipReport(),
                new DatasetSkipReport(),
                oversize);
            var grant = new ScopeFolderGrant(root, "temp", GrantFileKinds.Text);
            var names = new List<string>();
            await foreach (var document in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                names.Add(document.FileName);
            }

            names.Should().ContainSingle().Which.Should().Be("small.txt");
            oversize.Paths.Should().ContainSingle().Which.Should().Be(big);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
