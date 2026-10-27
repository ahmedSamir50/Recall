using FluentAssertions;
using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.IntegrationTests.Infrastructure;
using Recall.UnitTests.Fakes;

namespace Recall.IntegrationTests;

public sealed class PdfPatchProofTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "recall_pdf_proof_" + Guid.NewGuid().ToString("N"));
    private readonly string oplogDir = Path.Combine(Path.GetTempPath(), "recall_pdf_oplog_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        if (Directory.Exists(oplogDir))
        {
            Directory.Delete(oplogDir, recursive: true);
        }
    }

    [Fact]
    public async Task Pdf_Oz_StoresMoreThanOneChunk()
    {
        DemoDatasetPaths.RequireRoot();
        var pdfPath = DemoDatasetPaths.Combine("pdf_text", "en_long_wonderful_wizard_of_oz_baum.pdf");
        File.Exists(pdfPath).Should().BeTrue($"demo pdf missing: {pdfPath}");

        Directory.CreateDirectory(root);
        var encoder = new FakeTextEmbeddingEncoder();
        var index = new FakeTextVectorIndex();
        var cursors = new FakeIngestCursorStore();
        var (engine, opLog, _) = IngestTestKit.CreateEngineParts(encoder, index, oplogDir);
        var scope = new ScopeGrantService(new FakeScopeGrantStore());
        var grant = new ScopeFolderGrant(root, "demo", GrantFileKinds.Text);
        scope.TryAddGrant(grant);
        var ingest = IngestTestKit.CreateGrantedService(scope, new EmptyGrantedTextReader(), engine, cursors, index, opLog);

        await using var pdfStream = File.OpenRead(pdfPath);
        var pdfPages = PdfPigPageSource.Read(pdfStream);
        var pdfText = string.Concat(pdfPages.Select(page => page.Text));
        pdfText.Should().NotBeNullOrWhiteSpace();
        var document = new GrantedTextDocument(
            pdfPath,
            Path.GetFileName(pdfPath),
            pdfText,
            File.GetLastWriteTimeUtc(pdfPath));

        await ingest.IngestDocumentAsync(grant, document, TestContext.Current.CancellationToken);
        var chunks = cursors.ListChunks(pdfPath);
        chunks.Should().NotBeEmpty();
        chunks.Count.Should().BeGreaterThan(1);
        chunks.Should().Contain(c => c.PageStart > 0);
    }

    private sealed class EmptyGrantedTextReader : IGrantedTextReader
    {
        public IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, CancellationToken cancellationToken) =>
            AsyncEnumerable.Empty<GrantedTextDocument>();

        public IAsyncEnumerable<string> ListAudioAndVideoAsync(ScopeFolderGrant grant, CancellationToken cancellationToken) =>
            AsyncEnumerable.Empty<string>();

        public IAsyncEnumerable<string> ListImagesAsync(ScopeFolderGrant grant, CancellationToken cancellationToken) =>
            AsyncEnumerable.Empty<string>();
    }
}
