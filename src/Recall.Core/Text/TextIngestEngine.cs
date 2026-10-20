using Recall.Core.Checkpoints;
using Recall.Core.Ports;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Vectors;
using Recall.Core.Vocabulary;

namespace Recall.Core.Text;

public sealed record TextIngestChunk(string ContentId, int Ordinal, bool Dense = true);

public sealed record TextIngestResult(string ContentHash, IReadOnlyList<TextIngestChunk> Chunks);

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
        var sourceHash = ContentIdBuilder.HashSourceContent(rawText);
        var chunks = TextChunkPlanner.PlanChunks(title, rawText, tokenCounter.CountTokens);

        var resultChunks = new List<TextIngestChunk>();
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dense = NumericSpanPolicy.IsDense(chunk.RawText, codeFile);
            if (!dense)
            {
                resultChunks.Add(new TextIngestChunk(
                    ContentIdBuilder.Build(Modality.Text, sourceHash, StoredWidth.W512, chunk.Ordinal),
                    chunk.Ordinal,
                    false));
                continue;
            }

            if (cursors.AnyLivePath(ContentIdBuilder.Build(Modality.Text, sourceHash, StoredWidth.W512, chunk.Ordinal)))
            {
                resultChunks.Add(new TextIngestChunk(
                    ContentIdBuilder.Build(Modality.Text, sourceHash, StoredWidth.W512, chunk.Ordinal),
                    chunk.Ordinal));
                cursors.AddAlias(
                    ContentIdBuilder.Build(Modality.Text, sourceHash, StoredWidth.W512, chunk.Ordinal),
                    filePath);
                continue;
            }

            var full768 = await encoder.EmbedPrefixedTextAsync(chunk.PrefixedText, cancellationToken).ConfigureAwait(false);
            var vector512 = MatryoshkaVectorMath.CutAndNormalize(full768.Span, storedWidth);
            var contentId = ContentIdBuilder.Build(Modality.Text, sourceHash, StoredWidth.W512, chunk.Ordinal);
            var folded = string.Join(' ', KeywordFold.Tokens(KeywordFold.Fold(chunk.RawText)));
            var insert = new TextIndexInsert(
                contentId,
                filePath,
                title,
                chunk.RawText,
                vector512,
                modifiedAt,
                encoderHash,
                true,
                grantPath,
                folded,
                chunk.Ordinal);
            if (index.TryInsert(insert))
            {
                opLog.AppendInsert(contentId);
            }

            resultChunks.Add(new TextIngestChunk(contentId, chunk.Ordinal));
        }

        return new TextIngestResult(sourceHash, resultChunks);
    }
}
