using Recall.Core.Ports;
using Recall.Core.Search;
using Recall.Core.Sync;
using Recall.Core.Vectors;
using Recall.Core.Vocabulary;

namespace Recall.Core.Text;

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

    public void IngestFile(string filePath, string rawText, CancellationToken cancellationToken)
    {
        var storedWidth = (int)indexPolicy.TextStoredWidth;
        indexPolicy.EnsureStorableWidth(Modality.Text, storedWidth);

        var title = Path.GetFileName(filePath);
        var sourceHash = ContentIdBuilder.HashSourceContent(rawText);
        var chunks = TextChunkPlanner.PlanChunks(
            EmbeddingTaskPrefixes.WrapTitledDocument(title, rawText),
            tokenCounter.CountTokens);

        var mtime = File.Exists(filePath)
            ? new DateTimeOffset(File.GetLastWriteTimeUtc(filePath), TimeSpan.Zero)
            : DateTimeOffset.UtcNow;

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var full768 = encoder.EmbedPrefixedText(chunk.PrefixedText, cancellationToken).ToArray();
            MatryoshkaVectorMath.L2Normalize(full768);
            var vector512 = MatryoshkaVectorMath.CutAndNormalize(full768, storedWidth);
            var contentId = ContentIdBuilder.Build(Modality.Text, sourceHash, StoredWidth.W512, chunk.Ordinal);
            var insert = new TextIndexInsert(contentId, filePath, title, rawText, vector512, mtime);
            if (index.TryInsert(insert))
            {
                opLog.AppendInsert(contentId);
            }
        }
    }
}
