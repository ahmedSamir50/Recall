using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Runtime;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Vectors;
using Recall.Core.Vocabulary;

namespace Recall.Core.Text;

public sealed record TextIngestChunk(string ContentId, int Ordinal, bool Dense = true, int PageStart = 0, int PageEnd = 0, int CharStart = 0, int CharEnd = 0, string Bm25Raw = "");

public sealed record TextIngestResult(
    string ContentHash,
    IReadOnlyList<TextIngestChunk> Chunks,
    IReadOnlyList<int> OcrPageNumbers)
{
    public IReadOnlyList<int> ImageFastPathPages { get; init; } = [];

    public IngestStopReason Stop { get; init; }
}

public sealed class TextIngestEngine
{
    public const int IngestBatchChunkCount = 64;

    private readonly ITextEmbeddingEncoder encoder;
    private readonly ITextVectorIndex index;
    private readonly IIndexPolicy indexPolicy;
    private readonly ITextTokenCounter tokenCounter;
    private readonly LocalOpLogWriter opLog;
    private readonly IIngestCursorStore cursors;
    private readonly IMemoryBudget memory;
    private readonly MemoryGate gate;

    public TextIngestEngine(
        ITextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        IIndexPolicy indexPolicy,
        ITextTokenCounter tokenCounter,
        LocalOpLogWriter opLog,
        IIngestCursorStore cursors,
        IMemoryBudget memory,
        MemoryGate gate)
    {
        this.encoder = encoder;
        this.index = index;
        this.indexPolicy = indexPolicy;
        this.tokenCounter = tokenCounter;
        this.opLog = opLog;
        this.cursors = cursors;
        this.memory = memory;
        this.gate = gate;
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
        var title = Path.GetFileName(filePath);
        string documentText;
        IReadOnlyList<PdfPageText>? pdfPages = null;
        IReadOnlyList<int> ocrPageNumbers = [];
        IReadOnlyList<int> imageFastPathPages = [];
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
            imageFastPathPages = split.ImageFastPathPages;
        }
        else
        {
            documentText = rawText;
        }

        if (IsPdfPath(filePath) && string.IsNullOrWhiteSpace(documentText))
        {
            return new TextIngestResult(ContentIdBuilder.HashSourceContent(string.Empty), [], ocrPageNumbers)
            {
                ImageFastPathPages = imageFastPathPages,
            };
        }

        var sourceHash = ContentIdBuilder.HashSourceContent(documentText);
        var prefixTokenCount = tokenCounter.CountTokens(EmbeddingTaskPrefixes.TitledPrefix(title));
        var ends = tokenCounter.TokenEnds(documentText);
        var plan = DocumentPatchPlanner.Plan(title, documentText, pdfPages, prefixTokenCount, ends);
        var (chunks, stop) = await InsertPatchesAsync(title, filePath, sourceHash, plan.Patches, modifiedAt, grantPath, encoderHash, codeFile, cancellationToken).ConfigureAwait(false);
        if (plan.Truncated)
        {
            stop = IngestStopReason.PatchCap;
        }

