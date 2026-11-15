using Recall.Core.Checkpoints;
using Recall.Core.Ingest;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.Core.Vectors;
using Recall.Core.Vocabulary;

namespace Recall.Core.Search;

public static class PictureExactFile
{
    public static IReadOnlyList<MediaSearchHit> Promote(
        IReadOnlyList<MediaSearchHit> hits,
        string queryHash,
        IReadOnlyList<IngestFileRow> finishedRows)
    {
        ArgumentNullException.ThrowIfNull(hits);
        ArgumentNullException.ThrowIfNull(finishedRows);
        if (string.IsNullOrWhiteSpace(queryHash))
        {
            return hits;
        }

        var match = finishedRows.FirstOrDefault(row =>
            row.State == IngestCursorState.Finished
            && string.Equals(row.ContentHash, queryHash, StringComparison.Ordinal)
            && MediaFileKinds.IsImage(row.Path));
        if (match is null)
        {
            return hits;
        }

        var list = hits.ToList();
        var index = list.FindIndex(hit => string.Equals(hit.SourcePath, match.Path, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            var contentId = ContentIdBuilder.Build(Modality.Image, queryHash, StoredWidth.W768, 0);
            list.Insert(0, new MediaSearchHit(contentId, match.Path, 0, 0, 1, 1));
            return list;
        }

        if (index == 0)
        {
            return hits;
        }

        var promoted = list[index];
        list.RemoveAt(index);
        list.Insert(0, promoted);
        return list;
    }
}