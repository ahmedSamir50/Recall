using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Vectors;
using Recall.Core.Vocabulary;

namespace Recall.Core.Text;

public sealed record TextIngestChunk(string ContentId, int Ordinal, bool Dense = true, int PageStart = 0, int PageEnd = 0, int CharStart = 0, int CharEnd = 0);

public sealed record TextIngestResult(
    string ContentHash,
    IReadOnlyList<TextIngestChunk> Chunks,
    IReadOnlyList<int> OcrPageNumbers);

public sealed class TextIngestEngine
{
    private readonly ITextEmbeddingEncoder encoder;
    private readonly ITextVectorIndex index;
    private readonly IIndexPolicy indexPolicy;
    private readonly ITextTokenCounter tokenCounter;
    private readonly LocalOpLogWriter opLog;
    private readonly IIngestCursorStore cursors;

    public TextIngestEngine(
        ITextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        IIndexPolicy indexPolicy,
        ITextTokenCounter tokenCounter,
        LocalOpLogWriter opLog,
        IIngestCursorStore cursors)
    {
        this.encoder = encoder;
        this.index = index;
        this.indexPolicy = indexPolicy;
        this.tokenCounter = tokenCounter;
        this.opLog = opLog;
        this.cursors = cursors;
    }

    public async Task<TextIngestResult> IngestFileAsync(
        string filePath,
        string rawText,
        DateTimeOffset modifiedAt,
        string grantPath,
        string encoderHash,
        bool codeFile,
        CancellationToken cancellationToken)
    {
        var storedWidth = (int)WidthOnEdit.Choose(Modality.Text, indexPolicy.TextStoredWidth, pinned: false, pastCutoff: false);
        indexPolicy.EnsureStorableWidth(Modality.Text, storedWidth);

        var title = Path.GetFileName(filePath);
        string documentText;
        IReadOnlyList<PdfPageText>? pdfPages = null;
        IReadOnlyList<int> ocrPageNumbers = [];
        if (IsPdfPath(filePath))
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var pages = PdfPigPageSource.Read(stream);
            var split = DocumentPatchPlanner.SplitPdfPages(pages);
            documentText = split.JoinedDocument;
            pdfPages = split.EmbeddablePages;
            ocrPageNumbers = split.OcrPageNumbers;
        }
        else
        {
            documentText = rawText;
        }

        var sourceHash = ContentIdBuilder.HashSourceContent(documentText);
        var prefixTokenCount = tokenCounter.CountTokens(EmbeddingTaskPrefixes.TitledPrefix(title));
        var ends = tokenCounter.TokenEnds(documentText);
        var patches = DocumentPatchPlanner.Plan(title, documentText, pdfPages, prefixTokenCount, ends);

        var resultChunks = new List<TextIngestChunk>();
        foreach (var patch in patches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var contentId = ContentIdBuilder.Build(Modality.Text, sourceHash, StoredWidth.W512, patch.Ordinal);
            if (!NumericSpanPolicy.IsDense(patch.Raw, codeFile))
            {
                resultChunks.Add(new TextIngestChunk(
                    contentId,
                    patch.Ordinal,
                    false,
                    patch.PageStart,
                    patch.PageEnd,
                    patch.CharStart,
                    patch.CharEnd));
                continue;
            }

            if (cursors.AnyLivePath(contentId))
            {
                resultChunks.Add(new TextIngestChunk(
                    contentId,
                    patch.Ordinal,
                    true,
                    patch.PageStart,
                    patch.PageEnd,
                    patch.CharStart,
                    patch.CharEnd));
                cursors.AddAlias(contentId, filePath);
                continue;
            }

            var full768 = await encoder.EmbedPrefixedTextAsync(EmbeddingTaskPrefixes.WrapTitledDocument(title, patch.Raw), cancellationToken).ConfigureAwait(false);
            var vector512 = MatryoshkaVectorMath.CutAndNormalize(full768.Span, storedWidth);
            var folded = string.Join(' ', KeywordFold.Tokens(KeywordFold.Fold(patch.Raw)));
            var insert = new TextIndexInsert(
                contentId,
                filePath,
                title,
                patch.Raw,
                vector512,
                modifiedAt,
                encoderHash,
                true,
                grantPath,
                folded,
                patch.Ordinal);
            if (index.TryInsert(insert))
            {
                opLog.AppendInsert(contentId);
            }

            resultChunks.Add(new TextIngestChunk(
                contentId,
                patch.Ordinal,
                true,
                patch.PageStart,
                patch.PageEnd,
                patch.CharStart,
                patch.CharEnd));
        }

        return new TextIngestResult(sourceHash, resultChunks, ocrPageNumbers);
    }

    private static bool IsPdfPath(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".pdf", StringComparison.OrdinalIgnoreCase);
}
