using Recall.Core.Checkpoints;
using Recall.Core.Ingest;

namespace Recall.Core.Ocr;

public static class OcrPageWaitlist
{
    public static void Apply(
        string path,
        IReadOnlyList<int> pageNumbers,
        IIngestCursorStore cursors,
        IngestScheduler? scheduler = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(pageNumbers);
        ArgumentNullException.ThrowIfNull(cursors);
        cursors.ReplaceOcrPages(path, pageNumbers);
        if (pageNumbers.Count > 0 && scheduler is not null)
        {
            scheduler.Enqueue(new IngestJob(IngestModel.Ocr, path));
        }
    }
}
