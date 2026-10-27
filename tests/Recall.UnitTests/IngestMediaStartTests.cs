using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Download;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.Core.Sync;
using Recall.Core.Text;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class IngestMediaStartTests
{
    [Fact]
    public async Task IngestGrant_ImagesOnly_CallsIndexImageOnce()
    {
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Image);
        var scopeStore = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(scopeStore);
        scope.TryAddGrant(grant);
        var reader = new ImageListReader([@"D:\g\a.jpg"]);
        var (engine, opLog, cursors) = IngestTestKit.CreateEngine(new FakeTextEmbeddingEncoder(), new FakeTextVectorIndex());
        var vision = new RecordingVisionIngest();
        var service = new GrantedTextIngestService(scope, reader, engine, cursors, new FakeTextVectorIndex(), opLog, new FakeEncoderFileHash(), vision: vision);
        await service.IngestGrantAsync(grant.Path, TestContext.Current.CancellationToken);
        vision.ImageCalls.Should().Be(1);
    }

    [Fact]
    public async Task IngestGrant_ImagesOnly_NoVision_Throws()
    {
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Image);
        var scopeStore = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(scopeStore);
        scope.TryAddGrant(grant);
        var reader = new ImageListReader([@"D:\g\a.jpg"]);
        var (engine, opLog, cursors) = IngestTestKit.CreateEngine(new FakeTextEmbeddingEncoder(), new FakeTextVectorIndex());
        var service = new GrantedTextIngestService(scope, reader, engine, cursors, new FakeTextVectorIndex(), opLog, new FakeEncoderFileHash());
        var act = async () => await service.IngestGrantAsync(grant.Path, TestContext.Current.CancellationToken);
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Be(GrantedTextIngestService.PictureIngestNotRegistered);
    }

    [Fact]
    public async Task IngestGrant_OneThrow_Continues()
    {
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Text);
        var scopeStore = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(scopeStore);
        scope.TryAddGrant(grant);
        var reader = new DocumentReader(
        [
            new GrantedTextDocument(@"D:\g\first.txt", "first.txt", "x", DateTimeOffset.UtcNow),
            new GrantedTextDocument(@"D:\g\second.txt", "second.txt", "y", DateTimeOffset.UtcNow),
        ]);
        var index = new FakeTextVectorIndex();
        var throwOnce = new ThrowOnceEncoder();
        var (engine2, opLog2, cursors2) = IngestTestKit.CreateEngine(throwOnce, index);
        var service = IngestTestKit.CreateGrantedService(scope, reader, engine2, cursors2, index, opLog2);
        await service.IngestGrantAsync(grant.Path, TestContext.Current.CancellationToken);
        cursors2.GetFile(@"D:\g\first.txt")!.State.Should().Be(IngestCursorState.Missing);
        throwOnce.Calls.Should().Be(2);
    }

    [Fact]
    public async Task IngestGrant_TextThrow_StillCallsImage()
    {
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Text | GrantFileKinds.Image);
        var scopeStore = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(scopeStore);
        scope.TryAddGrant(grant);
        var reader = new ImageAndDocReader(
            [new GrantedTextDocument(@"D:\g\t.txt", "t.txt", "t", DateTimeOffset.UtcNow)],
            [@"D:\g\a.jpg"]);
        var throwOnce = new ThrowOnceEncoder();
        var (engine, opLog, cursors) = IngestTestKit.CreateEngine(throwOnce, new FakeTextVectorIndex());
        var vision = new RecordingVisionIngest();
        var service = new GrantedTextIngestService(scope, reader, engine, cursors, new FakeTextVectorIndex(), opLog, new FakeEncoderFileHash(), vision: vision);
        await service.IngestGrantAsync(grant.Path, TestContext.Current.CancellationToken);
        vision.ImageCalls.Should().Be(1);
    }

    [Fact]
    public async Task IngestGrant_Cancel_DoesNotMarkMissing()
    {
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Text);
        var scopeStore = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(scopeStore);
        scope.TryAddGrant(grant);
        var (engine, opLog, cursors) = IngestTestKit.CreateEngine(new FakeTextEmbeddingEncoder(), new FakeTextVectorIndex());
        var cancelReader = new CancelOnReadReader();
        var service = IngestTestKit.CreateGrantedService(scope, cancelReader, engine, cursors, new FakeTextVectorIndex(), opLog);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await FluentActions.Invoking(() => service.IngestGrantAsync(grant.Path, cts.Token)).Should().ThrowAsync<OperationCanceledException>();
        cursors.GetFile(@"D:\g\a.txt").Should().BeNull();
    }

    [Fact]
    public async Task IngestGrant_FinishedGrant_StillListsImages()
    {
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Image);
        var scopeStore = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(scopeStore);
        scope.TryAddGrant(grant);
        var reader = new ImageListReader([@"D:\g\a.jpg"]);
        var (engine, opLog, cursors) = IngestTestKit.CreateEngine(new FakeTextEmbeddingEncoder(), new FakeTextVectorIndex());
        cursors.MarkGrantFinished(grant.Path);
        var vision = new RecordingVisionIngest();
        var service = new GrantedTextIngestService(scope, reader, engine, cursors, new FakeTextVectorIndex(), opLog, new FakeEncoderFileHash(), vision: vision);
        await service.IngestGrantAsync(grant.Path, TestContext.Current.CancellationToken);
        vision.ImageCalls.Should().Be(1);
    }

    [Fact]
    public async Task IngestGrant_VisionNotReady_DoesNotMarkMissing()
    {
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Image);
        var scopeStore = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(scopeStore);
        scope.TryAddGrant(grant);
        var reader = new ImageListReader([@"D:\g\a.jpg"]);
        var (engine, opLog, cursors) = IngestTestKit.CreateEngine(new FakeTextEmbeddingEncoder(), new FakeTextVectorIndex());
        var vision = new RecordingVisionIngest();
        var downloads = new FakeModelDownloadService { VisionSnapshot = new TextModelDownloadSnapshot(TextModelDownloadPhase.Downloading, 0, null) };
        var service = new GrantedTextIngestService(scope, reader, engine, cursors, new FakeTextVectorIndex(), opLog, new FakeEncoderFileHash(), vision: vision, downloads: downloads);
        await service.IngestGrantAsync(grant.Path, TestContext.Current.CancellationToken);
        vision.ImageCalls.Should().Be(0);
        cursors.GetFile(@"D:\g\a.jpg").Should().BeNull();
    }

    [Fact]
    public async Task IndexImage_FinishedSameHash_DoesNotEmbed()
    {
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Image);
        var path = Path.Combine(Path.GetTempPath(), "ingest-skip-" + Guid.NewGuid().ToString("N") + ".jpg");
        await File.WriteAllBytesAsync(path, [0xFF, 0xD8, 0xFF, 0xD9], TestContext.Current.CancellationToken);
        try
        {
            var hash = ContentIdBuilder.HashBytes(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            var cursors = new FakeIngestCursorStore();
            cursors.MarkReading(path, hash, grant.Path);
            cursors.FinishPath(path, hash, grant.Path, "enc", [new ChunkWrite("id", 0)]);
            var encoder = new RecordingVisionEncoder();
            var ingest = new VisionFileIngest(encoder, new FakeMediaVectorIndex(), new FakeEncoderFileHash(), cursors);
            await ingest.IndexImageAsync(grant, path, TestContext.Current.CancellationToken);
            encoder.Calls.Should().Be(0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task IngestGranted_SecondCall_DoesNotEnterReader()
    {
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Text);
        var scopeStore = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(scopeStore);
        scope.TryAddGrant(grant);
        var reader = new BlockingCountingReader();
        var (engine, opLog, cursors) = IngestTestKit.CreateEngine(new FakeTextEmbeddingEncoder(), new FakeTextVectorIndex());
        var service = new GrantedTextIngestService(scope, reader, engine, cursors, new FakeTextVectorIndex(), opLog, new FakeEncoderFileHash());
        var first = service.IngestGrantedTextAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await service.IngestGrantedTextAsync(TestContext.Current.CancellationToken);
        reader.ReadCalls.Should().Be(1);
        reader.Release();
        await first;
    }

    [Fact]
    public async Task IngestGranted_Pending_RunsSecondPass()
    {
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Text);
        var scopeStore = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(scopeStore);
        scope.TryAddGrant(grant);
        var reader = new BlockingCountingReader();
        var (engine, opLog, cursors) = IngestTestKit.CreateEngine(new FakeTextEmbeddingEncoder(), new FakeTextVectorIndex());
        var service = new GrantedTextIngestService(scope, reader, engine, cursors, new FakeTextVectorIndex(), opLog, new FakeEncoderFileHash());
        var first = service.IngestGrantedTextAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await service.IngestGrantedTextAsync(TestContext.Current.CancellationToken);
        reader.Release();
        await first;
        reader.ReadCalls.Should().Be(2);
    }

    [Fact]
    public void ListUnfinished_Missing_IsListed()
    {
        var cursors = new FakeIngestCursorStore();
        var grant = new ScopeFolderGrant(@"D:\g", "g", GrantFileKinds.Text);
        cursors.MarkReading(@"D:\g\missing.txt", "h1", grant.Path);
        cursors.MarkMissing(@"D:\g\missing.txt");
        cursors.MarkReading(@"D:\g\done.txt", "h2", grant.Path);
        cursors.FinishPath(@"D:\g\done.txt", "h2", grant.Path, "e", [new ChunkWrite("c", 0)]);
        var status = new IngestStatusService(cursors);
        var list = status.ListUnfinished([grant]);
        list.Should().Contain(@"D:\g\missing.txt");
        list.Should().NotContain(@"D:\g\done.txt");
    }

    private sealed class ImageListReader(IReadOnlyList<string> images) : IGrantedTextReader
    {
        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<string> ListAudioAndVideoAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }

        public async IAsyncEnumerable<string> ListImagesAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var path in images)
            {
                yield return path;
            }

            await Task.CompletedTask;
        }
    }

    private sealed class DocumentReader(IReadOnlyList<GrantedTextDocument> docs) : IGrantedTextReader
    {
        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var doc in docs)
            {
                yield return doc;
            }

            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<string> ListAudioAndVideoAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }

        public async IAsyncEnumerable<string> ListImagesAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }
    }

    private sealed class ImageAndDocReader(IReadOnlyList<GrantedTextDocument> docs, IReadOnlyList<string> images) : IGrantedTextReader
    {
        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var doc in docs)
            {
                yield return doc;
            }

            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<string> ListAudioAndVideoAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }

        public async IAsyncEnumerable<string> ListImagesAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var path in images)
            {
                yield return path;
            }

            await Task.CompletedTask;
        }
    }

    private sealed class RecordingVisionIngest : IVisionFileIngest
    {
        public int ImageCalls { get; private set; }

        public Task IndexImageAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default)
        {
            ImageCalls++;
            return Task.CompletedTask;
        }

        public Task IndexVideoFramesAsync(ScopeFolderGrant grant, string path, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class RecordingVisionEncoder : IVisionEmbeddingEncoder
    {
        public int Calls { get; private set; }

        public void EmbedJpeg(ReadOnlyMemory<byte> jpeg, float[] destination768) => Calls++;
    }

    private sealed class ThrowOnceEncoder : ITextEmbeddingEncoder
    {
        public int Calls { get; private set; }

        public ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Calls == 1)
            {
                throw new IOException("fail");
            }

            var vector = new float[768];
            return ValueTask.FromResult<ReadOnlyMemory<float>>(vector);
        }
    }

    private sealed class CancelOnReadReader : IGrantedTextReader
    {
        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<string> ListAudioAndVideoAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }

        public async IAsyncEnumerable<string> ListImagesAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }
    }

    private sealed class BlockingCountingReader : IGrantedTextReader
    {
        private readonly SemaphoreSlim gate = new(0, 1);

        public int ReadCalls { get; private set; }

        public void Release() => gate.Release();

        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ReadCalls++;
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            yield break;
        }

        public async IAsyncEnumerable<string> ListAudioAndVideoAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }

        public async IAsyncEnumerable<string> ListImagesAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }
    }
}