        return new TextIngestResult(sourceHash, chunks, ocrPageNumbers)
        {
            ImageFastPathPages = imageFastPathPages,
            Stop = stop,
        };
    }

    public async Task IngestOcrPagesAsync(
        string filePath,
        IReadOnlyList<PdfPageText> pages,
        string grantPath,
        string encoderHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(pages);
        var title = Path.GetFileName(filePath);
        var marked = new List<PdfPageText>(pages.Count);
        var builder = new System.Text.StringBuilder();
        for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            var page = pages[pageIndex];
            var text = pageIndex < pages.Count - 1 ? page.Text + '\f' : page.Text;
            marked.Add(new PdfPageText(page.PageNumber, text));
            builder.Append(text);
        }

        var documentText = builder.ToString();
        var sourceHash = ContentIdBuilder.HashSourceContent(documentText);
        var prefixTokenCount = tokenCounter.CountTokens(EmbeddingTaskPrefixes.TitledPrefix(title));
        var ends = tokenCounter.TokenEnds(documentText);
        var plan = DocumentPatchPlanner.Plan(title, documentText, marked, prefixTokenCount, ends);
        var (chunks, stop) = await InsertPatchesAsync(
            title,
            filePath,
            sourceHash,
            plan.Patches,
            File.GetLastWriteTimeUtc(filePath),
            grantPath,
            encoderHash,
            codeFile: false,
            cancellationToken).ConfigureAwait(false);
        if (stop == IngestStopReason.MemoryBudget)
        {
            cursors.MarkStopped(filePath, IngestStopKeys.MemoryBudget);
            return;
        }

        var writes = new List<ChunkWrite>(chunks.Count);
        var bm25Only = new List<Bm25OnlyRow>();
        foreach (var chunk in chunks)
        {
            writes.Add(new ChunkWrite(chunk.ContentId, chunk.Ordinal, chunk.PageStart, chunk.PageEnd, chunk.CharStart, chunk.CharEnd));
            if (!chunk.Dense)
            {
                bm25Only.Add(new Bm25OnlyRow(chunk.ContentId, filePath, chunk.Bm25Raw));
            }
        }

        cursors.FinishPath(filePath, sourceHash, grantPath, encoderHash, writes, bm25Only);        cursors.ReplaceOcrPages(filePath, []);
    }

    private async Task<(List<TextIngestChunk> Chunks, IngestStopReason Stop)> InsertPatchesAsync(
        string title,
        string filePath,
        string sourceHash,
        IReadOnlyList<PdfPatch> patches,
        DateTimeOffset modifiedAt,
        string grantPath,
        string encoderHash,
        bool codeFile,
        CancellationToken cancellationToken)
    {
        var storedWidth = (int)WidthOnEdit.Choose(Modality.Text, indexPolicy.TextStoredWidth, pinned: false, pastCutoff: false);
        indexPolicy.EnsureStorableWidth(Modality.Text, storedWidth);
        var resultChunks = new List<TextIngestChunk>();
        var pending = new List<ChunkWrite>(IngestBatchChunkCount);
        var stop = IngestStopReason.None;
        foreach (var patch in patches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (memory.Pressure == MemoryPressure.Hard)
            {
                gate.EnterHard(DateTimeOffset.UtcNow);
            }

            if (!gate.IsOpen())
            {
                stop = IngestStopReason.MemoryBudget;
                break;
            }

            var contentId = ContentIdBuilder.Build(Modality.Text, sourceHash, StoredWidth.W512, patch.Ordinal);
            if (!NumericSpanPolicy.IsDense(patch.Raw, codeFile))
            {
                var sparse = new TextIngestChunk(
                    contentId,
                    patch.Ordinal,
                    false,
                    patch.PageStart,
                    patch.PageEnd,
                    patch.CharStart,
                    patch.CharEnd,
                    patch.Raw);
                resultChunks.Add(sparse);
                RememberChunk(filePath, pending, sparse);
                continue;
            }

            if (cursors.AnyLivePath(contentId))
            {
                var skipped = new TextIngestChunk(
                    contentId,
                    patch.Ordinal,
                    true,
                    patch.PageStart,
                    patch.PageEnd,
                    patch.CharStart,
                    patch.CharEnd);
                resultChunks.Add(skipped);
                RememberChunk(filePath, pending, skipped);
                continue;
            }

            var vector512 = await CutStoredAsync(title, patch.Raw, storedWidth, cancellationToken).ConfigureAwait(false);
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

            var stored = new TextIngestChunk(
                contentId,
                patch.Ordinal,
                true,
                patch.PageStart,
                patch.PageEnd,
                patch.CharStart,
                patch.CharEnd);
            resultChunks.Add(stored);
            RememberChunk(filePath, pending, stored);
        }

        FlushChunks(filePath, pending);
        return (resultChunks, stop);
    }

    private async Task<ReadOnlyMemory<float>> CutStoredAsync(string title, string raw, int storedWidth, CancellationToken cancellationToken)
    {
        var full768 = await encoder.EmbedPrefixedTextAsync(EmbeddingTaskPrefixes.WrapTitledDocument(title, raw), cancellationToken).ConfigureAwait(false);
        var stored = new float[storedWidth];
        MatryoshkaVectorMath.CutAndNormalize(full768.Span, stored);
        return stored;
    }

    private void RememberChunk(string filePath, List<ChunkWrite> pending, TextIngestChunk chunk)
    {
        pending.Add(new ChunkWrite(chunk.ContentId, chunk.Ordinal, chunk.PageStart, chunk.PageEnd, chunk.CharStart, chunk.CharEnd));
        if (pending.Count >= IngestBatchChunkCount)
        {
            FlushChunks(filePath, pending);
        }
    }

    private void FlushChunks(string filePath, List<ChunkWrite> pending)
    {
        if (pending.Count == 0)
        {
            return;
        }

        cursors.AppendLiveChunks(filePath, pending);
        pending.Clear();
    }

    private static bool IsPdfPath(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".pdf", StringComparison.OrdinalIgnoreCase);
}
