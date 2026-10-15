using Recall.Core.Ports;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Vectors;
using Recall.Core.Vocabulary;

namespace Recall.Core.Text;

public sealed record TextIngestChunk(string ContentId, int Ordinal);

public sealed record TextIngestResult(string ContentHash, IReadOnlyList<TextIngestChunk> Chunks);

public sealed class TextIngestEngine
{
    private readonly ITextEmbeddingEncoder encoder;
    private readonly ITextVectorIndex index;
    private readonly IIndexPolicy indexPolicy;
    private readonly ITextTokenCounter tokenCounter;
    private readonly LocalOpLogWriter opLog;

    public TextIngestEngine(
        ITextEmbeddingEncoder encoder,
        ITextVectorIndex index,
        IIndexPolicy indexPolicy,
        ITextTokenCounter tokenCounter,
        LocalOpLogWriter opLog)
    {
        this.encoder = encoder;
        this.index = index;
        this.indexPolicy = indexPolicy;
        this.tokenCounter = tokenCounter;
        this.opLog = opLog;
    }

    public async Task<TextIngestResult> IngestFileAsync(string filePath, string rawText, DateTimeOffset modifiedAt, CancellationToken cancellationToken)
    {
        var storedWidth = (int)indexPolicy.TextStoredWidth;
        indexPolicy.EnsureStorableWidth(Modality.Text, storedWidth);

        var title = Path.GetFileName(filePath);
        var sourceHash = ContentIdBuilder.HashSourceContent(rawText);
        var chunks = TextChunkPlanner.PlanChunks(title, rawText, tokenCounter.CountTokens);

        var resultChunks = new List<TextIngestChunk>();
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var full768 = await encoder.EmbedPrefixedTextAsync(chunk.PrefixedText, cancellationToken).ConfigureAwait(false);
            var vector512 = MatryoshkaVectorMath.CutAndNormalize(full768.Span, storedWidth);
            var contentId = ContentIdBuilder.Build(Modality.Text, sourceHash, StoredWidth.W512, chunk.Ordinal);
            var insert = new TextIndexInsert(contentId, filePath, title, chunk.RawText, vector512, modifiedAt);
            if (index.TryInsert(insert))
            {
                opLog.AppendInsert(contentId);
            }

            resultChunks.Add(new TextIngestChunk(contentId, chunk.Ordinal));
        }

        return new TextIngestResult(sourceHash, resultChunks);
    }
}
